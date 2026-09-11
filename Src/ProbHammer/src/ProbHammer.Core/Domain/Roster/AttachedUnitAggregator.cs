using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Builds the Attached Unit aggregate view: every distinct statline, weapon, and ability still in
/// play across a Unit's or AttachedUnit's present model-lines, recomputed live from casualty
/// state rather than cached.
/// </summary>
public static class AttachedUnitAggregator
{
    public static AttachedUnitAggregateView Build(ICombatUnit combatUnit, RuleClassificationBaseline baseline)
    {
        var presentLines = combatUnit.Components
            .SelectMany(unit => unit.ModelLines.Select(modelLine => (Unit: unit, ModelLine: modelLine)))
            .Where(x => x.ModelLine.RemainingCount > 0)
            .ToList();

        var abilities = BuildAbilities(combatUnit);
        var statlines = ResolveCaveatedInvulnerableSaves(BuildStatlines(combatUnit), baseline);
        statlines = ApplyStatlineFlagRules(statlines, abilities, baseline);

        return new AttachedUnitAggregateView(
            Name: combatUnit.Name,
            IsAttachedUnit: combatUnit is AttachedUnit,
            Statlines: statlines,
            Weapons: BuildWeapons(presentLines, abilities, baseline),
            Abilities: abilities,
            Keywords: KeywordResolution.EffectiveKeywords(combatUnit));
    }

    // A caveated Statline.InSv gets exactly one resolution attempt against the checked-in baseline,
    // via the same InvulnerableSaveEffectResolver ApplyInvulnerableSaveEffect (below) already uses
    // for an ordinary present ability. Deliberately narrow - scoped to InSv only, reading
    // Statline.InSv directly rather than joining through BuildAbilities' present-ability list -
    // since Datasheet's own exclusion of the InSv-caveat-internal ability names (see
    // Datasheet.IsExcludedFromGeneralAbilityWalk) already ensures that ability is never
    // independently "present" for ApplyStatlineFlagRules to also match: there is no
    // ability-presence collision left here to coordinate against, so ordering relative to
    // ApplyStatlineFlagRules doesn't matter for correctness. Placed before it only because "resolve
    // what's already known to need resolving, then apply ability-presence-driven flags" reads most
    // naturally.
    private static IReadOnlyList<AggregateStatlineEntry> ResolveCaveatedInvulnerableSaves(
        IReadOnlyList<AggregateStatlineEntry> statlines, RuleClassificationBaseline baseline) =>
        statlines.Select(entry =>
        {
            var insv = entry.Statline.InSv;
            if (!insv.IsCaveated)
                return entry;

            var sourceAbility = insv.ContributingAbilities[0];
            var normalizedText = RuleEffectClassifier.Normalize(sourceAbility.Text);
            if (!baseline.TryGet(normalizedText, out var baselineEntry))
                return entry;

            var effect = baselineEntry.Effects.OfType<InvulnerableSaveCharacteristicEffect>().FirstOrDefault();
            if (effect is null)
                return entry;

            var resolved = InvulnerableSaveEffectResolver.Resolve(effect, sourceAbility, insv);
            return entry with { Statline = entry.Statline with { InSv = resolved } };
        }).ToList();

    private static ScalarCharacteristicView GetScalarField(Statline statline, string characteristic) =>
        characteristic switch
        {
            "M" => statline.M,
            "T" => statline.T,
            "Sv" => statline.Sv,
            "W" => statline.W,
            "Ld" => statline.Ld,
            "Oc" => statline.Oc,
            _ => throw new InvalidOperationException($"Unrecognized characteristic '{characteristic}'.")
        };

    private static Statline SetScalarField(Statline statline, string characteristic, ScalarCharacteristicView value) =>
        characteristic switch
        {
            "M" => statline with { M = value },
            "T" => statline with { T = value },
            "Sv" => statline with { Sv = value },
            "W" => statline with { W = value },
            "Ld" => statline with { Ld = value },
            "Oc" => statline with { Oc = value },
            _ => throw new InvalidOperationException($"Unrecognized characteristic '{characteristic}'.")
        };

