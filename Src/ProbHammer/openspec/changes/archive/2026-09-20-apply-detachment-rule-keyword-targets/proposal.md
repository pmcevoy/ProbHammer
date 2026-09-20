## Why

A Detachment rule whose own text names a keyword-qualified target (e.g. Marshal's Household's
"Faith-Fuelled Resolve": "Friendly SWORD BRETHREN SQUAD units have +1 OC.") exists today only as
reference text printed once in the Army Header. Nothing evaluates which of the roster's actual
units that target reaches — `RuleEffectClassifier` already extracts this exact rule as
`KeywordRuleTarget("SWORD BRETHREN SQUAD")` plus a resolvable `Improve Oc 1` Effect, and neither
piece is consumed anywhere. A player currently has to read the header rule and manually work out
which unit block it applies to, and the OC bonus is never reflected on that unit's Statline at all.

This is the first, deliberately narrow slice of a broader "inbound ability" idea (a unit displaying
a rule granted to it from outside its own Datasheet). Army Rules (e.g. Templar Vows) already
happen to render correctly per-unit today via an unrelated mechanism (BSData's own per-datasheet
infoLink placement), and unit-sourced abilities (e.g. a Character's own aura reaching other units)
need a materially different Target shape — both are explicitly out of scope here; see Non-Goals.

## What Changes

- New `AbilityOrigin.DetachmentRule` value, identifying an `Ability` synthesized from a selected
  Detachment's own rule text rather than resolved from any Datasheet/BSData walk.
- New `ICombatUnit.InboundAbilities` property (mutable, defaults to empty), set once during roster
  enrichment — mirrors the existing `IsHalfStrengthOverride`/`IsBattleShocked` "settable
  post-construction, read everywhere" shape already on the interface.
- New roster-enrichment step, called once from `ArmyRosterProvider.Build` (the existing single
  point where both import pipelines already converge) after either pipeline's `ArmyRoster` is
  built: for each selected Detachment's own `DetachmentRule`, look up its normalized Text against
  the existing checked-in
  `RuleClassificationBaseline` (same lookup convention `statline-flag-rules` already uses — never a
  live `RuleEffectClassifier.Classify` call at runtime). When the matched entry's `Target` is a
  `KeywordRuleTarget(keyword)`, attach a synthesized `Ability` (Name/Text from the `DetachmentRule`,
  `Scope: Unit`, `Origin: DetachmentRule`) to every `ICombatUnit` whose existing
  `KeywordResolution.EffectiveKeywords` already contains that keyword.
- `AttachedUnitAggregator.BuildAbilities` reads `combatUnit.InboundAbilities` as a new, second
  "reported once, belonging to no single component" source (alongside the existing Army Rule
  promotion) — rendered orphaned above the Datasheet/ModelLine rows, reusing the exact visual slot
  an Army Rule promotion already occupies, no new `/LivePlay` markup.
- `statline-flag-rules`' bearer-scope check treats a `DetachmentRule`-origin ability as
  WholeUnit-scoped regardless of its own baseline-classified `Target`, so a matched rule's already-
  classified, resolvable Effect (Faith-Fuelled Resolve's `Improve Oc 1`) is genuinely applied to
  every present statline row of the matched unit, not merely displayed as a reference.

### Non-Goals (named for a later change, not built here)

- Army-Rule-origin keyword targets (e.g. Templar Vows → ADEPTUS ASTARTES). That mechanism (BSData
  per-datasheet infoLink presence) is untouched — it already produces correct scoping today and is
  a separate problem from this change's own matching mechanism.
- Any unit-sourced inbound ability (a Character's own aura reaching other units, e.g. Darnath
  Lysander's "Inspiring Commander") — needs a genuinely different Target shape (a named-unit-type
  list, not a single keyword) plus a model-level sub-filter ("non-Character models"); explicitly
  deferred.
- A multi-keyword (OR-list) `KeywordRuleTarget` — every real Detachment-rule keyword target
  classified so far is single-keyword; `KeywordRuleTarget` stays a single `string` in this change.
- A Detachment rule classified `UnconditionalRuleTarget` or `SelfRuleTarget` — stays reference-only
  in the Army Header exactly as today; not attached to any unit by this change.
- Any conditional/live-state-gated effect (e.g. "while not Battle-shocked") — not exercised by any
  rule this change's scope covers.
- A distinguishing "source" legend/marker on the rendered inbound entry (e.g. "from Detachment:
  Marshal's Household") — this change reuses the existing, unlabeled orphaned-entry rendering
  as-is; provenance labeling is deferred.
- Any change to the Army Header's existing per-Detachment rule display — purely additive, the
  header is untouched.
- Extending `AttachedUnitAggregator.BuildWeapons`' own bearer-scope check for the same treatment —
  no real Detachment-rule baseline entry classified so far carries a `WeaponCharacteristicEffect`,
  so this change only touches the Statline call site. See design.md's Risks.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `roster-model`: `ICombatUnit` gains an `InboundAbilities` property; a new requirement describes
  its shape and lifecycle.
- `army-roster-enrichment`: a new requirement describes how a Detachment rule's classified
  `KeywordRuleTarget` is matched against the roster's units to populate `InboundAbilities`.
- `attached-unit-tracker`: the "Aggregate Ability View" requirement gains `InboundAbilities` as a
  second "reported once, no single component" source.
- `statline-flag-rules`: the "Target-Scoped Application" requirement gains an exception — a
  `DetachmentRule`-origin ability is treated as WholeUnit-scoped regardless of its own baseline
  entry's classified target.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/AbilityOrigin.cs` — new `DetachmentRule` value.
- `src/ProbHammer.Core/Domain/Roster/ICombatUnit.cs` — new `InboundAbilities` property.
- `src/ProbHammer.Core/Domain/Roster/` — new shared matching helper (exact name in tasks.md).
- `src/ProbHammer.Web/Services/ArmyRosterProvider.cs` — gains a `RuleClassificationBaseline`
  constructor parameter; `Build` calls the new matching helper once, after either pipeline's
  `ArmyRoster` is built.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — `BuildAbilities` gains the new
  source; `TryGetApplicableEntry`/`IsBearerOf` gains the `DetachmentRule`-origin special case.
- No `/LivePlay` template changes — the new entries render through the existing orphaned-ability
  path.
- No changes to `RuleEffectClassifier`, `RuleClassificationBaseline`, or the checked-in baseline
  JSON — this change consumes existing classification as-is (Faith-Fuelled Resolve already
  baselines as `KeywordRuleTarget("SWORD BRETHREN SQUAD")` + `Improve Oc 1`).

## Resolved Question (was parked, closed before implementation)

Confirmed directly against `BsdataDatasheetMapper.BuildDatasheet` and the real Black Templars
corpus: `Datasheet.FactionKeywords` is never populated by the real mapper (`factionKeywords: []` is
passed unconditionally); every `categoryLinks` entry — a `"Faction: X"`-prefixed keyword
(prefix-stripped) and a self-referencing unit-type keyword alike — lands in the single `Keywords`
set. The real Sword Brethren Squad datasheet's own `categoryLinks` carries `"Sword Brethren Squad"`,
`"Faction: Adeptus Astartes"`, and `"Faction: Black Templars"`, all resolving into `Keywords`. So
`KeywordResolution.EffectiveKeywords` (built from `Keywords` alone) already covers every real
keyword shape this change's matching step needs — no design change required. See design.md's own
Open Questions section, now resolved the same way.
