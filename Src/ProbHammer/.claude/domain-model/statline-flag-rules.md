# Statline-Flag Rules

Full requirements: `openspec/changes/resolve-known-ability-effects/` (original hand-authored
mechanism), `apply-rule-effect-baseline/` (generalized to a classified vocabulary) and
`adopt-llm-ability-classifications/` (current: driven by the ability-classification catalogue - see
ability-classification-catalogue.md). Recognizes a rule/ability by the content hash of its own
text (never its Name) and derives a flagged Statline-characteristic value for display on the
*specific resolved unit* it's currently present on — never mutating the shared `Datasheet`/`Unit`
themselves. The matched source ability always keeps rendering normally in the unit's
Abilities/Enhancements listing alongside the derived value; this mechanism only ever adds a value,
never removes or hides anything.

```
AttachedUnitAggregator.ApplyStatlineFlagRules(statlines, abilities, classifications)
                                       // looks up each present AggregateAbilityEntry's own Ability
                                       // .Text in the catalogue - a classification whose Target is
                                       // SelfRuleTarget or AttachedUnitRuleTarget is applicable
                                       // (TryGetStatlineClassification); KeywordRuleTarget/
                                       // UnconditionalRuleTarget produce no match, the same outcome
                                       // as no classification at all - EXCEPT a DetachmentRule-
                                       // origin ability, whose keyword target was already evaluated
                                       // by DetachmentRuleInboundAbilityResolver. Only the
                                       // classification's unconditional effects apply.
IsBearerOf(abilityEntry, target, componentName, statlineName) -> bool
                                       // SelfRuleTarget - the matched ability's own bearer row(s),
                                       // one specific model-line when AggregateAbilityEntry
                                       // .StatlineName is set, the whole owning component when it's
                                       // null (a Datasheet-level or Enhancement-sourced ability);
                                       // AttachedUnitRuleTarget - every row of the whole
                                       // ICombatUnit regardless of which component granted it.
ApplyEffect(statline, effect, sourceAbility) -> Statline
                                       // ScalarCharacteristicEffect -> ApplyScalarEffect;
                                       // InvulnerableSaveCharacteristicEffect ->
                                       // ApplyInvulnerableSaveEffect (InvulnerableSaveEffectResolver);
                                       // every other effect kind leaves the Statline alone.
ApplyScalarEffect(statline, effect, sourceAbility) -> Statline
                                       // CharacteristicModificationResolver.Resolve, wrapped back
                                       // into ScalarCharacteristicView.Resolved(current
                                       // .OriginalValue, resolvedValue, [sourceAbility]) - keeps the
                                       // true pre-mutation OriginalValue through a chain of
                                       // mutations. Both Apply*Effect methods skip a field that
                                       // already carries a contributing ability: first applied wins.

AttachedUnitAggregator.ResolveCaveatedInvulnerableSaves(statlines, classifications)
                                       // a companion step: for every AggregateStatlineEntry whose
                                       // Statline.InSv is still caveated (set at parse time - see
                                       // "BSData JSON Ingestion"), looks up its single
                                       // ContributingAbilities[0] and applies the classification's
                                       // first unconditional InvulnerableSaveCharacteristicEffect.
                                       // A conditional or missing one leaves it caveated. No
                                       // collision guard needed: Datasheet excludes the InSv-caveat-
                                       // internal ability names from the general ability walk, so
                                       // ApplyStatlineFlagRules never also matches it.
```

**Wiring**: `AttachedUnitAggregator.Build` takes the `AbilityClassificationCatalogue` singleton
(`Program.cs`, loaded once from `Data/ability-classifications.json`), threaded through
`LivePlay.cshtml.cs`/`LivePlayCasualtyService`. Runs after `BuildStatlines`/`BuildAbilities` produce
their live, casualty-filtered results, so a flagged value's liveness falls out of ability presence
with no separate tracking. Never mutates `Datasheet`/`Unit`; only the returned, decorated copy of the
statline entries carries an effect. A conditional effect (see ability-classification-catalogue.md's
unconditional rule) is not applied and has no Statline display of its own yet.

**`/LivePlay` display** (`resolve-known-ability-effects`; generalized from InSv/Oc-only to all six
scalar characteristics by the "Remaining Scalar Characteristics Retyped" work in bsdata-json-ingestion.md): replaces the
old InSv-only always-visible `.insv-caveat-text` paragraph with a general per-run
footnote-marker-and-legend mechanism, driven uniformly by any populated `ContributingAbilities` — an
unresolved invulnerable-save caveat and a `statline-flag-rules` match alike — see
`.claude/design-tokens.md`'s "Flagged statline legend" for the visual mechanism, and
`live-play-view`'s "Flagged Statline Characteristic Rendering" for the full requirement.
`LivePlayModel.GroupStatlines` reads each scalar field's own `ContributingAbilities` via
`GetScalarField` (M/T/Sv/W/Ld/Oc, keyed by `LivePlayModel.ScalarStatlineFieldOrder`) into a
`StatlineBlockViewModel.ScalarFlagSources` dictionary, and `Statline.InSv.ContributingAbilities`
directly into its own dedicated `InvulnerableSaveFlagSource` field (InSv keeps its own bespoke
compound-view renderer, never folded into the scalar dictionary); `LivePlayModel.AssignFlagMarkers`
then walks every run in order, assigning the first distinct source seen `*`, the next `**`, and so
on into `ScalarMarkers`/`InvulnerableSaveMarker`, reusing an already-assigned marker for the same
source wherever it recurs — so a `WholeUnit`-scoped source affecting every run of a unit keeps one
marker throughout and gets a legend line in every one of those runs, never consolidated into a
single shared location.
