using System.Text.RegularExpressions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import.BattleScribe.Json;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Core.Domain.Import.BattleScribe;

/// <summary>
/// Maps an already-resolved <see cref="BsRoster"/> (see battlescribe-roster-import) directly into
/// an <see cref="ArmyRoster"/>, bypassing <c>Domain.Catalogue.Bsdata</c> entirely - every
/// <see cref="Datasheet"/>/<see cref="Statline"/>/<see cref="WeaponProfile"/>/<see cref="Ability"/>
/// needed is synthesized from the roster JSON's own already-resolved <c>profiles</c>/<c>rules</c>,
/// per design.md's "Bypass BSData entirely" decision.
/// </summary>
public static partial class BattleScribeRosterMapper
{
    public static ArmyRoster Map(BsRoster roster)
    {
        var force = roster.Forces.FirstOrDefault()
                    ?? throw new BattleScribeRosterParseException("Roster has no forces.");

        var pointsSpent = ReadCost(roster.Costs, "pts");
        var pointsLimit = ReadCost(roster.CostLimits, "pts");
        var faction = SplitFaction(force.CatalogueName);
        var detachments = FindGroup(force.Selections, "Detachment", "Detachments").Select(MapDetachment).ToList();
        var forceDisposition =
            FindGroup(force.Selections, "Force Disposition").Select(s => s.Name).FirstOrDefault() ?? "";
        var battleSize = StripTrailingParenthetical(
            FindGroup(force.Selections, "Battle Size").Select(s => s.Name).FirstOrDefault() ?? "");

        var knownArmyRuleNames = ArmyRuleNameLookup.Resolve(faction);
        var units = BuildUnits(force.Selections, knownArmyRuleNames);

        return new ArmyRoster(roster.Name, pointsSpent, faction, detachments, forceDisposition, battleSize, pointsLimit,
            units);
    }

    private static int ReadCost(IReadOnlyList<BsRosterCost> costs, string name) =>
        (int)Math.Round(costs.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
            ?.Value ?? 0);

    /// <summary>Maps a selected Detachment's own top-level selection directly into a
    /// <see cref="ResolvedDetachment"/> - no BSData involvement, per this pipeline's existing
    /// "synthesizes everything from the JSON's own already-resolved data" design. A roster JSON
    /// already carries a selected Detachment's own rule text inline on its own
    /// <c>selections[].rules[]</c> (confirmed real shape: Death Guard's "Virulent Vectorium" carries
    /// a nested "Worldblight" rule with name/description inline; Black Templars' "Companions of
    /// Vehemence" carries "Righteous Fervour" the same way).</summary>
    private static ResolvedDetachment MapDetachment(BsRosterSelection selection) =>
        new(selection.Name, selection.Rules.Select(r => new DetachmentRule(r.Name, r.Description)).ToList());

    private static IReadOnlyList<BsRosterSelection> FindGroup(IReadOnlyList<BsRosterSelection> topSelections,
        params string[] groupNames) =>
        topSelections.FirstOrDefault(s =>
                groupNames.Any(n => string.Equals(s.Name, n, StringComparison.OrdinalIgnoreCase)))
            ?.Selections ?? [];

    /// <summary>Splits a force's catalogue name (e.g. "Imperium - Adeptus Astartes - Black
    /// Templars") into an ordered segment list, matching <c>ArmyRoster.Faction</c>'s "parent codex
    /// before sub-faction" convention - not required to match <c>BsdataFactionResolver</c>'s own
    /// suffix-matching convention, since that resolver is never invoked by this pipeline (see
    /// design.md's Risks - Faction is otherwise unread outside the text pipeline).</summary>
    private static List<string> SplitFaction(string catalogueName)
    {
        var segments =
            catalogueName.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments.ToList() : [catalogueName];
    }

    [GeneratedRegex(@"^(.+?)\s*\(.*\)$")]
    private static partial Regex TrailingParentheticalRegex();

    private static string StripTrailingParenthetical(string text)
    {
        var match = TrailingParentheticalRegex().Match(text);
        return match.Success ? match.Groups[1].Value : text;
    }

