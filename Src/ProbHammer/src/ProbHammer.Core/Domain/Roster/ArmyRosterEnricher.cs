using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Resolves a <see cref="ParsedArmyList"/> (army-list-parsing) against a
/// <see cref="ResolvedBsdataCatalogue"/> (already resolved for the army's faction - see
/// <see cref="BsdataFactionResolver"/> and the app-wide catalogue cache in ProbHammer.Web) to build
/// a live <see cref="ArmyRoster"/>. Every name resolution is eager (validated during the walk, not
/// deferred to later aggregation) so a resolution failure surfaces here with a diagnostic naming
/// the unresolved text, per army-roster-enrichment's "Unit and Weapon Name Resolution" requirement -
/// wargear/statline names are still stored on <see cref="ModelLine"/> as plain strings afterward,
/// matching that type's existing by-name (not by-reference) convention.
/// </summary>
public static class ArmyRosterEnricher
{
    public static ArmyRoster Enrich(ParsedArmyList parsedArmyList, ResolvedBsdataCatalogue catalogue)
    {
        var knownArmyRuleNames = ArmyRuleNameLookup.Resolve(parsedArmyList.Faction);

        var units = new List<ICombatUnit>();
        units.AddRange(
            parsedArmyList.AttachmentGroups.Select(g => BuildAttachedUnit(g, catalogue, knownArmyRuleNames)));
        units.AddRange(parsedArmyList.StandaloneUnits.Select(u => BuildUnit(u, catalogue, knownArmyRuleNames)));

        var detachments = parsedArmyList.Detachments
            .SelectMany(text => DetachmentNameResolver.Resolve(text, catalogue))
            .ToList();

        return new ArmyRoster(
            parsedArmyList.Name,
            parsedArmyList.PointsSpent,
            parsedArmyList.Faction,
            detachments,
            parsedArmyList.ForceDisposition,
            parsedArmyList.BattleSize,
            parsedArmyList.PointsLimit,
            units);
    }

    private static AttachedUnit BuildAttachedUnit(
        ParsedAttachmentGroup group, ResolvedBsdataCatalogue catalogue, IReadOnlySet<string> knownArmyRuleNames) =>
        new(BuildUnit(group.Bodyguard, catalogue, knownArmyRuleNames),
            group.Attached.Select(u => BuildUnit(u, catalogue, knownArmyRuleNames)));

    private static Unit BuildUnit(ParsedUnit parsedUnit, ResolvedBsdataCatalogue catalogue,
        IReadOnlySet<string> knownArmyRuleNames)
    {
        var unitName = BsdataNameNormalization.Normalize(parsedUnit.Name);
        var datasheet = catalogue.ResolveDatasheet(unitName, knownArmyRuleNames);
        var modelLines = parsedUnit.ModelGroups.Select(g => BuildModelLine(g, datasheet)).ToList();
        var enhancements = ResolveEnhancements(parsedUnit.Enhancements, datasheet);

        return new Unit(datasheet, enhancements, modelLines);
    }

    /// <summary>Resolves each parsed Enhancement name against the Datasheet's on-demand ability
    /// index (per datasheet-catalogue's On-Demand Ability Resolution) - the same "resolve by name,
    /// fail loud with a did-you-mean diagnostic" contract as every other name resolution in this
    /// file. Deliberately does not check that a resolved ability's Origin is actually Enhancement
    /// (vs. some other optional grant) - that would be new eligibility-checking behavior this
    /// project has consistently declined to add elsewhere (see resolve-enhancement-abilities'
    /// design.md). An empty Enhancements list resolves to an empty result - no Enhancement is ever
    /// attached merely because the Datasheet defines one available.</summary>
    private static IReadOnlyList<Ability> ResolveEnhancements(IReadOnlyList<string> enhancementNames,
        Datasheet datasheet) =>
        enhancementNames
            .Select(BsdataNameNormalization.Normalize)
            .Select(name =>
            {
                if (datasheet.TryResolveAbility(name, out var ability))
                    return ability;

                var suggestion = BsdataNameSuggestion.FindClosest(name, datasheet.OptionalAbilityNames);
                var message = suggestion is null
                    ? $"Datasheet '{datasheet.Name}' has no Enhancement named '{name}'."
                    : $"Datasheet '{datasheet.Name}' has no Enhancement named '{name}'. Did you mean '{suggestion}'?";
                throw new BsdataNameResolutionException(name, message);
            })
            .ToList();

    private static ModelLine BuildModelLine(ParsedModelGroup group, Datasheet datasheet)
    {
        var statlineName = ResolveStatlineName(BsdataNameNormalization.Normalize(group.ModelName), datasheet);
        var weapons = new List<string>();
        var abilities = new List<Ability>();

        foreach (var itemName in group.Weapons.Select(BsdataNameNormalization.Normalize))
            ResolveWargearItem(itemName, datasheet, weapons, abilities);

        return new ModelLine(statlineName, weapons, group.Count, abilities);
    }

