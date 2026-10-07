using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Builds the Attached Unit aggregate view: every distinct statline, weapon, and ability still in
/// play across a Unit's or AttachedUnit's present model-lines, recomputed live from casualty
/// state rather than cached.
/// </summary>
public static class AttachedUnitAggregator
{
    public static AttachedUnitAggregateView Build(ICombatUnit combatUnit,
        AbilityClassificationCatalogue classifications)
    {
        var presentLines = combatUnit.Components
            .SelectMany(unit => unit.ModelLines.Select(modelLine => (Unit: unit, ModelLine: modelLine)))
            .Where(x => x.ModelLine.RemainingCount > 0)
            .ToList();

        var abilities = BuildAbilities(combatUnit);
        var matches = MatchAbilities(abilities, classifications, combatUnit.ConditionActivations);
        var statlines = ResolveCaveatedInvulnerableSaves(BuildStatlines(combatUnit), classifications);
        statlines = ApplyStatlineFlagRules(statlines, matches);

        return new AttachedUnitAggregateView(
            Name: combatUnit.Name,
            IsAttachedUnit: combatUnit is AttachedUnit,
            Statlines: statlines,
            Weapons: BuildWeapons(presentLines, matches),
            Abilities: abilities,
            Keywords: KeywordResolution.EffectiveKeywords(combatUnit),
            ActivatableConditions:
            BuildActivatableConditions(statlines, presentLines, matches, combatUnit.ConditionActivations));
    }

    // One toggle per (ability name, condition text) or (ability name, choice group) among conditional
    // effects that reach something on this unit, regardless of their current state.
    private static IReadOnlyList<ActivatableCondition> BuildActivatableConditions(
        IReadOnlyList<AggregateStatlineEntry> statlines, List<(Unit Unit, ModelLine ModelLine)> presentLines,
        IReadOnlyList<MatchedAbility> matches, ConditionActivations activations)
    {
        var result = new List<ActivatableCondition>();
        var seen = new HashSet<(string Ability, int? Group, string? Condition)>();

        foreach (var match in matches)
        {
            var ability = match.Entry.Ability;
            var activation = activations.For(ability.Name);
            foreach (var (effect, _) in match.Effects)
            {
                if (match.Classification.IsUnconditional(effect) ||
                    !Reaches(match, effect.Effect, statlines, presentLines))
                    continue;

                if (effect.ChoiceBranch is { } branch)
                {
                    if (branch.Group >= match.Classification.ChoiceGroups.Count ||
                        !seen.Add((ability.Name.ToUpperInvariant(), branch.Group, null)))
                        continue;

                    result.Add(new ChoiceToggle(ability, branch.Group, match.Classification.ChoiceGroups[branch.Group],
                        activation?.Choices.GetValueOrDefault(branch.Group) ?? new HashSet<int>()));
                }
                else
                {
                    var key = EffectStates.ConditionKey(effect);
                    if (!seen.Add((ability.Name.ToUpperInvariant(), null, key)))
                        continue;

                    result.Add(new ConditionToggle(ability, key, match.Classification.UsageLimit,
                        match.Classification.TurnOwnership, activation?.Conditions.Contains(key) == true));
                }
            }
        }

        return result;
    }

    private static bool Reaches(MatchedAbility match, RuleEffect effect,
        IReadOnlyList<AggregateStatlineEntry> statlines, List<(Unit Unit, ModelLine ModelLine)> presentLines) =>
        effect switch
        {
            ScalarCharacteristicEffect or InvulnerableSaveCharacteristicEffect => statlines.Any(s =>
                s.RemainingCount > 0 &&
                IsBearerOf(match.Entry, match.Classification.Target, s.ComponentName, s.StatlineName)),
            WeaponCharacteristicEffect { Characteristic: not ("S" or "A" or "AP" or "D" or "BS" or "WS") } => false,
            WeaponCharacteristicEffect or WeaponKeywordGrantEffect => presentLines.Any(x =>
                IsBearerOf(match.Entry, match.Classification.Target, x.Unit.Datasheet.Name,
                    x.ModelLine.StatlineName) &&
                x.ModelLine.Weapons.Any(w =>
                    WeaponEffectMatches(effect, x.Unit.Datasheet.ResolveWeaponProfile(w)))),
            _ => false
        };