    /// <summary>Resolves attachment (see battlescribe-roster-import's Attachment Relationship
    /// Resolution) from every top-level "unit"/"model" selection's own outgoing
    /// "Leading"/"Supporting" association, then builds each resulting <see cref="Unit"/>/
    /// <see cref="AttachedUnit"/>. Every other top-level selection (Battle Size, Detachment, Force
    /// Disposition, Show/Hide Options - all "upgrade"-typed) is not a real army entry and is
    /// excluded here.</summary>
    private static List<ICombatUnit> BuildUnits(
        IReadOnlyList<BsRosterSelection> topSelections, IReadOnlySet<string> knownArmyRuleNames)
    {
        var unitSelections = topSelections.Where(s => s.Type is "unit" or "model").ToList();

        var outgoing = unitSelections
            .Select(s => (Selection: s,
                Target: s.Associations.FirstOrDefault(a => a.Name is "Leading" or "Supporting")?.To))
            .ToList();

        var bodyguardIds = outgoing.Where(x => x.Target is not null).Select(x => x.Target!).ToHashSet();
        var attachedIds = outgoing.Where(x => x.Target is not null).Select(x => x.Selection.Id).ToHashSet();

        var units = new List<ICombatUnit>();
        foreach (var selection in unitSelections)
        {
            if (attachedIds.Contains(selection.Id))
                continue; // an Attached member - built below as part of its target's group

            if (bodyguardIds.Contains(selection.Id))
            {
                var attached = outgoing.Where(x => x.Target == selection.Id)
                    .Select(x => BuildUnit(x.Selection, knownArmyRuleNames));
                units.Add(new AttachedUnit(BuildUnit(selection, knownArmyRuleNames), attached));
            }
            else
            {
                units.Add(BuildUnit(selection, knownArmyRuleNames));
            }
        }

        return units;
    }

    /// <summary>Builds one <see cref="Unit"/> from a top-level unit/model selection: its
    /// Datasheet-wide Abilities from the selection's own "Abilities"-typeName profiles (Intrinsic)
    /// plus its own <c>rules</c> entries (CoreRule - see battlescribe-roster-import's Core Rule
    /// Extraction), its ModelLines and their Statlines/weapon profiles from BuildModelLines, and
    /// its Enhancements from every descendant selection tagged with a <c>costs["Enhancements"]</c>
    /// entry anywhere in its tree.</summary>
    private static Unit BuildUnit(BsRosterSelection top, IReadOnlySet<string> knownArmyRuleNames)
    {
        var statlines = new List<(string Name, Statline Statline)>();
        var seenStatlineNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var weapons = new Dictionary<string, WeaponProfile>(StringComparer.OrdinalIgnoreCase);

        void AddStatline(string name, Statline statline)
        {
            if (seenStatlineNames.Add(name))
                statlines.Add((name, statline));
        }

        void AddWeaponProfile(BsRosterProfile profile) =>
            weapons.TryAdd(profile.Name, MapWeaponProfile(profile));

        var modelLines = BuildModelLines(top, AddStatline, AddWeaponProfile);

        var intrinsicAbilities = top.Profiles
            .Where(p => p.TypeName == "Abilities")
            .Select(p => MapAbility(p, AbilityOrigin.Intrinsic));
        var coreRuleAbilities = top.Rules.Select(rule => MapCoreRuleAbility(rule, knownArmyRuleNames));

        var datasheet = new Datasheet(
            top.Name,
            factionKeywords: [],
            keywords: [],
            abilities: intrinsicAbilities.Concat(coreRuleAbilities).ToList(),
            statlines: statlines,
            weaponProfiles: weapons.Values);

        return new Unit(datasheet, FindEnhancements(top), modelLines);
    }

