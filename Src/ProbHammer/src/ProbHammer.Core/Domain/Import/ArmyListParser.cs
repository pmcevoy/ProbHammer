using System.Text.RegularExpressions;

namespace ProbHammer.Core.Domain.Import;

/// <summary>Concrete IArmyListParser for the GW-app 11e export text shape (see design.md's
/// "Pipeline shape", the iOS-captured exports in data/gw-app-export*.txt, and the
/// Android-captured ones in data/gw-android-export*.txt - see
/// openspec/changes/harden-army-list-parsing-for-android-exports/design.md for the two formats'
/// differences). Line-based: blank lines are discarded, then every remaining line is classified
/// either positionally (the fixed army-metadata preamble), by a small set of regexes (metadata
/// suffixes, section headers, attachment-group boundaries, unit headers - matched
/// case-insensitively where an Android export is known to vary casing), or, for bullet lines, by
/// the bullet character itself ("•" top-level, "◦" nested) together with each line's own
/// leading-whitespace count - Android reuses "•" for a nested list's own first item too and drops
/// the bullet entirely on every later item in that list, so indentation is what disambiguates a
/// nested item, a continuation, and a new unit/section header once the glyph alone no longer can;
/// see CollectBulletBlocks' own doc comment.</summary>
public sealed partial class ArmyListParser : IArmyListParser
{
    private static readonly string[] SectionHeaders =
        ["CHARACTERS", "BATTLELINE", "DEDICATED TRANSPORTS", "OTHER DATASHEETS"];

    public ParsedArmyList Parse(string exportText)
    {
        var cursor = new LineCursor(Tokenize(exportText));

        var (name, pointsSpent) = ParseNamePoints(cursor.Next().Content);

        var faction = new List<string>();
        while (cursor.HasMore && !TryParseDetachment(cursor.Peek().Content, out _))
            faction.Add(cursor.Next().Content);

        var detachments = new List<string>();
        while (cursor.HasMore && TryParseDetachment(cursor.Peek().Content, out var detachmentName))
        {
            detachments.Add(detachmentName!);
            cursor.Next();
        }

        // ForceDisposition is a free-text line with no fixed shape of its own, so its absence can
        // only be inferred from what follows: a real Android export (gw-android-export-
        // deathguard.txt) omits it entirely when nothing was selected, going straight from the
        // Detachment line to the "<BattleSize> (<N> Points)" line - if that next line already
        // looks like a BattleSize header, ForceDisposition is treated as absent rather than
        // consumed as its text.
        var forceDisposition = NamePointsRegex().IsMatch(cursor.Peek().Content)
            ? ""
            : cursor.Next().Content;
        var (battleSize, pointsLimit) = ParseNamePoints(cursor.Next().Content);

        var attachmentGroups = new List<ParsedAttachmentGroup>();
        var standaloneUnits = new List<ParsedUnit>();
        var mode = BodyMode.None;
        List<(ParsedUnit Unit, Role Role)>? currentGroupMembers = null;

        while (cursor.HasMore)
        {
            var line = cursor.Peek().Content;

            if (string.Equals(line, "ATTACHED UNITS", StringComparison.OrdinalIgnoreCase))
            {
                cursor.Next();
                mode = BodyMode.Attached;
                continue;
            }

            if (SectionHeaders.Contains(line))
            {
                cursor.Next();
                mode = BodyMode.Standalone;
                continue;
            }

            if (line.StartsWith("Exported with", StringComparison.Ordinal))
                break;

            if (AttachedUnitGroupRegex().IsMatch(line))
            {
                FlushGroup(currentGroupMembers, attachmentGroups);
                currentGroupMembers = [];
                cursor.Next();
                continue;
            }

            var isAttached = mode == BodyMode.Attached;
            var unit = ParseUnitBlock(cursor, isAttached, out var role);
            if (isAttached)
                currentGroupMembers!.Add((unit, role));
            else
                standaloneUnits.Add(unit);
        }

        FlushGroup(currentGroupMembers, attachmentGroups);

        return new ParsedArmyList(
            name, pointsSpent, faction, detachments, forceDisposition, battleSize, pointsLimit,
            attachmentGroups, standaloneUnits);
    }

