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
            Weapons: BuildWeapons(presentLines),
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
            var applicable = matches.Where(m => IsBearer(m.Entry, m.Matched!.Target, entry)).ToList();
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
    // matched ability is already confirmed present on this ICombatUnit.
    private static bool IsBearer(AggregateAbilityEntry abilityEntry, RuleTarget target,
        AggregateStatlineEntry statlineEntry)
    {
        if (target is AttachedUnitRuleTarget)
            return true;

        return abilityEntry.StatlineName is not null
            ? abilityEntry.ComponentName == statlineEntry.ComponentName &&
              abilityEntry.StatlineName == statlineEntry.StatlineName
            : abilityEntry.ComponentName == statlineEntry.ComponentName;
    }

    // If two baseline-matched entries would both touch the same characteristic of the same statline
    // entry, the first applied wins and the second is skipped - "skip if the field already carries a
    // contributing ability", no separate accumulate logic. No real corpus example needs this today.
    private static Statline ApplyEffect(Statline statline, CharacteristicEffect effect, Ability sourceAbility) =>
        effect switch
        {
            ScalarCharacteristicEffect scalar => ApplyScalarEffect(statline, scalar, sourceAbility),
            InvulnerableSaveCharacteristicEffect insv => ApplyInvulnerableSaveEffect(statline, insv, sourceAbility),
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
                        InitialCount: ml.Count))
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
    private static IReadOnlyList<AggregateWeaponEntry> BuildWeapons(
        List<(Unit Unit, ModelLine ModelLine)> presentLines)
    {
        var groups = new Dictionary<WeaponProfileEqualityKey,
            (WeaponProfile Profile, DiceExpression TotalAttacks, List<WeaponContribution> Contributions)>();

        foreach (var (unit, modelLine) in presentLines)
        {
            foreach (var weaponName in modelLine.Weapons)
            {
                var profile = unit.Datasheet.ResolveWeaponProfile(weaponName);
                var key = profile.EqualityKey();

                var contribution = new WeaponContribution(
                    ComponentName: unit.Datasheet.Name,
                    StatlineName: modelLine.StatlineName,
                    Count: modelLine.RemainingCount,
                    PerModelAttacks: profile.A,
                    LoadoutIndex: LoadoutIndexOf(unit, modelLine));
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
            .Select(v => new AggregateWeaponEntry(v.Profile, v.TotalAttacks, v.Contributions))
            .ToList();
    }

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