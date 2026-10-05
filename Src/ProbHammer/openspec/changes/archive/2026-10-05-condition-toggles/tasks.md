# Tasks

## 1. Core: activation state and effect classification

- [x] 1.1 Add `ConditionActivations`/`AbilityActivation` and a `ConditionActivations` property on `ICombatUnit`, `Unit` and `AttachedUnit` (default empty); verify `dotnet build` succeeds
- [x] 1.2 Add the three-way effect state helper (Applied / Suppressed / NotApplied, design D2) with unit tests for: unconditional, activated text condition, activated empty-text (usage-limit) condition, selected option, unselected option with a selection, no selection, a selected option carrying its own condition text, unknown ability ignored
- [x] 1.3 Route `ApplyStatlineFlagRules` and its not-applied recording through the helper; verify new `StatlineFlagRuleTests` for an activated OC/InSv effect and that every existing test still passes
- [x] 1.4 Route `MatchedWeaponEffects` and `FindNotAppliedEffects`/`FindNotAppliedKeywordGrants` through the helper; verify aggregator tests for activated Chance for Glory (S/A/AP/D), an activated grant splitting a merged row, Dark Pacts with Lethal Hits selected (applied grant, no not-added Sustained Hits 1), and an activated effect dropping when its bearer dies
- [x] 1.5 Report `ActivatableConditions` on `AttachedUnitAggregateView` from matching (design D3); verify tests for Chance for Glory (one empty-text condition with usage limit), two distinct condition texts, a Dark Pacts choice toggle, and a melee-only condition on a unit with no melee weapons reporting nothing
- [x] 1.6 Update `.claude/domain-model/roster-context.md` and `.claude/domain-model/ability-classification-catalogue.md` (activation as a consumer input, the three-way state); verify both describe the shipped shapes

## 2. Web: sync plumbing

- [x] 2.1 Add `ActivationAdjustment` to `LivePlaySyncRequest`, apply it in `RebuildRosterWithStatus`, and include its unit indexes in `LivePlayCasualtyService`'s re-rendered set; verify a sync test that an activation-only request re-renders that unit with the effect applied, and that an out-of-range index is ignored
- [x] 2.2 In `live-play.js`, add the activations storage map and include it in `syncLivePlayState`'s request and no-op check; make `syncPhaseTurn` send the full casualty, status and activation state; verify a sync test that a phase change with recorded casualties re-renders blocks showing those casualties

## 3. Web: Apply controls and provenance

- [x] 3.1 Let `RulePopoverRenderer` take an Apply-section builder (design D4) and supply it from `_UnitBlock.cshtml` only; render switches and option lists with labels per D4 and checked state from the view; verify rendering tests: Chance for Glory switch labelled "Once per battle", Dark Pacts radios None/[LETHAL HITS]/[SUSTAINED HITS 1] with None checked, a nested ability popover inside a chip popover also has controls, the Army Header popover has none, radio names unique per popover
- [x] 3.2 Cap multi-select checkboxes at the group's maximum in `live-play.js`; verify live in the browser on a fixture with a `1-2` group (or a rendering test asserting the `data-max` attribute plus a manual check)
- [x] 3.3 Wire control changes to storage and the close-triggered sync (design D5) in `live-play.js`; verify live that switching Chance for Glory keeps the popover open, and closing it turns the Daemon hammer's S/A/AP/D amber
- [x] 3.4 Add the "activated" note to Applied provenance lines and granted chip sources (design D7); verify a rendering test that an activated Chance for Glory Strength popover shows original 8, "+1 activated", total 9
- [x] 3.5 Style the Apply section in `site.css` from existing tokens (title bar like `.rule-popover-title`, full-width rows, `--bg3` for on/selected) and document it in `.claude/design-tokens.md`; verify at 667×315 and 375×539 with chrome-devtools emulation that rows are tappable and long option labels wrap

## 4. Integration

- [x] 4.1 Real-list check with `data/gw-app-export-chaos-lord-terminator-armour.txt` and a Heretic Astartes list carrying Dark Pacts: activate Chance for Glory, pick each pact, reload, change phase, mark a casualty; verify every step in the running app via chrome-devtools DOM inspection, not screenshots alone
- [x] 4.2 Run the full test suite; verify it passes
- [x] 4.3 Delete the "Condition toggles inside the ability popover" entry from `.claude/vnext-ideas.md`; verify the unit-fact, "used" marker and Wellspring-link entries remain