    private enum BodyMode
    {
        None,
        Attached,
        Standalone
    }

    private enum Role
    {
        Bodyguard,
        Leader,
        Support
    }

    private static void FlushGroup(
        List<(ParsedUnit Unit, Role Role)>? members, List<ParsedAttachmentGroup> groups)
    {
        if (members is null)
            return;

        var bodyguards = members.Where(m => m.Role == Role.Bodyguard).ToList();
        if (bodyguards.Count != 1)
            throw new ArmyListParseException(
                $"Attached unit group must have exactly one Bodyguard-role member, found {bodyguards.Count}.");

        var attached = members.Where(m => m.Role != Role.Bodyguard).Select(m => m.Unit).ToList();
        groups.Add(new ParsedAttachmentGroup(bodyguards[0].Unit, attached));
    }

    private static ParsedUnit ParseUnitBlock(LineCursor cursor, bool isAttached, out Role role)
    {
        var (unitName, _) = ParseNamePoints(cursor.Next().Content);
        var blocks = CollectBulletBlocks(cursor);

        role = Role.Leader;
        var blockIndex = 0;
        if (isAttached)
        {
            if (blocks.Count == 0 || !TryParseAttachedAs(blocks[0].TopContent, out role))
                throw new ArmyListParseException(
                    $"Unit '{unitName}' is missing its 'Attached as:' role line.", unitName);
            blockIndex = 1;
        }

        var enhancements = new List<string>();
        var explicitGroups = new List<ParsedModelGroup>();
        var directWeapons = new List<(string Name, int Count)>();

        for (; blockIndex < blocks.Count; blockIndex++)
        {
            var (topContent, nested) = blocks[blockIndex];

            var enhancementMatch = EnhancementRegex().Match(topContent);
            if (enhancementMatch.Success)
            {
                enhancements.Add(enhancementMatch.Groups[1].Value.Trim());
                continue;
            }

            var weaponMatch = WeaponCountRegex().Match(topContent);
            if (!weaponMatch.Success)
                continue; // e.g. "Warlord" - not wargear/model composition, not captured

            var count = int.Parse(weaponMatch.Groups[1].Value);
            var itemName = weaponMatch.Groups[2].Value.Trim();

            if (nested.Count > 0)
            {
                var weapons = nested.Select(ParseWeaponCountLine).ToList();
                explicitGroups.AddRange(PartitionModelGroup(unitName, itemName, count, weapons));
            }
            else
            {
                directWeapons.Add((itemName, count));
            }
        }

        var modelGroups = new List<ParsedModelGroup>(explicitGroups);
        if (directWeapons.Count > 0)
            modelGroups.Add(SynthesizeImplicitGroup(unitName, directWeapons));

        return new ParsedUnit(unitName, modelGroups, enhancements);
    }