    // Runs after BuildStatlines/BuildAbilities produce their live, casualty-filtered results -
    // abilities is already filtered to only currently-present sources, so a matched entry's
    // liveness falls out for free with no separate tracking. Never mutates Datasheet/Unit; only the
    // returned decorated copy of the statline entries carries an effect. Looks up each present
    // ability's own normalized Text against the checked-in RuleClassificationBaseline - never
    // Name+Text, and never a live call to RuleEffectClassifier.Classify.
    private static IReadOnlyList<AggregateStatlineEntry> ApplyStatlineFlagRules(
        IReadOnlyList<AggregateStatlineEntry> statlines, IReadOnlyList<AggregateAbilityEntry> abilities,
        RuleClassificationBaseline baseline)
    {
        var matches = abilities
            .Select(a => (Entry: a, Matched: TryGetApplicableEntry(baseline, a.Ability)))
            .Where(x => x.Matched is not null)
            .ToList();

        if (matches.Count == 0)
            return statlines;

        return statlines.Select(entry =>
        {
            var applicable = matches
                .Where(m => IsBearerOf(m.Entry, m.Matched!.Target, entry.ComponentName, entry.StatlineName))
                .ToList();
            if (applicable.Count == 0)
                return entry;

            var mutated = entry.Statline;
            foreach (var (abilityEntry, baselineEntry) in applicable)
            foreach (var effect in baselineEntry!.Effects)
                mutated = ApplyEffect(mutated, effect, abilityEntry.Ability);

            return entry with { Statline = mutated };
        }).ToList();
    }

    // A baseline entry whose own classified target this capability can't yet apply (KeywordRuleTarget/
    // UnconditionalRuleTarget - no roster-wide predicate evaluation exists) produces no match at all,
    // the same outcome as an ability matching no baseline entry.
    private static RuleClassificationBaselineEntry? TryGetApplicableEntry(RuleClassificationBaseline baseline,
        Ability ability)
    {
        var normalizedText = RuleEffectClassifier.Normalize(ability.Text);
        return baseline.TryGet(normalizedText, out var entry) &&
               entry.Target is SelfRuleTarget or AttachedUnitRuleTarget
            ? entry
            : null;
    }

    // SelfRuleTarget applies to the matched ability's own (ComponentName, StatlineName): one specific
    // model-line when StatlineName is set, the whole component when it's null (a Datasheet-level or
    // Enhancement-sourced ability). AttachedUnitRuleTarget applies to every row regardless, since the
    // matched ability is already confirmed present on this ICombatUnit. Takes the raw bearer identity
    // rather than a full AggregateStatlineEntry so both the Statline call site (above) and
    // BuildWeapons' own weapon-contribution call site (below) can share this one check - a weapon
    // contribution carries the same (ComponentName, StatlineName) shape a statline entry does, just
    // not wrapped in that record.
    private static bool IsBearerOf(AggregateAbilityEntry abilityEntry, RuleTarget target,
        string componentName, string? statlineName)
    {
        if (target is AttachedUnitRuleTarget)
            return true;

        return abilityEntry.StatlineName is not null
            ? abilityEntry.ComponentName == componentName && abilityEntry.StatlineName == statlineName
            : abilityEntry.ComponentName == componentName;
    }

    // If two baseline-matched entries would both touch the same characteristic of the same statline
    // entry, the first applied wins and the second is skipped - "skip if the field already carries a
    // contributing ability", no separate accumulate logic. No real corpus example needs this today.
    //
    // A WeaponCharacteristicEffect is a real, expected case here, not an unrecognized one - a
    // present ability's baseline entry can carry it alongside (or instead of) a Statline-shaped
    // effect, and ApplyStatlineFlagRules' own TryGetApplicableEntry only filters by Target, not by
    // Effect subtype (deliberately - it doesn't know about weapon effects at all). Pre-existing bug
    // found while wiring resolve-weapon-characteristic-effects: 17 of the 19 real, checked-in
    // weapon-characteristic baseline entries are Self-targeted, so any roster carrying one of those
    // abilities already reached this switch's old default arm and threw
    // ArgumentOutOfRangeException, before this change existed - a WeaponCharacteristicEffect is
    // simply irrelevant to Statline resolution and must be skipped, not treated as an
    // unrecognized/error case; ResolveContributionProfile (BuildWeapons, below) is what actually
    // applies it.
    private static Statline ApplyEffect(Statline statline, CharacteristicEffect effect, Ability sourceAbility) =>
        effect switch
        {
            ScalarCharacteristicEffect scalar => ApplyScalarEffect(statline, scalar, sourceAbility),
            InvulnerableSaveCharacteristicEffect insv => ApplyInvulnerableSaveEffect(statline, insv, sourceAbility),
            WeaponCharacteristicEffect => statline,
            _ => throw new ArgumentOutOfRangeException(nameof(effect))
        };

