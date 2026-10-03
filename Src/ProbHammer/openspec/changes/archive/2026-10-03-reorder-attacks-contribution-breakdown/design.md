## Context

See proposal.md for motivation. Two facts about the current render, both measured live (Chrome,
667×315 landscape, Helbrecht's Astartes Chainsword expanded):

- **Order** is fixed in `_UnitBlock.cshtml`: per weapon, the `FlagLegend` row, then the
  `GroupWideAttacksLines` loop, then the `Breakdown` loop (each base row followed by its own
  row-bound `AttacksLines`). Ranged and Melee repeat the same block.
- **Indent** differs from what `site.css` intends. `.weapon-contribution-label { padding-left:
  1.75rem }` (specificity 0,1,0) is outranked by `.live-play-page .weapon-table td { padding: 0.3rem }`
  (0,2,1), so base rows have rendered at a computed `4.8px` ever since that rule was added. The
  Attacks-line rules (`.weapon-attacks-contribution-row.group-wide/.row-bound
  .weapon-attacks-contribution-label`, 0,3,0) do win, giving `28px`/`44px`. The "same indent as a
  contributor row" comment on the group-wide rule assumed the dead 1.75rem.

`live-play.js` (`recomputeWeaponRow`, `initWeaponProvenanceToggles`) finds these rows by
`data-weapon-id`, never by DOM position, so reordering sibling `<tr>`s is safe for it.

## Goals / Non-Goals

**Goals:** group-wide line last and level with the base rows; row-bound lines keep one indent step
below their base row; the CSS states the indents that actually render.

**Non-Goals:** removing `weapon-flag-legend-row` (follow-up value-provenance-popovers change); any
model/view-model change; restyling the base rows.

## Decisions

**D1 — Move the `GroupWideAttacksLines` loop after the `Breakdown` loop, in both sections.** The
legend row stays first. Alternative: sort lines into one ordered list in `LivePlayModel` - rejected,
the view model already separates group-wide from row-bound and only the markup order is wrong.

**D2 — Base rows' rendered position (td padding, 0.3rem) is the reference indent.** The group-wide
rule drops its own padding override so it inherits the same td padding; the dead
`.weapon-contribution-label` padding rule is deleted; row-bound becomes `0.3rem + 1rem` (`1.3rem`),
keeping the original one-`1rem`-step relationship to its base row. Alternative: revive the intended
`1.75rem` for base rows (fix the specificity) - rejected: it changes base rows the user is satisfied
with, spends horizontal room at 667px, and the user's sketch has base rows and the bonus line flush.

**D3 — Keep the scenario title "…renders once, above the breakdown" in the delta.** Strict
validation rejects renaming a scenario inside a MODIFIED requirement. The delta updates the body to
"after every base contributor row"; the main-spec title is retitled by hand after archive (task).

## Risks / Trade-offs

- [The ability pill's own internal padding puts its text a few px right of the base rows' text,
  even with equal cell indent] → pill edge aligns with base text; verify visually, accept if it reads
  as one column.
- [Group-wide line now sits below selection-excluded (struck) rows, e.g. Close combat weapon's
  `Initiate w/ Power fist`] → its amount still counts while anything is selected (existing JS
  behavior); confirm it doesn't read as excluded itself.
