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
                                       // (TryGetApplicableClassification); KeywordRuleTarget/
                                       // UnconditionalRuleTarget produce no match, the same outcome
                                       // as no classification at all - EXCEPT two whole-unit-scoped
                                       // origins (IsWholeUnitScopedOrigin): a DetachmentRule-origin
                                       // ability, whose keyword target was already evaluated by
                                       // DetachmentRuleInboundAbilityResolver, and an ArmyRule-origin
                                       // ability, only ever present on a unit that carries it. Only the
                                       // classification's unconditional effects apply; its
                                       // conditional Scalar/InvulnerableSave effects are recorded
                                       // on the same entries' NotAppliedEffects instead.
IsBearerOf(abilityEntry, target, componentName, statlineName) -> bool
                                       // SelfRuleTarget - the matched ability's own bearer row(s),
                                       // one specific model-line when AggregateAbilityEntry
                                       // .StatlineName is set, the whole owning component when it's
                                       // null (a Datasheet-level or Enhancement-sourced ability);
                                       // AttachedUnitRuleTarget - every row of the whole
                                       // ICombatUnit regardless of which component granted it. A
                                       // DetachmentRule- or ArmyRule-origin ability also gets every
                                       // row, whatever its classified Target (an Army Rule entry is
                                       // promoted to ComponentName null, so Self would match none).
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
unconditional rule) is not applied; it is recorded on `AggregateStatlineEntry.NotAppliedEffects` as a
`NotAppliedStatlineEffect` (source ability, effect, `EffectCondition`) on every entry an unconditional
effect of that record would reach, following the same bearer liveness, so `/LivePlay` can show it
without adding it (`value-provenance-popovers`).

**`/LivePlay` display** (`value-provenance-popovers`, replacing the earlier footnote-marker-and-
legend mechanism): every value an ability has something to say about renders as an amber,
tappable `.provenance-tile` whose popover lists the original value, each ability line and the
result - see `.claude/design-tokens.md`'s "Value Provenance Highlight" and `live-play-view`'s
"Value Provenance Highlight"/"Value Provenance Popover Content". `ValueProvenanceBuilder`
(`Pages/ValueProvenance.cs`, called from `LivePlayModel.BuildUnitBlock`) builds one
`ValueProvenance` per highlighted Statline field and InSv (`StatlineBlockViewModel.Provenance`) and
per weapon A/Skill/S/AP/D (`WeaponRowViewModel.Provenance`; the Skill entry is titled BS or WS by
weapon type, and its lines show the resulting `N+` like Sv/Ld): applied lines from the view's
`ContributingAbilities`, a caveat line when it is still caveated, not-added lines from
`NotAppliedEffects` (condition summary + "not added"), a fixed "Battle-shocked → 0" line on a
Battle-shocked OC, and the source ability's `UnclassifiedResidue` from the catalogue as a note.
