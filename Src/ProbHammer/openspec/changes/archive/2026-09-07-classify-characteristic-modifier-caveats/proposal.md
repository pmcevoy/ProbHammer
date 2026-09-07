## Why

`/LivePlay` shows correct-but-incomplete data: an ability, wargear choice, or Enhancement that
structurally changes a specific Statline/WeaponProfile characteristic is never linked to the
characteristic it affects — the reader has to already know the rules. BSData's own structured
`BsModifier` data (`Type`/`Field`/`Value`/`Conditions`) already carries this fact for most real
cases, and is currently read only for hidden-gating (`IsGameModeGated`), never for its literal
effect. A prior attempt (`resolve-structured-characteristic-modifiers`, built then reverted
2026-09-07 — see `project_resolve_structured_characteristic_modifiers_change` memory) tried to
reach a fully-resolved displayed value in one pass, and shipped two real classification bugs: a
condition-tree check that silently treated any condition shape it didn't recognize as "safe to
bake in unconditionally," and an "is this optional" heuristic (`entry.Type == "upgrade"`) that a
real, capped, player-chosen squad slot (typed `"model"`, not `"upgrade"`) defeated. Both bugs
passed 515+ automated tests and every full-corpus scan; only a manual cross-check against
NewRecruit caught them. This change ships a deliberately smaller, safer first half: classify and
surface *that* a characteristic is modified (a "caveat"), for only the subset of cases that can be
checked deterministically without repeating either bug — with no attempt yet to compute the actual
resulting value, and no baking of any value into the shared, selection-blind `Datasheet`.

## What Changes

- `BsdataDatasheetMapper` classifies each `BsModifier` encountered while building a `Datasheet`
  using a closed-world condition-tree check: a modifier is recognized only when it carries **no
  condition at all** ("tier 1" — rides directly on its granting selection), or when its condition
  is provably scoped entirely to that **same granting selection's own state** ("tier 2" — e.g. a
  quantity threshold within the same entry). Any other condition shape — one referencing a
  different selection, a different unit, live attachment state, or any shape the checker doesn't
  specifically recognize — is left **unclassified**, never guessed as safe. This mirrors the
  "closed vocabulary, no fuzzy match" discipline `statline-flag-rules` already established, applied
  to structured data instead of hand-authored ability text.
- `Datasheet` exposes classified candidates **on demand** (mirroring the existing
  `OptionalAbilityNames`/`TryResolveWeaponProfile` on-demand pattern) — selection-blind catalog
  data, identical for every roster resolving this `Datasheet`. No value is written into
  `Datasheet`'s own `Statline`/`WeaponProfile` fields; a candidate only ever describes "this
  selection, if chosen, would modify this characteristic this way."
- `AttachedUnitAggregator.Build` applies a classified candidate to a specific resolved unit **only**
  when its granting selection is confirmed present on that unit — uniformly alongside the existing
  hand-authored `StatlineFlagRuleCatalogue` matches, and using the same live, casualty-filtered
  presence check that mechanism already relies on. This produces a caveated
  `CharacteristicView` (`IsCaveated: true`, `ContributingAbilities` populated, `DerivedValue: null`)
  — nothing computes or displays an actual resulting number. There is deliberately no "unconditional
  bake-in" path of any kind: every candidate, regardless of its own selection's structural type, is
  presence-checked before it can apply to a specific unit — removing the exact heuristic
  (`entry.Type == "upgrade"`) that caused the reverted attempt's second bug.
- `/LivePlay`'s existing flagged-tile-and-legend rendering (already generalized to all six scalar
  characteristics by the prior scalar-retyping work) is expected to display a populated-caveat,
  no-derived-value tile with no code change — this needs confirming against a real render during
  implementation, not assumed from the prior generalization's own test coverage.

**Explicitly deferred, not part of this change:**
- Computing an actual `DerivedValue` for any caveat this change surfaces (a distinct, later
  "resolve" step, tracked in `.claude/vnext-ideas.md`).
- Prose-only classification — an ability whose *text* describes a characteristic change with no
  backing `BsModifier` at all (e.g. Darnath Lysander's "Inspiring Commander"); resumes the paused
  schema in `project_ability_classification_prose_schema` memory.
- Tier 3+ conditions: a sibling selection on the same unit (e.g. a relic gating another
  characteristic change), live attachment state, or a condition naming a different unit entirely.
  Scoped out deliberately so this change's real-data correctness can be verified — including a
  manual NewRecruit cross-check — before any wider condition-tree coverage is attempted.

## Capabilities

### New Capabilities
- `characteristic-modifier-caveats`: recognizes when a structured, BSData-modifier-derived
  characteristic change applies to a specific resolved unit (its granting selection is present),
  and surfaces it as a caveated `CharacteristicView` with no computed value — the data-derived
  counterpart to `statline-flag-rules`' hand-authored ability-text rules.

### Modified Capabilities
- `catalogue-json-ingestion`: `BsdataDatasheetMapper` gains a closed-world tier-1/tier-2 `BsModifier`
  condition classifier; an unrecognized or wider (tier 3+) condition shape is left unclassified
  rather than assumed safe.
- `datasheet-catalogue`: `Datasheet` gains an on-demand exposure of classified characteristic-modifier
  candidates, selection-blind like its existing weapon/ability on-demand indexes.

## Impact

`ProbHammer.Core` only: `Domain/Catalogue/Bsdata/BsdataDatasheetMapper.cs` (new classification
step), `Domain/Catalogue/Datasheet.cs` (new on-demand exposure), `Domain/Roster/AttachedUnitAggregator.cs`
(new presence-gated application step, alongside the existing `ApplyStatlineFlagRules`). No new
`Domain.Catalogue.CharacteristicValue`/`CharacteristicView` shape needed — this reuses
`IsCaveated`/`ContributingAbilities` exactly as `unify-invulnerable-save-characteristic-view`/
`unify-objective-control-characteristic-view` already established. Verification requires both the
existing full-corpus-scan discipline (a new explicit-only scan asserting every real `BsModifier` in
the live clone is either correctly classified tier 1-2 or deliberately left unclassified) and a
manual NewRecruit cross-check against real captured rosters before this is considered proven — per
`feedback_verify_bsdata_claims_precisely` memory, an automated-tests-only pass already missed both
of the reverted attempt's real bugs.
