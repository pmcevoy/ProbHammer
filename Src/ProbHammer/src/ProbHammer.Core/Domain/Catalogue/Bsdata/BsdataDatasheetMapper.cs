using System.Text.RegularExpressions;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Maps a resolved <see cref="BsSelectionEntry"/> (see <see cref="BsdataNameResolver"/>) into a
/// <see cref="Datasheet"/>. Walks the entry's full descendant tree once, collecting every
/// "Unit"-typeName profile as a named <see cref="Statline"/> (deduped by name, first occurrence
/// wins, in declared order - this single walk handles both the single-model case, where the
/// profile sits on the entry itself, and the squad case, where it sits on distinct nested child
/// entries, per design.md's squad-detection decision), every "Ranged Weapons"/"Melee
/// Weapons"-typeName profile as a resolvable <see cref="WeaponProfile"/>, and every
/// "Abilities"-typeName profile as an <see cref="Ability"/>. EntryLinks are followed through an
/// id index built over the closure so that wargear defined via a shared entry (in this file or an
/// imported one) is still reachable - see design.md's "entryLink target location" risk. InfoLinks
/// of type "profile" are followed through a separate id index into BsCatalogue.SharedProfiles -
/// some model statlines (observed in real data: Chaos - Chaos Space Marines.json's "Legionaries"
/// squad reaches its troop model's Unit profile this way, not nested in any selectionEntry's own
/// Profiles list) exist only as a standalone shared profile referenced this way.
/// </summary>
public static class BsdataDatasheetMapper
{
    public static Datasheet BuildDatasheet(
        BsSelectionEntry entry,
        IReadOnlyDictionary<string, BsSelectionEntry> idIndex,
        IReadOnlyDictionary<string, BsSelectionEntryGroup> groupIdIndex,
        IReadOnlyDictionary<string, BsProfile>? profileIdIndex = null)
    {
        var context = new WalkContext(idIndex, groupIdIndex, profileIdIndex ?? new Dictionary<string, BsProfile>());
        WalkEntry(entry, context);

        return new Datasheet(
            entry.Name,
            factionKeywords: [],
            keywords: [],
            abilities: context.Abilities,
            statlines: context.Statlines,
            weaponProfiles: context.Weapons.Values);
    }