    /// <summary>Splits one explicit "Nx ModelName" header's nested weapons into one or more
    /// ParsedModelGroups: a weapon whose count equals the header's own total is common to every
    /// resulting sub-group; every other weapon is its own mutually-exclusive alternative, becoming
    /// its own sub-group carrying that weapon's own count - see "Model Group and Weapon Selection
    /// Parsing". Two alternatives can coincidentally share the same numeric count without merging
    /// (verified against a real second squad in design.md: Company Veteran's "1x Master-crafted
    /// bolt rifle" / "1x Master-crafted heavy bolter" are two distinct 1-model sub-groups, not one
    /// 1-model sub-group carrying both weapons - grouping by count value instead of by weapon entry
    /// would incorrectly collapse them and fail the total-count sum). Throws when the non-common
    /// counts don't sum exactly to the total, rather than guessing a split. Known open gap:
    /// Adeptus Custodes' Custodian Guard ("1x Guardian spear" / "3x Praesidium Shield" / "3x
    /// Sentinel blade" against a total of 4) needs Shield+blade paired as one 3-model sub-group,
    /// which this count-only rule can't infer without risking a wrong merge on the Company
    /// Veteran case above - left throwing loudly rather than guessed at; see PROGRESS.md's Known
    /// Issues.</summary>
    private static IEnumerable<ParsedModelGroup> PartitionModelGroup(
        string unitName, string modelName, int totalCount, IReadOnlyList<(string Name, int Count)> weapons)
    {
        var shared = weapons.Where(w => w.Count == totalCount).Select(w => w.Name).ToList();
        var remaining = weapons.Where(w => w.Count != totalCount).ToList();

        if (remaining.Count == 0)
            return [new ParsedModelGroup(modelName, totalCount, shared)];

        if (remaining.Sum(w => w.Count) != totalCount)
        {
            var rawText = string.Join(", ", weapons.Select(w => $"{w.Count}x {w.Name}"));
            throw new ArmyListParseException(
                $"Unit '{unitName}' model group '{modelName}' (total {totalCount}) has weapon " +
                $"counts that cannot be partitioned into a common set plus alternatives summing " +
                $"to the total: {rawText}",
                unitName, rawText);
        }

        return remaining.Select(w => new ParsedModelGroup(modelName, w.Count, [.. shared, w.Name]));
    }

    private static ParsedModelGroup SynthesizeImplicitGroup(string unitName,
        IReadOnlyList<(string Name, int Count)> directWeapons)
    {
        var weapons = directWeapons.SelectMany(w => Enumerable.Repeat(w.Name, w.Count)).ToList();
        return new ParsedModelGroup(unitName, 1, weapons);
    }

    /// <summary>Android exports never emit the nested "◦" glyph at all - a weapon list's own
    /// first item (whether at the unit's top level or nested under a model-group header) reuses
    /// "•", and every later item in that same list drops its bullet entirely, relying on
    /// indentation alone. Two signals - both confirmed necessary against the real captured
    /// Android exports (see design.md) - resolve this without a depth-specific special case:
    /// (1) a "•" line nests under the current top-level block, rather than starting a new
    /// sibling, only when that block's own text looks like an "Nx ModelName" header (so it can
    /// plausibly own a weapon list), it has no nested item yet (so only the list's own first
    /// item optionally re-adopts a bullet), and this line is indented deeper than that block's
    /// own bullet - the same relation a role line like "Attached as: Leader" and a following
    /// "Warlord"/"1x Weapon" sibling bullet share, which is exactly why the "Nx ModelName" check
    /// on the parent's own text is also required, not indentation alone. (2) an unbulleted line
    /// extends whichever list is currently open only when its own indentation is at least that
    /// list's first item's own text column - this is what stops a genuine next unit/section
    /// header (always at column 0) from being swallowed as a stray continuation.</summary>
    private static List<(string TopContent, List<string> Nested)> CollectBulletBlocks(LineCursor cursor)
    {
        var blocks = new List<(string TopContent, List<string> Nested)>();
        var lastTopIndent = -1;
        var openListTextColumn = -1;

        while (cursor.HasMore)
        {
            var line = cursor.Peek();

            if (line.Content.StartsWith('•'))
            {
                var content = StripBulletPrefix(line.Content);
                var nestsUnderCurrentBlock =
                    blocks.Count > 0
                    && blocks[^1].Nested.Count == 0
                    && line.Indent > lastTopIndent
                    && WeaponCountRegex().IsMatch(blocks[^1].TopContent);

                if (nestsUnderCurrentBlock)
                {
                    blocks[^1].Nested.Add(content);
                }
                else
                {
                    blocks.Add((content, []));
                    lastTopIndent = line.Indent;
                }

                openListTextColumn = line.Indent + 2;
                cursor.Next();
            }
            else if (line.Content.StartsWith('◦'))
            {
                if (blocks.Count == 0)
                    throw new ArmyListParseException(
                        $"Nested bullet line '{line.Content}' has no preceding top-level bullet.");
                blocks[^1].Nested.Add(StripBulletPrefix(line.Content));
                openListTextColumn = line.Indent + 2;
                cursor.Next();
            }
            else if (blocks.Count > 0 && line.Indent >= openListTextColumn)
            {
                if (blocks[^1].Nested.Count > 0)
                    blocks[^1].Nested.Add(line.Content);
                else
                    blocks.Add((line.Content, []));

                cursor.Next();
            }
            else
            {
                break;
            }
        }

        return blocks;
    }