    /// <summary>Builds one <see cref="ModelLine"/> per per-loadout model group (see
    /// battlescribe-roster-import's Statline, Weapon, and Ability Extraction requirement): when the
    /// top-level selection has one or more immediate "model"-typed nested selections, each is its
    /// own loadout - either with its own Unit-typeName profile (Crusader Squad's Sword
    /// Brother/Initiate/Neophyte) or, when it carries none, falling back to the top-level
    /// selection's own Unit profile because every model in that loadout wrapper shares one
    /// datasheet-wide statline (Sword Brethren Squad's "Sword Brother" wrapper -> "Sword
    /// Brethren"). When there are no such nested "model" children at all, the top-level selection
    /// is itself a single/solo model (Helbrecht, Impulsor) and produces exactly one ModelLine.</summary>
    private static List<ModelLine> BuildModelLines(
        BsRosterSelection top, Action<string, Statline> addStatline, Action<BsRosterProfile> addWeaponProfile)
    {
        var loadoutChildren = top.Selections.Where(s => s.Type == "model").ToList();
        var modelLines = new List<ModelLine>();

        foreach (var node in loadoutChildren.Count == 0 ? [top] : loadoutChildren)
        {
            var statlineProfile = ResolveStatlineProfile(node, top);
            addStatline(statlineProfile.Name, MapStatline(statlineProfile, node, top));

            var (lineWeapons, lineAbilities) = CollectWeaponsAndAbilities(node, node.Number, addWeaponProfile);
            modelLines.Add(new ModelLine(statlineProfile.Name, lineWeapons, node.Number, lineAbilities));
        }

        return modelLines;
    }

    private static BsRosterProfile ResolveStatlineProfile(BsRosterSelection node, BsRosterSelection top) =>
        node.Profiles.FirstOrDefault(p => p.TypeName == "Unit")
        ?? top.Profiles.FirstOrDefault(p => p.TypeName == "Unit")
        ?? throw new BattleScribeRosterParseException(
            $"Selection '{top.Name}' has no resolvable Unit-typeName profile for loadout '{node.Name}'.");

    /// <summary>Walks a loadout node's own nested wargear <c>selections</c> recursively, collecting
    /// its per-model weapon name list (a weapon-carrying child's own <c>number</c> divided by
    /// <paramref name="modelCount"/> gives the per-model quantity - already-resolved, no partition
    /// inference needed, per battlescribe-roster-import's own requirement) and any wargear-granted
    /// Abilities (e.g. Impulsor's "Shield Dome" - OptionalGrant, mirroring the BSData pipeline's own
    /// classification for the equivalent shape). A selection carrying no <c>profiles</c> of its own
    /// (a pure grouping wrapper, e.g. "2 Storm Bolters") is walked one level deeper rather than
    /// skipped. An Enhancement-tagged selection (its own <c>costs["Enhancements"]</c>) is skipped
    /// entirely here - it's resolved once, separately, by <see cref="FindEnhancements"/>.</summary>
    private static (List<string> Weapons, List<Ability> Abilities) CollectWeaponsAndAbilities(
        BsRosterSelection node, int modelCount, Action<BsRosterProfile> addWeaponProfile)
    {
        var weapons = new List<string>();
        var abilities = new List<Ability>();
        Walk(node);
        return (weapons, abilities);

        void Walk(BsRosterSelection current)
        {
            foreach (var child in current.Selections)
            {
                if (IsEnhancement(child))
                    continue;

                var weaponProfiles = child.Profiles.Where(p => p.TypeName is "Ranged Weapons" or "Melee Weapons")
                    .ToList();
                var abilityProfiles = child.Profiles.Where(p => p.TypeName == "Abilities").ToList();

                if (weaponProfiles.Count > 0)
                {
                    foreach (var profile in weaponProfiles)
                        addWeaponProfile(profile);

                    var perModelCount = modelCount == 0 ? 0 : child.Number / modelCount;
                    for (var i = 0; i < perModelCount; i++)
                        weapons.AddRange(weaponProfiles.Select(p => p.Name));
                }
                else if (abilityProfiles.Count > 0)
                {
                    abilities.AddRange(abilityProfiles.Select(p => MapAbility(p, AbilityOrigin.OptionalGrant)));
                }
                else
                {
                    Walk(child);
                }
            }
        }
    }

    private static bool IsEnhancement(BsRosterSelection selection) =>
        selection.Costs.Any(c => string.Equals(c.Name, "Enhancements", StringComparison.OrdinalIgnoreCase));