    // The one missing piece of glue CharacteristicModificationResolver itself doesn't provide -
    // wraps its resolved raw CharacteristicValue back into a ScalarCharacteristicView,
    // preserving the true pre-mutation OriginalValue through a chain of mutations (never the field's
    // current effective Value, which may already reflect an earlier effect in this same pass).
    private static Statline ApplyScalarEffect(Statline statline, ScalarCharacteristicEffect effect,
        Ability sourceAbility)
    {
        var current = GetScalarField(statline, effect.Characteristic);
        if (current.ContributingAbilities.Count > 0)
            return statline;

        var resolvedValue = CharacteristicModificationResolver.Resolve(
            effect.Characteristic, current.Value, effect.Verb, effect.Amount);
        var resolved = ScalarCharacteristicView.Resolved(current.OriginalValue, resolvedValue, [sourceAbility]);
        return SetScalarField(statline, effect.Characteristic, resolved);
    }

    private static Statline ApplyInvulnerableSaveEffect(Statline statline, InvulnerableSaveCharacteristicEffect effect,
        Ability sourceAbility)
    {
        if (statline.InSv.ContributingAbilities.Count > 0)
            return statline;

        return statline with { InSv = InvulnerableSaveEffectResolver.Resolve(effect, sourceAbility, statline.InSv) };
    }

    // Component display order: an AttachedUnit's Attached units first, in their list order, then
    // the Bodyguard; a plain Unit is just itself. Shared by BuildStatlines and BuildAbilities so
    // both walk components in the same order.
    private static IReadOnlyList<Unit> ComponentDisplayOrder(ICombatUnit combatUnit) =>
        combatUnit is AttachedUnit attachedUnit
            ? [.. attachedUnit.Attached, attachedUnit.Bodyguard]
            : combatUnit.Components;