    // A caveated Statline.InSv gets exactly one resolution attempt against the catalogue, via the
    // same InvulnerableSaveEffectResolver ApplyInvulnerableSaveEffect (below) already uses for an
    // ordinary present ability. Deliberately narrow - scoped to InSv only, reading Statline.InSv
    // directly rather than joining through BuildAbilities' present-ability list - since Datasheet's
    // own exclusion of the InSv-caveat-internal ability names (see
    // Datasheet.IsExcludedFromGeneralAbilityWalk) already ensures that ability is never
    // independently "present" for ApplyStatlineFlagRules to also match, so ordering relative to
    // ApplyStatlineFlagRules doesn't matter for correctness.
    private static IReadOnlyList<AggregateStatlineEntry> ResolveCaveatedInvulnerableSaves(
        IReadOnlyList<AggregateStatlineEntry> statlines, AbilityClassificationCatalogue classifications) =>
        statlines.Select(entry =>
        {
            var insv = entry.Statline.InSv;
            if (!insv.IsCaveated)
                return entry;

            var sourceAbility = insv.ContributingAbilities[0];
            if (!classifications.TryGet(sourceAbility.Text, out var classification))
                return entry;

            var effect = classification.UnconditionalEffects<InvulnerableSaveCharacteristicEffect>().FirstOrDefault();
            if (effect is null)
                return entry;

            var resolved = InvulnerableSaveEffectResolver.ResolveCaveat(effect, sourceAbility, insv);
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

    // A present ability whose classification applies here, each classified effect paired with its
    // state on this unit. Abilities is already filtered to currently-present sources, so a matched
    // entry's liveness falls out for free.
    private sealed record MatchedAbility(
        AggregateAbilityEntry Entry,
        AbilityClassification Classification,
        IReadOnlyList<(ClassifiedEffect Effect, EffectState State)> Effects);

    private static IReadOnlyList<MatchedAbility> MatchAbilities(IReadOnlyList<AggregateAbilityEntry> abilities,
        AbilityClassificationCatalogue classifications, ConditionActivations activations) =>
        abilities
            .Select(a => (Entry: a, Classification: TryGetApplicableClassification(classifications, a.Ability)))
            .Where(x => x.Classification is not null)
            .Select(x => new MatchedAbility(x.Entry, x.Classification!,
                x.Classification!.Effects
                    .Select(e => (e, EffectStates.Of(x.Classification, e, activations.For(x.Entry.Ability.Name))))
                    .ToList()))
            .ToList();

    // Never mutates Datasheet/Unit; only the returned decorated copy of the statline entries carries
    // an effect. Applied effects apply; NotApplied Scalar/InvulnerableSave effects reaching the same
    // entry are recorded instead.
    private static IReadOnlyList<AggregateStatlineEntry> ApplyStatlineFlagRules(
        IReadOnlyList<AggregateStatlineEntry> statlines, IReadOnlyList<MatchedAbility> matches)
    {
        if (matches.Count == 0)
            return statlines;

        return statlines.Select(entry =>
        {
            var applicable = matches
                .Where(m => IsBearerOf(m.Entry, m.Classification.Target, entry.ComponentName, entry.StatlineName))
                .ToList();
            if (applicable.Count == 0)
                return entry;

            var mutated = entry.Statline;
            var notApplied = new List<NotAppliedStatlineEffect>();
            foreach (var match in applicable)
            {
                foreach (var (effect, _) in match.Effects.Where(e => e.State == EffectState.Applied))
                    mutated = ApplyEffect(mutated, effect.Effect, match.Entry.Ability);

                notApplied.AddRange(match.Effects
                    .Where(e => e.State == EffectState.NotApplied &&
                                e.Effect.Effect is ScalarCharacteristicEffect or InvulnerableSaveCharacteristicEffect)
                    .Select(e => new NotAppliedStatlineEffect(
                        match.Entry.Ability, e.Effect.Effect, EffectCondition.Of(match.Classification, e.Effect))));
            }

            return entry with { Statline = mutated, NotAppliedEffects = notApplied };
        }).ToList();
    }

    // A classification whose own target this capability can't yet apply (KeywordRuleTarget/
    // UnconditionalRuleTarget - no roster-wide predicate evaluation exists) produces no match at all,
    // the same outcome as an unclassified ability - EXCEPT a whole-unit-scoped origin (statline-flag-
    // rules' Target-Scoped Application exceptions; see IsWholeUnitScopedOrigin). Shared by the
    // Statline and weapon paths.
    private static AbilityClassification? TryGetApplicableClassification(
        AbilityClassificationCatalogue classifications, Ability ability) =>
        classifications.TryGet(ability.Text, out var classification) &&
        (classification.Target is SelfRuleTarget or AttachedUnitRuleTarget || IsWholeUnitScopedOrigin(ability))
            ? classification
            : null;

    // A DetachmentRule ability's keyword target was already evaluated against the roster by
    // DetachmentRuleInboundAbilityResolver; an ArmyRule ability is only present on a unit that carries it.
    private static bool IsWholeUnitScopedOrigin(Ability ability) =>
        ability.Origin is AbilityOrigin.DetachmentRule or AbilityOrigin.ArmyRule;

    // SelfRuleTarget applies to the matched ability's own (ComponentName, StatlineName): one specific
    // model-line when StatlineName is set, the whole component when it's null (a Datasheet-level or
    // Enhancement-sourced ability). AttachedUnitRuleTarget applies to every row regardless, since the
    // matched ability is already confirmed present on this ICombatUnit - a whole-unit-scoped origin
    // (DetachmentRule/ArmyRule) resolves the same unconditional way, whatever its own classified
    // Target, since its eligibility was already decided before it became present here. Takes the raw
    // bearer identity rather than a full AggregateStatlineEntry so both the Statline call site
    // (above) and BuildWeapons' own weapon-contribution call site (below) can share this one check -
    // a weapon contribution carries the same (ComponentName, StatlineName) shape a statline entry
    // does, just not wrapped in that record.
    private static bool IsBearerOf(AggregateAbilityEntry abilityEntry, RuleTarget target,
        string componentName, string? statlineName)
    {
        if (target is AttachedUnitRuleTarget || IsWholeUnitScopedOrigin(abilityEntry.Ability))
            return true;

        return abilityEntry.StatlineName is not null
            ? abilityEntry.ComponentName == componentName && abilityEntry.StatlineName == statlineName
            : abilityEntry.ComponentName == componentName;
    }

    // If two matched classifications would both touch the same characteristic of the same statline
    // entry, the first applied wins and the second is skipped - "skip if the field already carries a
    // contributing ability", no separate accumulate logic. Every other effect kind leaves the
    // Statline alone; weapon effects are applied by BuildWeapons.
    private static Statline ApplyEffect(Statline statline, RuleEffect effect, Ability sourceAbility) =>
        effect switch
        {
            ScalarCharacteristicEffect scalar => ApplyScalarEffect(statline, scalar, sourceAbility),
            InvulnerableSaveCharacteristicEffect insv => ApplyInvulnerableSaveEffect(statline, insv, sourceAbility),
            _ => statline
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

        return statline with { InSv = InvulnerableSaveEffectResolver.Merge(effect, sourceAbility, statline.InSv) };
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
        List<(Unit Unit, ModelLine ModelLine)> presentLines, IReadOnlyList<MatchedAbility> matches)
    {
        var groups = new Dictionary<WeaponProfileEqualityKey,
            (WeaponProfile Profile, DiceExpression TotalAttacks, List<WeaponContribution> Contributions)>();

        foreach (var (unit, modelLine) in presentLines)
        {
            foreach (var weaponName in modelLine.Weapons)
            {
                var baseProfile = unit.Datasheet.ResolveWeaponProfile(weaponName);
                var (profile, keywordGrants) =
                    ResolveContributionProfile(baseProfile, unit, modelLine, matches);
                var notAppliedEffects =
                    FindNotAppliedEffects(baseProfile, unit, modelLine, matches);
                var notAppliedKeywordGrants =
                    FindNotAppliedKeywordGrants(profile, unit, modelLine, matches);
                var attacksContributions =
                    ResolveAttacksContributions(baseProfile, unit, modelLine, matches);
                var key = profile.EqualityKey();

                var contribution = new WeaponContribution(
                    ComponentName: unit.Datasheet.Name,
                    StatlineName: modelLine.StatlineName,
                    Count: modelLine.RemainingCount,
                    PerModelAttacks: profile.A,
                    Name: profile.Name,
                    LoadoutIndex: LoadoutIndexOf(unit, modelLine),
                    NotAppliedEffects: notAppliedEffects,
                    AttacksContributions: attacksContributions,
                    KeywordGrants: keywordGrants,
                    NotAppliedKeywordGrants: notAppliedKeywordGrants);
                var scaledAttacks =
                    (profile.A + attacksContributions.Sum(c => c.Amount)).Scale(modelLine.RemainingCount);

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
                    NotAppliedEffects: CompositeNotAppliedEffects(v.Contributions),
                    KeywordGrants: v.Contributions.SelectMany(c => c.KeywordGrants)
                        .DistinctBy(g => (g.SourceAbility.Name, g.SourceAbility.Text, g.Keyword)).ToList(),
                    NotAppliedKeywordGrants: v.Contributions.SelectMany(c => c.NotAppliedKeywordGrants)
                        .DistinctBy(g => (g.SourceAbility.Name, g.SourceAbility.Text, g.Keyword)).ToList()))
            .ToList();
    }

    // Applies every present, unconditional, bearer-scoped, selector-matched WeaponCharacteristicEffect
    // and WeaponKeywordGrantEffect to this contribution's own resolved profile before EqualityKey
    // grouping runs (see BuildWeapons' own comment), returning the grants that changed its keywords.
    // An Attacks-characteristic effect is filtered out here - it's handled entirely by
    // ResolveAttacksContributions' own separate path instead, never by
    // WeaponCharacteristicEffectResolver, which stays fail-loud for that case.
    private static (WeaponProfile Profile, IReadOnlyList<KeywordGrant> KeywordGrants) ResolveContributionProfile(
        WeaponProfile profile, Unit unit, ModelLine modelLine,
        IReadOnlyList<MatchedAbility> matches)
    {
        var applicableEffects = MatchedWeaponEffects<WeaponCharacteristicEffect>(
                profile, unit, modelLine, matches, EffectState.Applied)
            .Where(x => x.Effect.Characteristic is "S" or "AP" or "D" or "BS" or "WS");

        var resolved = profile;
        foreach (var (effect, sourceAbility, _) in applicableEffects)
            resolved = ApplyWeaponCharacteristicEffect(resolved, effect, sourceAbility);

        var keywordGrants = new List<KeywordGrant>();
        foreach (var (grant, sourceAbility, _) in MatchedWeaponEffects<WeaponKeywordGrantEffect>(
                     profile, unit, modelLine, matches, EffectState.Applied))
        {
            var result = WeaponKeywordGrantResolver.Apply(resolved.KeywordsText, grant.Keyword);
            if (!result.Changed)
                continue;

            resolved = resolved with { KeywordsText = result.Keywords };
            keywordGrants.Add(new KeywordGrant(sourceAbility, result.Added!, result.Replaced));
        }

        return (resolved, keywordGrants);
    }

    // Sibling to ResolveContributionProfile, for the Attacks characteristic specifically: collects a
    // list of signed per-model deltas instead of mutating a WeaponProfile field - there is no
    // WeaponProfile.A ScalarCharacteristicView field to mutate, and folding several abilities'
    // amounts into one resolved value would discard the per-ability attribution the render layer
    // needs.
    private static IReadOnlyList<AttacksContribution> ResolveAttacksContributions(
        WeaponProfile profile, Unit unit, ModelLine modelLine,
        IReadOnlyList<MatchedAbility> matches) =>
        MatchedWeaponEffects<WeaponCharacteristicEffect>(
                profile, unit, modelLine, matches, EffectState.Applied)
            .Where(x => x.Effect.Characteristic == "A")
            .Select(x => new AttacksContribution(
                x.SourceAbility, CharacteristicModificationResolver.ResolveAttacksAmount(x.Effect)))
            .ToList();

    // Conditional counterpart to the two methods above: never mutates the profile, only records
    // what the effect would do so it stays visible without evaluating its condition.
    private static IReadOnlyList<NotAppliedWeaponEffect> FindNotAppliedEffects(
        WeaponProfile profile, Unit unit, ModelLine modelLine,
        IReadOnlyList<MatchedAbility> matches) =>
        MatchedWeaponEffects<WeaponCharacteristicEffect>(
                profile, unit, modelLine, matches, EffectState.NotApplied)
            .Where(x => x.Effect.Characteristic is "S" or "AP" or "D" or "A" or "BS" or "WS")
            .Select(x => new NotAppliedWeaponEffect(x.SourceAbility, x.Effect.Characteristic, x.Effect.Verb,
                x.Effect.Verb == EffectVerb.Set
                    ? x.Effect.Amount
                    : CharacteristicModificationResolver.ResolveDelta(
                        CharacteristicModificationKinds.Of(x.Effect.Characteristic), x.Effect.Verb, x.Effect.Amount),
                x.Condition))
            .DistinctBy(e => (e.SourceAbility.Name, e.SourceAbility.Text, e.Characteristic))
            .ToList();

    // Conditional grants that would still change the contribution's already-resolved keywords; one a
    // native keyword (or an applied grant) already covers is dropped rather than shown.
    private static IReadOnlyList<NotAppliedKeywordGrant> FindNotAppliedKeywordGrants(
        WeaponProfile resolvedProfile, Unit unit, ModelLine modelLine,
        IReadOnlyList<MatchedAbility> matches) =>
        MatchedWeaponEffects<WeaponKeywordGrantEffect>(
                resolvedProfile, unit, modelLine, matches, EffectState.NotApplied)
            .Select(x => (x.SourceAbility, x.Condition,
                Result: WeaponKeywordGrantResolver.Apply(resolvedProfile.KeywordsText, x.Effect.Keyword)))
            .Where(x => x.Result.Changed)
            .Select(x => new NotAppliedKeywordGrant(x.SourceAbility, x.Result.Added!, x.Condition))
            .DistinctBy(g => (g.SourceAbility.Name, g.SourceAbility.Text, g.Keyword))
            .ToList();

    // Every TEffect weapon effect in the given state of a matched ability, reaching this contribution
    // and matching its profile.
    private static IEnumerable<(TEffect Effect, Ability SourceAbility, EffectCondition Condition)>
        MatchedWeaponEffects<TEffect>(
            WeaponProfile profile, Unit unit, ModelLine modelLine, IReadOnlyList<MatchedAbility> matches,
            EffectState state) where TEffect : RuleEffect =>
        matches
            .Where(m => IsBearerOf(m.Entry, m.Classification.Target, unit.Datasheet.Name, modelLine.StatlineName))
            .SelectMany(m => m.Effects
                .Where(e => e.State == state)
                .Where(e => e.Effect.Effect is TEffect && WeaponEffectMatches(e.Effect.Effect, profile))
                .Select(e => (Effect: (TEffect)e.Effect.Effect, SourceAbility: m.Entry.Ability,
                    Condition: EffectCondition.Of(m.Classification, e.Effect))));

    private static WeaponSelector SelectorOf(RuleEffect effect) =>
        effect switch
        {
            WeaponCharacteristicEffect characteristic => characteristic.Selector,
            WeaponKeywordGrantEffect grant => grant.Selector,
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "Not a weapon effect.")
        };

    private static bool WeaponEffectMatches(RuleEffect effect, WeaponProfile profile) =>
        WeaponSelectorMatches(SelectorOf(effect), profile) &&
        (effect is not WeaponCharacteristicEffect characteristic ||
         WeaponCharacteristicEffectResolver.SkillEffectApplies(characteristic.Characteristic, profile));

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
            "BS" or "WS" => profile.Skill,
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

    private static IReadOnlyList<NotAppliedWeaponEffect> CompositeNotAppliedEffects(
        List<WeaponContribution> contributions) =>
        contributions.SelectMany(c => c.NotAppliedEffects)
            .DistinctBy(e => (e.SourceAbility.Name, e.SourceAbility.Text, e.Characteristic))
            .ToList();

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

        var result = PromoteArmyRuleAbilities(entries).ToList();

        // InboundAbilities is a fact about the whole combat unit, not any one component - reported
        // once each, belonging to no single component (the same slot an Army Rule promotion
        // occupies), but only while some component is still present (mirrors PromoteArmyRuleAbilities'
        // own liveness rule, since this source has no per-component IsPresent gate of its own).
        // ContributingComponentNames is set to every one of the combat unit's own component names
        // (not just currently-present ones) rather than left empty - LivePlayModel.
        // BuildWholeUnitAbilitySpans (the page layer's own separate liveness check for this same
        // ComponentName-null shape) derives IsFullyDead by filtering statline blocks down to this
        // list and calling .All() on the result; an empty list makes that filter match nothing,
        // and .All() on an empty sequence is vacuously true - which wrongly marked this entry
        // data-dead/collapsed even while every component was alive. Listing every component here
        // makes that check correctly require every component of the whole unit to be dead first.
        if (combatUnit.Components.Any(c => c.IsPresent))
            result.AddRange(combatUnit.InboundAbilities.Select(ability =>
                new AggregateAbilityEntry(ComponentName: null, StatlineName: null, ability,
                    ContributingComponentNames: combatUnit.Components.Select(c => c.Datasheet.Name).ToList())));

        return result;
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