    private static string StripBulletPrefix(string line) => line[1..].TrimStart();

    private static (string Name, int Count) ParseWeaponCountLine(string content)
    {
        var match = WeaponCountRegex().Match(content);
        if (!match.Success)
            throw new ArmyListParseException($"Expected a 'Nx WeaponName' line, got '{content}'.");

        return (match.Groups[2].Value.Trim(), int.Parse(match.Groups[1].Value));
    }

    private static bool TryParseAttachedAs(string content, out Role role)
    {
        var match = AttachedAsRegex().Match(content);
        if (!match.Success)
        {
            role = Role.Leader;
            return false;
        }

        role = Enum.Parse<Role>(match.Groups[1].Value);
        return true;
    }

    private static (string Name, int Points) ParseNamePoints(string line)
    {
        var match = NamePointsRegex().Match(line);
        if (!match.Success)
            throw new ArmyListParseException($"Expected a '<Name> (<N> Points)' header line, got '{line}'.");

        return (match.Groups[1].Value.Trim(), int.Parse(match.Groups[2].Value.Replace(",", "")));
    }

    private static bool TryParseDetachment(string line, out string? name)
    {
        var match = DetachmentRegex().Match(line);
        if (!match.Success)
        {
            name = null;
            return false;
        }

        name = match.Groups[1].Value.Trim();
        return true;
    }

    /// <summary>One non-blank export line, with its own leading-whitespace count preserved
    /// alongside its fully-trimmed content - Content is what every existing regex/equality check
    /// already expects unchanged; Indent exists only for CollectBulletBlocks' bullet-continuation
    /// rule (see its own doc comment).</summary>
    private readonly record struct Line(string Content, int Indent);

    private static List<Line> Tokenize(string exportText) =>
        exportText
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => l.TrimStart().Length > 0)
            .Select(l => new Line(l.TrimStart(), l.Length - l.TrimStart().Length))
            .ToList();

    [GeneratedRegex(@"^(.+?)\s*\((\d[\d,]*)\s*Points\)$", RegexOptions.IgnoreCase)]
    private static partial Regex NamePointsRegex();

    [GeneratedRegex(@"^(.+?)\s*\((\d+)\s*Detachment Points?\)$")]
    private static partial Regex DetachmentRegex();

    [GeneratedRegex(@"^Attached unit \d+$", RegexOptions.IgnoreCase)]
    private static partial Regex AttachedUnitGroupRegex();

    [GeneratedRegex(@"^Attached as:\s*(Leader|Support|Bodyguard)(?:\s*\([^)]*\))?$")]
    private static partial Regex AttachedAsRegex();

    [GeneratedRegex(@"^Enhancements?:\s*(.+)$")]
    private static partial Regex EnhancementRegex();

    [GeneratedRegex(@"^(\d+)x\s*(.+)$")]
    private static partial Regex WeaponCountRegex();

    private sealed class LineCursor(List<Line> lines)
    {
        private int _index;

        public bool HasMore => _index < lines.Count;

        public Line Peek() => lines[_index];

        public Line Next() => lines[_index++];
    }
}