    // Walks components in display order, and within each component, its Datasheet's declared
    // Statlines in order. Matching each declared name against only that component's own
    // ModelLines (never another component's) is what makes per-component merge scoping fall out
    // for free - two components can never combine into one entry, and there's no separate sort
    // step to disagree with the grouping.
    private static IReadOnlyList<AggregateStatlineEntry> BuildStatlines(ICombatUnit combatUnit)
    {
        var components = ComponentDisplayOrder(combatUnit);

        var entries = new List<AggregateStatlineEntry>();
        foreach (var component in components)
        {
            foreach (var (statlineName, statline) in component.Datasheet.Statlines)
            {
                var lines = component.ModelLines
                    .Where(ml => string.Equals(ml.StatlineName, statlineName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (lines.Count == 0)
                    continue;

                var loadouts = lines
                    .Select(ml => new ModelLineLoadout(
                        WeaponsLabel: string.Join(", ", ml.Weapons),
                        Weapons: ml.Weapons,
                        RemainingCount: ml.RemainingCount,
                        InitialCount: ml.Count,
                        Abilities: ml.Abilities.Select(a => a.Name).ToList(),
                        DisplayName: ml.DisplayName))
                    .ToList();

                entries.Add(new AggregateStatlineEntry(
                    ComponentName: component.Datasheet.Name,
                    StatlineName: statlineName,
                    Statline: statline,
                    RemainingCount: lines.Sum(ml => ml.RemainingCount),
                    InitialCount: lines.Sum(ml => ml.Count),
                    Loadouts: loadouts));
            }
        }

        return entries;
    }

    // Groups weapons by structural profile equality (WeaponProfile.EqualityKey), aggregating a true
    // TotalAttacks (per contributing model-line: PerModelAttacks.Scale(RemainingCount), Add-reduced
    // across every contributor sharing the key) and retaining per-contribution provenance - the
    // aggregation concept ported from SimulationAdapter's weapon-group-by-equality-key algorithm.
    // Each contribution's own profile is resolved against applicable weapon-characteristic effects
    // (ResolveContributionProfile, below) BEFORE its EqualityKey is computed, so a mutation reaching
    // every current contributor of what would otherwise be one group re-merges into one entry, and a
    // mutation reaching only some of them splits those into their own entry - grouping itself needs
    // no new logic to do this (resolve-weapon-characteristic-effects design.md D5).
    private static IReadOnlyList<AggregateWeaponEntry> BuildWeapons(
        List<(Unit Unit, ModelLine ModelLine)> presentLines,
        IReadOnlyList<AggregateAbilityEntry> abilities, RuleClassificationBaseline baseline)
    {
        var groups = new Dictionary<WeaponProfileEqualityKey,
            (WeaponProfile Profile, DiceExpression TotalAttacks, List<WeaponContribution> Contributions)>();

        foreach (var (unit, modelLine) in presentLines)
        {
            foreach (var weaponName in modelLine.Weapons)
            {
                var baseProfile = unit.Datasheet.ResolveWeaponProfile(weaponName);
                var profile = ResolveContributionProfile(baseProfile, unit, modelLine, abilities, baseline);
                var unresolvedAbilities = FindUnresolvedAbilities(baseProfile, unit, modelLine, abilities, baseline);
                var key = profile.EqualityKey();

                var contribution = new WeaponContribution(
                    ComponentName: unit.Datasheet.Name,
                    StatlineName: modelLine.StatlineName,
                    Count: modelLine.RemainingCount,
                    PerModelAttacks: profile.A,
                    Name: profile.Name,
                    LoadoutIndex: LoadoutIndexOf(unit, modelLine),
                    UnresolvedAbilities: unresolvedAbilities);
                var scaledAttacks = profile.A.Scale(modelLine.RemainingCount);

                if (groups.TryGetValue(key, out var existing))
                {
                    existing.Contributions.Add(contribution);
                    groups[key] = (existing.Profile, existing.TotalAttacks.Add(scaledAttacks), existing.Contributions);
                }
                else
                {
                    groups[key] = (profile, scaledAttacks, [contribution]);
                }
            }
        }

        return groups.Values
            .Select(v =>
                new AggregateWeaponEntry(v.Profile, v.TotalAttacks, CompositeName(v.Contributions), v.Contributions,
                    UnresolvedAbilities: CompositeUnresolvedAbilities(v.Contributions)))
            .ToList();
    }

    // Applies every present, non-caveated, bearer-scoped, selector-matched WeaponCharacteristicEffect
    // to this contribution's own resolved profile before EqualityKey grouping runs (see BuildWeapons'
    // own comment). Reads the same abilities list BuildAbilities already assembles - no separate
    // per-component walk. An Attacks-characteristic effect is filtered out here rather than reaching
    // WeaponCharacteristicEffectResolver, which stays fail-loud for that case (design.md D3).
    private static WeaponProfile ResolveContributionProfile(
        WeaponProfile profile, Unit unit, ModelLine modelLine,
        IReadOnlyList<AggregateAbilityEntry> abilities, RuleClassificationBaseline baseline)
    {
        var applicableEffects = abilities
            .Select(a => (Entry: a, Matched: TryGetWeaponEffectEntry(baseline, a.Ability, isCaveated: false)))
            .Where(x => x.Matched is not null)
            .Where(x => IsBearerOf(x.Entry, x.Matched!.Target, unit.Datasheet.Name, modelLine.StatlineName))
            .SelectMany(x => x.Matched!.Effects.OfType<WeaponCharacteristicEffect>()
                .Where(e => e.Characteristic != "A")
                .Where(e => WeaponSelectorMatches(e.Selector, profile))
                .Select(e => (Effect: e, SourceAbility: x.Entry.Ability)));

        var resolved = profile;
        foreach (var (effect, sourceAbility) in applicableEffects)
            resolved = ApplyWeaponCharacteristicEffect(resolved, effect, sourceAbility);

        return resolved;
    }

    // Caveated-branch counterpart to ResolveContributionProfile (design.md D5): identical
    // IsBearerOf/WeaponSelectorMatches matching against the same unmutated base profile, admitting a
    // caveated baseline entry instead of a non-caveated one, and never mutating the profile - only
    // naming the source ability so a caveated match is still visible without evaluating the
    // activation condition this app has no mechanism for (attached-unit-tracker's "Aggregate Weapon
    // Count View" requirement).
    private static IReadOnlyList<Ability> FindUnresolvedAbilities(
        WeaponProfile profile, Unit unit, ModelLine modelLine,
        IReadOnlyList<AggregateAbilityEntry> abilities, RuleClassificationBaseline baseline)
    {
        return abilities
            .Select(a => (Entry: a, Matched: TryGetWeaponEffectEntry(baseline, a.Ability, isCaveated: true)))
            .Where(x => x.Matched is not null)
            .Where(x => IsBearerOf(x.Entry, x.Matched!.Target, unit.Datasheet.Name, modelLine.StatlineName))
            .Where(x => x.Matched!.Effects.OfType<WeaponCharacteristicEffect>()
                .Where(e => e.Characteristic != "A")
                .Any(e => WeaponSelectorMatches(e.Selector, profile)))
            .Select(x => x.Entry.Ability)
            .DistinctBy(a => (a.Name, a.Text))
            .ToList();
    }

    // A baseline entry's own weapon-characteristic Effects are only ever applied when its
    // classification is NOT caveated - deliberately diverges from TryGetApplicableEntry's own
    // Statline precedent above (which applies regardless of IsCaveated), since this family's
    // caveats are disproportionately real, unmodeled activation conditions rather than harmless
    // trailing flavor text (design.md D4). Same KeywordRuleTarget/UnconditionalRuleTarget exclusion
    // as the Statline case - no roster-wide predicate evaluation exists. isCaveated selects which
    // branch a caller wants: false for the applied-mutation path, true for the unresolved-reference
    // path (FindUnresolvedAbilities, above) - both read the identical Target-scoping rule.
    private static RuleClassificationBaselineEntry? TryGetWeaponEffectEntry(
        RuleClassificationBaseline baseline, Ability ability, bool isCaveated)
    {
        var normalizedText = RuleEffectClassifier.Normalize(ability.Text);
        return baseline.TryGet(normalizedText, out var entry) &&
               entry.IsCaveated == isCaveated &&
               entry.Target is SelfRuleTarget or AttachedUnitRuleTarget
            ? entry
            : null;
    }

    private static bool WeaponSelectorMatches(WeaponSelector selector, WeaponProfile profile) =>
        selector switch
        {
            AllWeapons => true,
            WeaponClass weaponClass => profile.Type == weaponClass.Type,
            NamedWeapon namedWeapon => string.Equals(
                profile.Name, namedWeapon.Name, StringComparison.OrdinalIgnoreCase),
            _ => throw new ArgumentOutOfRangeException(nameof(selector))
        };

    // First-applied-wins per field, mirroring ApplyScalarEffect's own collision rule for the
    // Statline case (design.md D7) - no real corpus example collides today.
    private static WeaponProfile ApplyWeaponCharacteristicEffect(
        WeaponProfile profile, WeaponCharacteristicEffect effect, Ability sourceAbility)
    {
        var current = GetWeaponScalarField(profile, effect.Characteristic);
        return current.ContributingAbilities.Count > 0
            ? profile
            : WeaponCharacteristicEffectResolver.Resolve(effect, sourceAbility, profile);
    }

    private static ScalarCharacteristicView GetWeaponScalarField(WeaponProfile profile, string characteristic) =>
        characteristic switch
        {
            "S" => profile.S,
            "AP" => profile.Ap,
            "D" => profile.D,
            _ => throw new InvalidOperationException($"Unrecognized weapon characteristic '{characteristic}'.")
        };

    // Order-preserving Distinct() (first-occurrence order) over the merged contributions' own
    // Names, then joined per the same 1/2/3+ Oxford-comma convention AttachedUnit.Name already
    // uses for its own composite-name case.
    private static string CompositeName(List<WeaponContribution> contributions)
    {
        var names = contributions.Select(c => c.Name).Distinct().ToList();
        return names.Count switch
        {
            1 => names[0],
            2 => $"{names[0]} and {names[1]}",
            _ => $"{string.Join(", ", names[..^1])}, and {names[^1]}"
        };
    }

    // Same order-preserving-distinct convention as CompositeName, but over every contribution's own
    // UnresolvedAbilities rather than its Name - no join/Oxford-comma step, since the render layer
    // needs the actual Ability list (for a popover trigger), not a display string.
    private static IReadOnlyList<Ability> CompositeUnresolvedAbilities(List<WeaponContribution> contributions) =>
        contributions.SelectMany(c => c.UnresolvedAbilities).DistinctBy(a => (a.Name, a.Text)).ToList();

    // Mirrors the same (unfiltered by RemainingCount) statline-name match BuildStatlines uses to
    // build a statline entry's Loadouts list, so this index always lines up with that ModelLine's
    // position in Loadouts - including when a dead sibling loadout still occupies an earlier slot.
    // -1 when the statline has only one ModelLine (BuildStatlines renders no Loadouts breakdown at
    // all in that case, so there is nothing to index).
    private static int LoadoutIndexOf(Unit unit, ModelLine modelLine)
    {
        var lines = unit.ModelLines
            .Where(ml => string.Equals(ml.StatlineName, modelLine.StatlineName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return lines.Count <= 1 ? -1 : lines.IndexOf(modelLine);
    }

    // Walks components in display order; for each present component, reports its Datasheet's own
    // Abilities, its own resolved Enhancements (both component-wide, StatlineName: null - an
    // Enhancement's Ability.Origin is what a renderer reads to show it distinctly, not this
    // record's own shape), and each of its present ModelLines' own Abilities (StatlineName: that
    // line's own name) - regardless of Scope, and with no cross-component combination or
    // deduplication. The explicit IsPresent guard is needed for both component-wide sources
    // (Datasheet.Abilities and Enhancements alike): unlike BuildStatlines, where a fully-dead
    // component naturally produces zero entries through its per-statline RemainingCount check,
    // neither has a statline-level gate of its own to fall through.
    private static IReadOnlyList<AggregateAbilityEntry> BuildAbilities(ICombatUnit combatUnit)
    {
        var entries = new List<AggregateAbilityEntry>();

        foreach (var component in ComponentDisplayOrder(combatUnit))
        {
            if (!component.IsPresent)
                continue;

            foreach (var ability in component.Datasheet.Abilities)
                entries.Add(new AggregateAbilityEntry(component.Datasheet.Name, StatlineName: null, ability));

            foreach (var ability in component.Enhancements)
                entries.Add(new AggregateAbilityEntry(component.Datasheet.Name, StatlineName: null, ability));

            foreach (var modelLine in component.ModelLines.Where(ml => ml.RemainingCount > 0))
            foreach (var ability in modelLine.Abilities)
                entries.Add(new AggregateAbilityEntry(component.Datasheet.Name, modelLine.StatlineName, ability));
        }

        return PromoteArmyRuleAbilities(entries);
    }

    /// <summary>An ArmyRule-origin ability (see AbilityOrigin.ArmyRule - a Core rule whose own
    /// gating is chapter/sub-faction exclusive, e.g. "Templar Vows"/"Oath of Moment") is an
    /// army-wide fact, never a per-component one - so it ALWAYS gets promoted to belong to no
    /// single component (<see cref="AggregateAbilityEntry.ComponentName"/> null,
    /// <see cref="AggregateAbilityEntry.ContributingComponentNames"/> listing every contributor),
    /// regardless of how many present components in THIS roster happen to reference it - even a
    /// standalone Unit's own single component. This is deliberately NOT "shared by 2+ components":
    /// that rule would only promote a shared army-wide ability within a multi-component
    /// AttachedUnit, leaving it as an ordinary per-component entry on a standalone Unit, which is
    /// exactly as much an army-wide fact there. Whether an ability
    /// gets this treatment is a structural property of the ability itself (its Origin), not a
    /// headcount of who happens to reference it in one particular roster. Multiple components
    /// referencing the same ArmyRule ability still collapse into one entry, same as before. Since
    /// this runs on a freshly-rebuilt list of only PRESENT components' abilities every request
    /// (this view is never cached), a component that dies simply stops contributing on the next
    /// rebuild - what makes the promoted entry's own collapse rule ("hidden once every
    /// contributing component is fully dead") fall out for free rather than needing separate
    /// tracking. Every other Origin (including CoreRule - a Core rule with no chapter exclusivity,
    /// e.g. "Deadly Demise") is untouched, exactly matching the existing "no cross-component
    /// combination" rule for everything else.</summary>
    private static IReadOnlyList<AggregateAbilityEntry> PromoteArmyRuleAbilities(List<AggregateAbilityEntry> entries)
    {
        var armyRuleGroups = entries
            .Where(e => e.Ability.Origin == AbilityOrigin.ArmyRule)
            .GroupBy(e => e.Ability.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var result = new List<AggregateAbilityEntry>();
        var promotedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (entry.Ability.Origin != AbilityOrigin.ArmyRule)
            {
                result.Add(entry);
                continue;
            }

            if (promotedNames.Add(entry.Ability.Name))
            {
                result.Add(entry with
                {
                    ComponentName = null,
                    ContributingComponentNames =
                    armyRuleGroups[entry.Ability.Name].Select(e => e.ComponentName!).ToList()
                });
            }
            // else: the promoted entry for this name was already added by an earlier contributor.
        }

        return result;
    }
}