    private sealed class WalkContext(
        IReadOnlyDictionary<string, BsSelectionEntry> idIndex,
        IReadOnlyDictionary<string, BsSelectionEntryGroup> groupIdIndex,
        IReadOnlyDictionary<string, BsProfile> profileIdIndex)
    {
        public IReadOnlyDictionary<string, BsSelectionEntry> IdIndex { get; } = idIndex;
        public IReadOnlyDictionary<string, BsSelectionEntryGroup> GroupIdIndex { get; } = groupIdIndex;
        public IReadOnlyDictionary<string, BsProfile> ProfileIdIndex { get; } = profileIdIndex;

        public List<(string Name, Statline Statline)> Statlines { get; } = [];
        public HashSet<string> SeenStatlineNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, WeaponProfile> Weapons { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<Ability> Abilities { get; } = [];
        public HashSet<string> SeenAbilityNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> VisitedEntryIds { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> VisitedGroupIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static void WalkEntry(BsSelectionEntry entry, WalkContext ctx)
    {
        if (!string.IsNullOrEmpty(entry.Id) && !ctx.VisitedEntryIds.Add(entry.Id))
            return;

        foreach (var profile in entry.Profiles)
            ProcessProfile(profile, ctx);

        foreach (var child in entry.SelectionEntries)
            WalkEntry(child, ctx);

        foreach (var group in entry.SelectionEntryGroups)
            WalkGroup(group, ctx);

        foreach (var link in entry.EntryLinks)
            WalkLink(link, ctx);

        foreach (var link in entry.InfoLinks)
            WalkInfoLink(link, ctx);
    }

    private static void WalkGroup(BsSelectionEntryGroup group, WalkContext ctx)
    {
        if (!string.IsNullOrEmpty(group.Id) && !ctx.VisitedGroupIds.Add(group.Id))
            return;

        foreach (var child in group.SelectionEntries)
            WalkEntry(child, ctx);

        foreach (var nested in group.SelectionEntryGroups)
            WalkGroup(nested, ctx);

        foreach (var link in group.EntryLinks)
            WalkLink(link, ctx);

        foreach (var link in group.InfoLinks)
            WalkInfoLink(link, ctx);
    }

    private static void WalkLink(BsEntryLink link, WalkContext ctx)
    {
        if (ctx.IdIndex.TryGetValue(link.TargetId, out var targetEntry))
        {
            WalkEntry(targetEntry, ctx);
        }
        else if (ctx.GroupIdIndex.TryGetValue(link.TargetId, out var targetGroup))
        {
            WalkGroup(targetGroup, ctx);
        }
        // else: target not found in this closure (e.g. links into apparatus this loader doesn't
        // model) - skipped rather than failing resolution.
    }

    private static void WalkInfoLink(BsEntryLink link, WalkContext ctx)
    {
        if (link.Type == "profile" && ctx.ProfileIdIndex.TryGetValue(link.TargetId, out var profile))
            ProcessProfile(profile, ctx);
        // else: a "rule" infoLink (rules-text reference) or an unresolvable target - not
        // something this loader maps.
    }

    private static void ProcessProfile(BsProfile profile, WalkContext ctx)
    {
        switch (profile.TypeName)
        {
            case "Unit":
                if (ctx.SeenStatlineNames.Add(profile.Name))
                    ctx.Statlines.Add((profile.Name, MapStatline(profile)));
                break;
            case "Ranged Weapons":
                ctx.Weapons.TryAdd(profile.Name, MapRangedWeapon(profile));
                break;
            case "Melee Weapons":
                ctx.Weapons.TryAdd(profile.Name, MapMeleeWeapon(profile));
                break;
            case "Abilities":
                if (ctx.SeenAbilityNames.Add(profile.Name))
                    ctx.Abilities.Add(MapAbility(profile));
                break;
        }
    }

    private static Statline MapStatline(BsProfile profile) =>
        new(
            M: ParseMeasurement(profile.CharacteristicText("M")),
            T: ParsePlainInt(profile.CharacteristicText("T")),
            Sv: ParseThreshold(profile.CharacteristicText("Sv"),CharacteristicPattern.Sv),
            W: ParsePlainInt(profile.CharacteristicText("W")),
            Ld: ParseThreshold(profile.CharacteristicText("LD"), CharacteristicPattern.Ld),
            Oc: ParsePlainInt(profile.CharacteristicText("OC")))
        {
            InSv = ParseThreshold(profile.CharacteristicText("InSv"), CharacteristicPattern.InvSv)
        };

    private static WeaponProfile MapRangedWeapon(BsProfile profile)
    {
        var weapon = new RangedWeapon(
            profile.Name,
            ParseMeasurement(profile.CharacteristicText("Range")),
            DiceExpression.Parse(profile.CharacteristicText("A") ?? "0"),
            ParseThreshold(profile.CharacteristicText("BS"), CharacteristicPattern.Bs),
            ParsePlainInt(profile.CharacteristicText("S")),
            ParsePlainInt(profile.CharacteristicText("AP")),
            DiceExpression.Parse(profile.CharacteristicText("D") ?? "0"));

        return WeaponKeywordParser.Apply(weapon, profile.CharacteristicText("Keywords") ?? "-");
    }

    private static WeaponProfile MapMeleeWeapon(BsProfile profile)
    {
        var weapon = new MeleeWeapon(
            profile.Name,
            DiceExpression.Parse(profile.CharacteristicText("A") ?? "0"),
            ParseThreshold(profile.CharacteristicText("WS"), CharacteristicPattern.Ws),
            ParsePlainInt(profile.CharacteristicText("S")),
            ParsePlainInt(profile.CharacteristicText("AP")),
            DiceExpression.Parse(profile.CharacteristicText("D") ?? "0"));

        return WeaponKeywordParser.Apply(weapon, profile.CharacteristicText("Keywords") ?? "-");
    }

    private static Ability MapAbility(BsProfile profile) =>
        new()
        {
            Name = profile.Name,
            Text = profile.CharacteristicText("Description") ?? "",
            Scope = AbilityScope.Unit
        };

    /// <summary>True for BSData's "not applicable" marker - confirmed on M (a Fortification/
    /// terrain piece that cannot move, e.g. Feculent Gnarlmaw), OC (a piece that can never
    /// contribute to objective control, e.g. Heldrake), and Sv (an unarmoured terrain piece).
    /// Parses as 0, the same "0 means absent" convention already used for Statline.InSv.</summary>
    private static bool IsNotApplicable(string text) => text == "-";

    private static bool IsNA(string text) => text.Equals("N/A", StringComparison.OrdinalIgnoreCase);

    /// <summary>Strips a trailing measurement mark and parses the number. Usually a trailing `"`
    /// (5", 18"), but some flyers print a minimum-move constraint as e.g. `12+` instead of a fixed
    /// distance (confirmed on Heldrake, Hell Talon, Raven Strike Fighter) - `+` is stripped too,
    /// losing the "minimum, not exact" nuance rather than failing to parse at all. "N/A" (confirmed
    /// on the Deathstrike missile's own Range - it targets by a different rule entirely) parses as
    /// 0, same as "-". A literal "Melee" (confirmed on one "Ranged Weapons"-typeName profile,
    /// Greater Blight Drone [Legends] - a data-entry classification quirk, not a real ranged
    /// weapon) also parses as 0, mirroring MeleeWeapon's own fixed Range=0.</summary>
    private static int ParseMeasurement(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Trim();
        if (IsNotApplicable(text) || IsNA(text) || text.Equals("Melee", StringComparison.OrdinalIgnoreCase)) return 0;
        return int.Parse(text.TrimEnd('"', '+'));
    }

    
    /// <summary>Parses a single-value threshold characteristic (Sv, InSv, BS, WS, LD) against the
    /// shape permitted for that specific characteristic (see <see cref="CharacteristicPattern"/>):
    /// digits with an optional trailing "+" for all five, or a trailing "*" footnote marker for
    /// InSv only (e.g. "5+*", confirmed on Wyches/Howling Banshees - not confirmed on the other
    /// four, but no reason to assume they can't appear there too). An empty string (BSData's "no
    /// invulnerable save" representation), "N/A" (BSData's no-hit-roll-required marker on e.g. a
    /// Torrent weapon's BS/WS), or "-" (not applicable) parses as 0 regardless of characteristic.
    /// Anything that doesn't match the allowed shape - including InSv's two known
    /// two-different-values-by-context forms, either "/"-split (e.g. "4+* / 5+", confirmed on
    /// Wyches and Howling Banshees) or a parenthetical annotation (e.g. "5+ (Ranged)", confirmed on
    /// the four Titan datasheets and the Sokar-pattern Stormbird) - throws <see
    /// cref="AmbiguousCharacteristicException"/> instead of silently keeping one value. This
    /// reverses this method's earlier behavior of always taking the first/unparenthesized value:
    /// Statline's single-int Sv/InSv fields have no way to represent two different values, and
    /// picking one silently hid that loss instead of surfacing it. The corpus-wide resolution test
    /// (PROGRESS.md's "Known Issues") is expected to catch this exception for the known
    /// multi-value-InSv datasheets and check it against a maintained "known limitation" allowlist,
    /// rather than this method quietly tolerating the ambiguity itself.</summary>
    private static int ParseThreshold(string? text, CharacteristicPattern expectedPattern)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Trim();
        if (IsNotApplicable(text) || IsNA(text)) return 0;
        
        var match = expectedPattern.Pattern.Match(text);
        if(!match.Success)
            throw new AmbiguousCharacteristicException(expectedPattern.Characteristic, text);

        return int.Parse(match.Groups[1].Value);
    }

    /// <summary>Parses a plain integer characteristic (W, OC, S, AP - AP already carries its own
    /// negative sign in source data, matching WeaponProfile.Ap's convention). "-" (not applicable,
    /// confirmed on OC) parses as 0. A trailing "+" (confirmed on a Deathwatch Terminator Squad
    /// weapon's Strength, "8+") is stripped, same as the measurement/threshold parsers.</summary>
    private static int ParsePlainInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Trim();
        return IsNotApplicable(text) ? 0 : int.Parse(text.TrimEnd('+'));
    }
}

/// <summary>The exact single-value shape <see cref="BsdataDatasheetMapper"/>'s ParseThreshold
/// accepts for one named characteristic, plus that name for use in a thrown
/// <see cref="AmbiguousCharacteristicException"/>'s message and properties. Each of Sv/InSv/BS/
/// WS/LD gets its own instance rather than sharing one regex so that InSv's extra trailing-"*"
/// allowance (a footnoted/conditional save, e.g. "5+*") doesn't silently leak into the other four,
/// where it has never been observed in real data.</summary>
public partial class CharacteristicPattern
{
    private CharacteristicPattern(string characteristic, Regex pattern)
    {
        Characteristic = characteristic;
        Pattern = pattern;
    }

