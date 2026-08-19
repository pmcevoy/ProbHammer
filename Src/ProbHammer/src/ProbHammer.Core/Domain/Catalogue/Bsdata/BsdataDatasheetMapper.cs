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
            Sv: ParseThreshold(profile.CharacteristicText("Sv")),
            W: ParsePlainInt(profile.CharacteristicText("W")),
            Ld: ParseThreshold(profile.CharacteristicText("LD")),
            Oc: ParsePlainInt(profile.CharacteristicText("OC")))
        {
            InSv = ParseThreshold(profile.CharacteristicText("InSv"))
        };

    private static WeaponProfile MapRangedWeapon(BsProfile profile)
    {
        var weapon = new RangedWeapon(
            profile.Name,
            ParseMeasurement(profile.CharacteristicText("Range")),
            DiceExpression.Parse(profile.CharacteristicText("A") ?? "0"),
            ParseThreshold(profile.CharacteristicText("BS")),
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
            ParseThreshold(profile.CharacteristicText("WS")),
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

    /// <summary>Strips a trailing "+" threshold marker (e.g. 2+, 6+) and parses the number. An
    /// empty string (BSData's "no invulnerable save" representation), "N/A" (BSData's
    /// no-hit-roll-required marker on e.g. a Torrent weapon's BS/WS), or "-" (not applicable)
    /// parses as 0. A trailing "*" (a footnoted/conditional save, e.g. "5+*") is stripped along
    /// with the "+", dropping the footnote rather than failing to parse. A value describing two
    /// different saves depending on context - either split with "/" (e.g. "4+* / 5+", confirmed on
    /// Wyches and Howling Banshees) or annotated with a parenthetical (e.g. "5+ (Ranged)",
    /// confirmed on the four Titan datasheets) - has no representation in Statline's single-int
    /// Sv/InSv fields; only the first/unparenthesized value is kept, and the conditional-context
    /// nuance is a known, accepted loss until something downstream actually needs to represent
    /// it.</summary>
    private static int ParseThreshold(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Trim();
        if (IsNotApplicable(text) || IsNA(text)) return 0;
        var firstValue = text.Split('/', '(')[0].Trim();
        return int.Parse(firstValue.TrimEnd('+', '*'));
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