    /// <summary>Finds every Enhancement selection anywhere in the top-level selection's own
    /// descendant tree (see battlescribe-roster-import's Enhancement Recognition requirement) -
    /// recognized structurally by its own <c>costs["Enhancements"]</c> tag, never by name or group
    /// path. A roster JSON only ever contains selections the player actually made, so - unlike the
    /// BSData text pipeline - this cannot leak an available-but-unselected Enhancement onto a Unit
    /// that never chose it.</summary>
    private static List<Ability> FindEnhancements(BsRosterSelection top)
    {
        var found = new List<Ability>();
        Walk(top);
        return found;

        void Walk(BsRosterSelection node)
        {
            foreach (var child in node.Selections)
            {
                if (IsEnhancement(child))
                {
                    var abilityProfile = child.Profiles.FirstOrDefault(p => p.TypeName == "Abilities");
                    if (abilityProfile is not null)
                        found.Add(MapAbility(abilityProfile, AbilityOrigin.Enhancement));
                }

                Walk(child);
            }
        }
    }

    private static Statline MapStatline(BsRosterProfile profile, BsRosterSelection node, BsRosterSelection top) =>
        new(
            M: ParseMeasurement(profile.CharacteristicText("M")),
            T: ParsePlainInt(profile.CharacteristicText("T")),
            Sv: ParseThreshold(profile.CharacteristicText("Sv")),
            W: ParsePlainInt(profile.CharacteristicText("W")),
            Ld: ParseThreshold(profile.CharacteristicText("LD")),
            Oc: ParsePlainInt(profile.CharacteristicText("OC")))
        {
            InSv = ResolveInvulnerableSave(profile.CharacteristicText("InSv"), node, top)
        };

    /// <summary>This format's own, simpler invulnerable-save caveat resolution (see design.md's
    /// Decisions - deferred detail, no BSData-style entryLink ancestry chain available here): a
    /// footnoted value's linked ability is searched for directly on the owning loadout node, then
    /// the top-level selection, under the same two BSData naming conventions
    /// ("Invulnerable Save ({digit}+*)" / "*Invulnerable Save") - unverified against a real
    /// caveated-InSv sample (none exists in the one roster analyzed so far; see PROGRESS.md).</summary>
    private static InvulnerableSave ResolveInvulnerableSave(string? text, BsRosterSelection node, BsRosterSelection top)
    {
        if (string.IsNullOrWhiteSpace(text)) return new InvulnerableSave();
        text = text.Trim();
        if (IsNotApplicable(text)) return new InvulnerableSave();

        var parenMatch = InSvParentheticalRegex().Match(text);
        if (parenMatch.Success)
        {
            var value = int.Parse(parenMatch.Groups[1].Value);
            var isRanged = parenMatch.Groups[2].Value.Equals("Ranged", StringComparison.OrdinalIgnoreCase);
            return isRanged
                ? new InvulnerableSave(meleeInSv: 0, rangedInSv: value, caveated: false, caveatAbility: null)
                : new InvulnerableSave(meleeInSv: value, rangedInSv: 0, caveated: false, caveatAbility: null);
        }

        if (text.Contains('/'))
        {
            var parts = text.Split('/').Select(p => p.Trim()).ToArray();
            if (parts.Length != 2)
                throw new BattleScribeRosterParseException($"Unexpected InSv text: '{text}'.");

            var left = InSvBareValueRegex().Match(parts[0]);
            var right = InSvBareValueRegex().Match(parts[1]);
            if (!left.Success || !right.Success)
                throw new BattleScribeRosterParseException($"Unexpected InSv text: '{text}'.");

            var leftFootnoted = left.Groups[2].Success;
            var rightFootnoted = right.Groups[2].Success;
            if (leftFootnoted == rightFootnoted)
                throw new BattleScribeRosterParseException($"Unexpected InSv text: '{text}'.");

            var footnotedDigit = int.Parse((leftFootnoted ? left : right).Groups[1].Value);
            var plainDigit = int.Parse((leftFootnoted ? right : left).Groups[1].Value);
            var ability = ResolveCaveatAbility(footnotedDigit, node, top, text);
            return new InvulnerableSave(plainDigit, plainDigit, caveated: true, caveatAbility: ability);
        }

        var bareMatch = InSvBareValueRegex().Match(text);
        if (!bareMatch.Success)
            throw new BattleScribeRosterParseException($"Unexpected InSv text: '{text}'.");

        var digit = int.Parse(bareMatch.Groups[1].Value);
        if (!bareMatch.Groups[2].Success)
            return new InvulnerableSave(digit, digit, caveated: false, caveatAbility: null);

        var caveatAbility = ResolveCaveatAbility(digit, node, top, text);
        return new InvulnerableSave(digit, digit, caveated: true, caveatAbility: caveatAbility);
    }

