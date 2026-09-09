# Statline-Flag Rules

Full requirements: `openspec/changes/resolve-known-ability-effects/` (original hand-authored
mechanism) and `apply-rule-effect-baseline/` (current baseline-driven implementation — retired the
former's closed 2-rule vocabulary). Recognizes a rule/ability by its own normalized Text (never
Name+Text, and never a live call to `RuleEffectClassifier.Classify` — the checked-in,
human-verified `RuleClassificationBaseline` is the runtime trust boundary, see
"Verified-Classification Baseline" in rule-effect-classification.md) and derives a flagged Statline-characteristic value for
display on the *specific resolved unit* it's currently present on — never mutating the shared
`Datasheet`/`Unit` themselves. The matched source ability always keeps rendering normally in the
unit's Abilities/Enhancements listing alongside the derived value; this mechanism only ever adds a
value, never removes or hides anything.

```
AttachedUnitAggregator.ApplyStatlineFlagRules(statlines, abilities, baseline)
                                       // looks up each present AggregateAbilityEntry's own Ability
                                       // .Text, normalized via RuleEffectClassifier.Normalize,
                                       // against baseline.TryGet - a match whose own classified
                                       // RuleTarget is SelfRuleTarget or AttachedUnitRuleTarget is
                                       // applicable (TryGetApplicableEntry); KeywordRuleTarget/
                                       // UnconditionalRuleTarget produce no match, the same outcome
                                       // as no baseline entry at all (no roster-wide keyword-
                                       // predicate evaluation exists in this capability - the two
                                       // known real instances, Army: Shivversplint and Faith-Fuelled
                                       // Resolve, are expected, already-catalogued gaps, not bugs).
IsBearer(abilityEntry, target, statlineEntry) -> bool
                                       // the old StatlineFlagRuleScope.Bearer/WholeUnit split,
                                       // reread off the matched entry's own classified RuleTarget
                                       // instead of a hand-set enum: SelfRuleTarget - the matched
                                       // ability's own bearer row(s), one specific model-line when
                                       // AggregateAbilityEntry.StatlineName is set, the whole owning
                                       // component when it's null (a Datasheet-level or Enhancement-
                                       // sourced ability); AttachedUnitRuleTarget - every row of the
                                       // whole ICombatUnit regardless of which component granted it
                                       // (Vexilla's own "models in the bearer's unit" spans every
                                       // component of an attached formation in real 11e rules, not
                                       // just the bearer's own).
ApplyEffect(statline, effect, sourceAbility) -> Statline
                                       // dispatches per CharacteristicEffect subtype:
                                       // ScalarCharacteristicEffect -> ApplyScalarEffect (below);
                                       // InvulnerableSaveCharacteristicEffect -> ApplyInvulnerableSaveEffect,
                                       // a thin wrapper around the already-proven
                                       // InvulnerableSaveEffectResolver.Resolve (Invulnerable-Save
                                       // Effect Resolution, invulnerable-save-effect-resolution.md)
ApplyScalarEffect(statline, effect, sourceAbility) -> Statline
                                       // the one piece of glue CharacteristicModificationResolver
                                       // itself doesn't provide (Characteristic-Modification Kind's
                                       // own Non-Goals): resolves via
                                       // CharacteristicModificationResolver.Resolve(effect
                                       // .Characteristic, current.Value, effect.Verb, effect.Amount),
                                       // then wraps the result back into a ScalarCharacteristicView
                                       // via ScalarCharacteristicView.Resolved(current.OriginalValue,
                                       // resolvedValue, [sourceAbility]) - preserving the true
                                       // pre-mutation OriginalValue through a chain of mutations,
                                       // mirroring VexillaStatlineFlagRule.Apply's own old behavior
                                       // exactly. Both ApplyScalarEffect and
                                       // ApplyInvulnerableSaveEffect skip (return the statline
                                       // unchanged) when the target field already carries a
                                       // contributing ability - same-field stacking between two
                                       // baseline matches: the first applied wins, the second is
                                       // skipped, no accumulate logic (no real corpus example needs
                                       // it - checked against the full 41-entry baseline).

AttachedUnitAggregator.ResolveCaveatedInvulnerableSaves(statlines, baseline)
                                       // unify-characteristic-effect-resolution: a companion step,
                                       // run separately from ApplyStatlineFlagRules (different input
                                       // shape, different iteration - walks statlines directly, not
                                       // present abilities). For every AggregateStatlineEntry whose
                                       // Statline.InSv is still caveated (set by
                                       // ResolveInvulnerableSave/BattleScribeRosterMapper's own
                                       // mirrored resolver at parse time - see "BSData JSON
                                       // Ingestion"), normalizes its single ContributingAbilities[0]
                                       // Text and looks it up against the same baseline, applying an
                                       // InvulnerableSaveCharacteristicEffect via the same
                                       // InvulnerableSaveEffectResolver ApplyInvulnerableSaveEffect
                                       // uses. No presence/collision guard needed here (unlike
                                       // ApplyStatlineFlagRules/ApplyScalarEffect's "skip if already
                                       // touched") - Datasheet's own exclusion of the two InSv-
                                       // caveat-internal ability-name conventions from the general
                                       // ability walk (see "BSData JSON Ingestion") means this
                                       // ability is never independently "present" for
                                       // ApplyStatlineFlagRules to also match, so there is nothing
                                       // left to collide over. An unresolved caveat (no baseline
                                       // match) is left exactly as before. Called from
                                       // AttachedUnitAggregator.Build right after BuildStatlines,
                                       // before ApplyStatlineFlagRules - ordering doesn't matter for
                                       // correctness, this placement just reads most naturally
                                       // ("resolve what's already known to need resolving, then
                                       // apply presence-driven flags").
```

**Wiring** (`AttachedUnitAggregator.Build`, which now takes a `RuleClassificationBaseline` parameter
- registered as a singleton in `Program.cs`, loaded once from
`src/ProbHammer.Web/Data/RuleEffectClassifications.json` resolved against
`IWebHostEnvironment.ContentRootPath`, mirroring `BsdataCatalogueCache`'s own root-resolution
convention, and threaded through `LivePlay.cshtml.cs`/`LivePlayCasualtyService`'s own DI-injected
copy): runs as an additional step after `BuildStatlines`/`BuildAbilities` produce their live,
casualty-filtered results (`ApplyStatlineFlagRules`). Since `BuildAbilities`' own output is already
filtered to only currently-present sources (the same liveness rule that governs whether the ability
itself renders), a flagged value's liveness falls out for free with no separate tracking — marking
the bearer a casualty removes the matching `AggregateAbilityEntry` on the next `Build`, so the
lookup pass simply has nothing to match against and the affected `Statline` reverts to its own
Datasheet base value. Never mutates `Datasheet`/`Unit`; only the returned, decorated copy of the
statline entries carries an effect. Every matched baseline Effect is applied regardless of its own
`IsCaveated`/`FullyHandled` state — every Effect-bearing baseline entry is independently verified
correct for that Effect specifically, regardless of what else its own text states (see
"Verified-Classification Baseline" in rule-effect-classification.md).

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
