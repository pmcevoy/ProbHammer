## Why

An expanded weapon row on `/LivePlay` is the Attacks "contribution" area: it should read as a
breakdown summing to the row's total A. Today a group-wide ability contribution (e.g. Helbrecht's
"[Crusade of Wrath] (7×1) 7") renders *above* the base rows it adds to, and at a visibly deeper
indent than them, so the breakdown reads out of order: bonus first, then the values it is a bonus
on. Confirmed live at the 667×315 landscape viewport against `data/gw-app-export-templars-latest.txt`.

## What Changes

- A group-wide Attacks ability-contribution line renders **after** every base contributor row of the
  breakdown (including merged and selection-excluded rows), not above them.
- That line renders at the **same indent** as the base contributor rows.
- Row-bound / partial-reach Attacks lines are unchanged in placement: still nested directly under
  each contributor row they reach, one indent step deeper than that row.
- The base rows' rendered indent is the reference. The stylesheet's intended `1.75rem` contributor-
  row indent (`.weapon-contribution-label`) has never applied — it is outranked by the generic
  weapon-table `td` padding — so base rows render flush with the weapon name's cell edge. The group-
  wide line is brought in line with that actual position, and the row-bound indent is re-expressed
  relative to it.
- **Out of scope (deliberate):** the `weapon-flag-legend-row` ("* Crusade of Wrath", explaining an
  S/AP/D marker like `5*`) stays, so the same ability still appears twice in an expanded row (legend +
  Attacks line). That legend is currently the only explanation of an S/AP/D marker; it is retired by
  the follow-up value-provenance-popovers change (`.claude/vnext-ideas.md`), which replaces markers
  and legends page-wide.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `live-play-view`: "Weapon Ability-Contribution Row Rendering" — a group-wide line's placement
  moves from "once, above the breakdown" to "once, after every base contributor row, at the same
  indent".

## Impact

- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` — Ranged and Melee sections: loop order of
  `GroupWideAttacksLines` vs. `Breakdown`.
- `src/ProbHammer.Web/wwwroot/css/site.css` — contribution-row indent rules.
- `live-play.js` — no change expected: it selects contribution rows by `data-weapon-id`, not
  position.
- No domain/model change; `LivePlayModel.BuildGroupWideAttacksLines` is untouched.
- Docs: `.claude/design-tokens.md` (breakdown ordering/indent).