    private static Ability ResolveCaveatAbility(int digit, BsRosterSelection node, BsRosterSelection top,
        string rawText)
    {
        var expectedNames = new[] { $"Invulnerable Save ({digit}+*)", "*Invulnerable Save" };

        foreach (var candidate in new[] { node, top })
        {
            var match = candidate.Profiles.FirstOrDefault(p =>
                p.TypeName == "Abilities" &&
                expectedNames.Any(n => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)));
            if (match is not null)
                return MapAbility(match, AbilityOrigin.Intrinsic);
        }

        throw new BattleScribeRosterParseException(
            $"Could not resolve caveated invulnerable save ability for '{rawText}'.");
    }

    [GeneratedRegex(@"^(\d+)\+\s*\((Ranged|Melee)\)$")]
    private static partial Regex InSvParentheticalRegex();

    [GeneratedRegex(@"^(\d+)\+(\*)?$")]
    private static partial Regex InSvBareValueRegex();

    private static WeaponProfile MapWeaponProfile(BsRosterProfile profile)
    {
        var weapon = profile.TypeName == "Ranged Weapons"
            ? (WeaponProfile)new RangedWeapon(
                profile.Name,
                ParseMeasurement(profile.CharacteristicText("Range")),
                DiceExpression.Parse(profile.CharacteristicText("A") is { Length: > 0 } a ? a : "0"),
                ParseThreshold(profile.CharacteristicText("BS")),
                ParsePlainInt(profile.CharacteristicText("S")),
                ParsePlainInt(profile.CharacteristicText("AP")),
                DiceExpression.Parse(profile.CharacteristicText("D") is { Length: > 0 } d ? d : "0"))
            : new MeleeWeapon(
                profile.Name,
                DiceExpression.Parse(profile.CharacteristicText("A") is { Length: > 0 } a2 ? a2 : "0"),
                ParseThreshold(profile.CharacteristicText("WS")),
                ParsePlainInt(profile.CharacteristicText("S")),
                ParsePlainInt(profile.CharacteristicText("AP")),
                DiceExpression.Parse(profile.CharacteristicText("D") is { Length: > 0 } d2 ? d2 : "0"));

        return WeaponKeywordParser.Apply(weapon, profile.CharacteristicText("Keywords") is { Length: > 0 } k ? k : "-");
    }

    private static Ability MapAbility(BsRosterProfile profile, AbilityOrigin origin) =>
        new()
        {
            Name = profile.Name,
            Text = profile.CharacteristicText("Description") ?? "",
            Scope = AbilityScope.Unit,
            Origin = origin
        };

    private static Ability MapCoreRuleAbility(BsRosterRule rule, IReadOnlySet<string> knownArmyRuleNames) =>
        new()
        {
            Name = rule.Name,
            Text = rule.Description,
            Scope = AbilityScope.Unit,
            Origin = knownArmyRuleNames.Contains(rule.Name) ? AbilityOrigin.ArmyRule : AbilityOrigin.CoreRule
        };

    private static bool IsNotApplicable(string text) =>
        text == "-" || text.Equals("N/A", StringComparison.OrdinalIgnoreCase);

    private static int ParseMeasurement(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Trim();
        if (IsNotApplicable(text) || text.Equals("Melee", StringComparison.OrdinalIgnoreCase)) return 0;
        return int.Parse(text.TrimEnd('"', '+'));
    }

    private static int ParseThreshold(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Trim();
        return IsNotApplicable(text) ? 0 : int.Parse(text.TrimEnd('+'));
    }

    private static int ParsePlainInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Trim();
        return IsNotApplicable(text) ? 0 : int.Parse(text.TrimEnd('+'));
    }
}