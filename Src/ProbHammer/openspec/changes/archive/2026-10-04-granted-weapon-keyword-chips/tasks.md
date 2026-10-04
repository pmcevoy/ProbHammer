# Tasks

## 1. Core: keyword identity and equality

- [x] 1.1 Add `WeaponKeyword` (`Parse`, identity, value, display, `Compare`) in `Domain/Catalogue` (design D1); verify with unit tests covering every "Weapon Keyword Identity" and "Comparing Weapon Keyword Values" scenario, plus a test parsing every distinct grant keyword string in the checked-in catalogue without throwing and matching each to a sane identity.
- [x] 1.2 Switch `WeaponProfile.EqualityKey` to `WeaponKeyword` identity plus value (D2); verify with tests that Sustained Hits 1 vs 2 and Anti-Infantry vs Anti-Vehicle no longer compare equal, while casing/ordering differences still do, and that the existing aggregator tests still pass.
- [x] 1.3 Add `WeaponKeywordGrantResolver.Apply` (D3); verify with tests for each "Resolving A Weapon Keyword Grant" scenario (added, already present, better value replaces, worse value ignored).

## 2. Core: applying and recording grants

- [x] 2.1 Widen `MatchedWeaponEffects` to any weapon effect type and admit Detachment-rule-origin abilities regardless of classified target (D5); verify with a roster test where a Detachment-rule keyword-targeted Strength effect mutates every component's weapon, and that the existing weapon-effect tests still pass.
- [x] 2.2 Apply unconditional grants in `ResolveContributionProfile` and record `KeywordGrant`s on `WeaponContribution`/`AggregateWeaponEntry` (D3); verify with roster tests for the unit-wide grant (merged), bearer-scoped grant (split), redundant grant (not recorded) and Detachment-rule grant scenarios of "Aggregate Weapon Count View".
- [x] 2.3 Record `NotAppliedKeywordGrant`s only when the grant would change the keywords (D4); verify with tests that a conditional Lance grant reaching some contributors keeps the entry merged and is reported at entry level, and a conditional redundant grant is not recorded.
- [x] 2.4 Add a real-corpus test against a captured export with a Detachment-rule grant or Empyric Wellspring (Masters of the Maelstrom Legionaries' ranged weapons carry a not-added Strength effect); verify it passes.
- [x] 2.5 Update `.claude/domain-model/roster-context.md` (contribution/entry grant records, Detachment-rule weapon reach) and `ability-classification-catalogue.md` (consumer table gains keyword grants; drop the "known asymmetry" note); verify neither still says a Detachment rule never reaches weapons.

## 3. Web: chips, popovers and the conditional colour

- [x] 3.1 Replace `ProvenanceLine.Applied` with `ProvenanceLineKind` and add `ValueProvenance.IsConditionalOnly` (D7); verify `LivePlayModelTests` show a Chance for Glory value is conditional-only and a caveated or mixed value is not.
- [x] 3.2 Build `WeaponRowViewModel.Chips` (Native/Granted/NotAdded with sources) in `LivePlayModel` (D6); verify with `LivePlayModelTests` for each "Granted Weapon Keyword Chips" scenario, including order (native, granted in keyword order, then not-added).
- [x] 3.3 Add `RulePopoverRenderer.BuildKeywordChipPopover` and render chips in both weapon tables in `_UnitBlock.cshtml`, keeping native chip markup unchanged (D6); verify with rendering tests for each "Granted Keyword Chip Popover" scenario, including a granted keyword with no glossary entry still being a trigger.
- [x] 3.4 Render a conditional-only provenance tile with `provenance-cond` (D7); update the Chance for Glory rendering tests to the conditional colour and add the mixed and caveated stay-amber cases from "Value Provenance Highlight"; verify they pass.
- [x] 3.5 Add `--cond: #3d6fa3` and `--cond-tint` (design D7) and the conditional chip/tile rules to `site.css`; verify by grepping the CSS for the tokens and a screenshot at 667x315 of the Chaos Lord's Daemon hammer matching the approved mock.
- [x] 3.6 Update `.claude/design-tokens.md` (new tokens in the Colour Palette, granted/not-added chip styles, provenance tile colour rule) and `rules-glossary-and-popovers.md` if it describes chip popovers, and delete the shipped "Granted weapon keywords as amber chips" and "Detachment-rule weapon effects" entries from `.claude/vnext-ideas.md`; verify the docs name `--cond`, no longer say every highlighted value is amber, and neither vnext entry remains.

## 4. Integration

- [x] 4.1 Live check with `docker compose up --build` at `667x315x2,mobile,touch,landscape`: import every list in `data/`, confirm no weapon rows split or merge unexpectedly versus before (D2), the Masters of the Maelstrom Legionaries' ranged Strength shows a conditional-colour tile, and a granted chip's popover opens with its nested ability; verify each against the spec scenarios.
- [x] 4.2 Run the full test suite and verify it passes.