    /// <summary>Resolves a parsed model sub-group's own name to one of the Datasheet's actual
    /// Statline names. Tries an exact match first (the common case - see design.md's "Model-line
    /// count-splitting is arithmetic over the export's own weapon counts, not a BSData name-match"),
    /// then two independent fallbacks, both confirmed necessary against the live BSData clone (one
    /// does not subsume the other - a Datasheet can be multi-statline with one statline named after
    /// the whole unit, or single-statline with an arbitrarily decorated name, and these are genuinely
    /// different real shapes):
    ///   1. The Datasheet's own name, when it's itself one of several Statline names (confirmed:
    ///      Scout Squad, Legionaries, Eradicator Squad - every troop model shares the squad's own
    ///      name as its Unit profile name, alongside a separately-named Sergeant/Champion statline).
    ///   2. The Datasheet's sole Statline, when it has exactly one (confirmed: Emperor's Champion ->
    ///      "The Emperor's Champion", Impulsor -> "Black Templars Impulsor", Ancient -> "Primaris
    ///      Ancient", Sword Brethren Squad -> "Sword Brethren" - none of these match the export label
    ///      or the Datasheet's own name, but with only one Statline in play there is no ambiguity
    ///      about what a model group must refer to regardless of what it's actually called).
    /// Throws with a "did you mean...?" suggestion when none of these resolve.</summary>
    private static string ResolveStatlineName(string modelName, Datasheet datasheet)
    {
        if (datasheet.TryGetStatline(modelName, out _))
            return modelName;

        if (datasheet.TryGetStatline(datasheet.Name, out _))
            return datasheet.Name;

        if (datasheet.Statlines.Count == 1)
            return datasheet.Statlines[0].Name;

        var suggestion = BsdataNameSuggestion.FindClosest(modelName, datasheet.Statlines.Select(s => s.Name));
        var message = suggestion is null
            ? $"Datasheet '{datasheet.Name}' has no statline named '{modelName}'."
            : $"Datasheet '{datasheet.Name}' has no statline named '{modelName}'. Did you mean '{suggestion}'?";
        throw new BsdataNameResolutionException(modelName, message);
    }

    /// <summary>Resolves one exported wargear name against a Datasheet, appending to
    /// <paramref name="weapons"/> or <paramref name="abilities"/> as appropriate. Tries an exact
    /// weapon-profile match first, then a multi-profile expansion: some real wargear choices
    /// (confirmed against the live BSData clone: High Marshal Helbrecht's "Sword of the High
    /// Marshals") resolve to more than one weapon profile, each named "{leading marker}{ExportedName}
    /// - {mode}" (e.g. "➤ Sword of the High Marshals - Sweep" / "- Strike") rather than the bare
    /// exported name - every such profile is included. Then a Datasheet Ability match: some real
    /// wargear choices aren't weapons at all (confirmed against the live BSData clone: Impulsor's
    /// "Shield Dome", a BSData "Abilities"-typeName profile granting an invulnerable save) - the
    /// export text lists these identically to a weapon selection, so a name that resolves as an
    /// Ability instead is attached to this model-line's own Abilities rather than treated as an
    /// unresolved weapon. Throws with a "did you mean...?" suggestion when none of these
    /// resolve.</summary>
    private static void ResolveWargearItem(string itemName, Datasheet datasheet, List<string> weapons,
        List<Ability> abilities)
    {
        if (datasheet.TryResolveWeaponProfile(itemName, out _))
        {
            weapons.Add(itemName);
            return;
        }

        var prefix = $"{itemName} - ";
        var multiProfileMatches = datasheet.WeaponNames
            .Where(n => StripLeadingMarker(n).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (multiProfileMatches.Count > 0)
        {
            weapons.AddRange(multiProfileMatches);
            return;
        }

        if (datasheet.TryResolveAbility(itemName, out var ability))
        {
            abilities.Add(ability);
            return;
        }

        var suggestion = BsdataNameSuggestion.FindClosest(itemName, datasheet.WeaponNames);
        var message = suggestion is null
            ? $"Datasheet '{datasheet.Name}' has no weapon profile named '{itemName}'."
            : $"Datasheet '{datasheet.Name}' has no weapon profile named '{itemName}'. Did you mean '{suggestion}'?";
        throw new BsdataNameResolutionException(itemName, message);
    }

    private static string StripLeadingMarker(string name) =>
        name.StartsWith("➤ ", StringComparison.Ordinal) ? name[2..] : name;
}