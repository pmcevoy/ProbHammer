## Why

Every mechanism this project has for turning an ability/rule's *effect* into a computed value
(`classify-characteristic-modifier-caveats`'s structural `BsModifier` classifier) reads BSData's
structured JSON, inside `BsdataDatasheetMapper`. That classifier is correctly scoped to structural
data — it cannot see anything in the free-text prose that carries most of the game's real rules
(Shield Dome's invulnerable save grant, Vexilla's Objective Control bonus, a Detachment rule's
keyword-scoped buff, an Army Rule's own vow text). Extending `BsdataDatasheetMapper` itself to also
parse prose was considered and rejected: that mapper is already large, already the source of
several real data-misunderstanding bugs found only by manual NewRecruit cross-checks, and adding
more classification responsibility to it increases risk to code that already works. A standalone,
text-only classifier — input is just an `Ability`/`DetachmentRule`'s own `Name`+`Text`, no BSData
JSON tree knowledge at all — is both safer to build in isolation and, as a side effect, usable
against both this project's import pipelines (BSData-derived and BattleScribe/NewRecruit-derived)
uniformly, since neither pipeline's own JSON shape is ever touched.

This change proves that mechanism against four known, real, ground-truth examples — Shield Dome,
Vexilla, Templar Vows, and a Black Templars Detachment rule (Marshal's Household) — before any
larger corpus-wide or LLM-assisted work is attempted.

## What Changes

- Add a new, standalone classification component (independent of `Domain.Catalogue.Bsdata` and any
  BSData JSON types) that takes a rule/ability's `Name` and `Text` and extracts, where recognizable:
  - **Target**: who the rule affects — the bearer alone, the bearer's whole attached unit, or a
    roster-wide keyword-filtered/unconditional target — always one of a closed set, never absent.
  - **Effects**: zero or more atomic, single-characteristic mutations (`Improve`/`Worsen`/`Set` a
    named `Statline` scalar by a fixed amount) the text unconditionally states.
- Add a small CLI tool that runs this classifier against real ability/rule text pulled from the live
  BSData corpus clone and reports what classifies vs. what doesn't, so results can be inspected
  directly rather than only through test pass/fail output.
- Deliberately excludes (see design.md for the full list, so it isn't lost): conditional effects,
  `WeaponProfile`-targeting effects, `Multiply`/`Divide` verbs, evaluating a Target predicate against
  an actual resolved roster, applying/executing a classified Effect anywhere, and any live/runtime
  LLM call. All are real, confirmed future needs — not eliminated, just out of this change's scope.

## Capabilities

### New Capabilities
- `rule-effect-classification`: standalone, text-only extraction of a rule/ability's Target and
  unconditional characteristic Effects from its Name+Text, independent of any specific catalogue
  format or resolved roster.

### Modified Capabilities
(none — this change adds a new, standalone capability with no existing behavior changed)

## Impact

- New code only, under `ProbHammer.Core` (exact namespace decided in design.md), with no dependency
  on `Domain.Catalogue.Bsdata` or `Domain.Import.BattleScribe`.
- A new CLI project or runnable command (decided in design.md) for corpus-wide manual inspection —
  not wired into `ProbHammer.Web`/`/LivePlay` at all.
- No changes to `BsdataDatasheetMapper`, `AttachedUnitAggregator`, or any existing rendering code.