    internal static readonly CharacteristicPattern Ws  = new ("Ws", WsRegex());
    internal static readonly CharacteristicPattern Bs = new("Bs", BsRegex());
    internal static readonly CharacteristicPattern Sv = new("Sv", SvRegex());
    internal static readonly CharacteristicPattern Ld = new("Ld", LdRegex());
    internal static readonly CharacteristicPattern InvSv = new("InSv", InvSvRegex());

    public string Characteristic { get; init; }
    public Regex Pattern { get; init; }

    [GeneratedRegex(@"^(\d+)[+]?$")]
    private static partial Regex WsRegex();
    [GeneratedRegex(@"^(\d+)[+]?$")]
    private static partial Regex BsRegex();
    [GeneratedRegex(@"^(\d+)[+]?$")]
    private static partial Regex SvRegex();
    [GeneratedRegex(@"^(\d+)[+]?$")]
    private static partial Regex LdRegex();
    [GeneratedRegex(@"^(\d+)[+*]?$")]
    private static partial Regex InvSvRegex();
}

/// <summary>Thrown by ParseThreshold when a threshold characteristic's text doesn't match its
/// allowed single-value shape (see <see cref="CharacteristicPattern"/>) - most notably BSData's
/// two-different-values-by-context InSv forms (see ParseThreshold's own doc comment). Carries both
/// the characteristic name and the raw offending text as properties, not just baked into the
/// message, so a catching corpus-wide test can check a failure against a maintained "known
/// limitation" allowlist without needing to parse <see cref="Exception.Message"/>.</summary>
public class AmbiguousCharacteristicException(string characteristic, string text) : Exception($"Unexpected text for characteristic: {characteristic}={text}")
{
    public string Text { get; } = text;
    public string Characteristic { get; } = characteristic;
}