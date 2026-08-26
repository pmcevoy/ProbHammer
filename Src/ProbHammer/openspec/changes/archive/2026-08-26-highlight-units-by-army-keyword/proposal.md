## Why

Now that `display-unit-keywords-section`/`resolve-category-keywords` give every unit block a real,
populated Keywords section, a player still has to open every single unit's Keywords section by hand
to answer a very common in-game question: "which of my units does this stratagem/ability actually
apply to?" (e.g. "All friendly ADEPTUS ASTARTES units..."). The Army Header already aggregates
army-wide facts (Rules, Detachments) above the unit-block list — it should also let a player pick a
keyword once and immediately see every currently-relevant unit, without opening each one.

## What Changes

- Adds a new "All Keywords" section to the Army Header, rendered as its own collapsible section
  (closed by default) directly beneath the existing Rules section — an alphabetical, deduplicated
  union of every keyword currently present on any live-play unit, one pill per keyword.
- The union is casualty-aware: a keyword whose only carrier(s) are currently dead is not shown, and
  reappears automatically if those casualties are later reverted (Reset Casualties, or manually
  incrementing a model back up).
- Each pill in this new section is a toggle control. Selecting one:
  - Applies the page's existing filled/`--bg3`-and-white "active control" treatment to that pill
    itself.
  - Force-expands (without collapsing any section the player already had open) every unit's
    Keywords section that contains a matching keyword.
  - Flags that same keyword's own pill inside each matching unit's Keywords section with the
    existing amber-tint "flagged value" treatment — a passive highlight, not itself a new control.
  - More than one keyword can be selected at once; a unit qualifies for expansion/highlighting if
    it has ANY currently-selected keyword (OR matching), not all of them.
  - Deselecting reverses the active-control style and the flag, but never auto-collapses a section
    it had force-expanded — matching this page's existing "sections only ever get easier to see,
    never yanked shut on you" posture.
- Selection state is transient: it does not persist across a page reload, matching the existing
  per-unit weapon/statline selection filter's own precedent (`Selection State Does Not Persist
  Across A Page Reload`), not the casualty/half-strength/battle-shock localStorage precedent.
- Purely a client-side, rendering-and-interaction feature: the union is computed by scanning the
  already-rendered per-unit Keywords chips (mirroring the existing weapon-total recomputation
  pattern), not by teaching the server a second keyword aggregation. No changes to
  `ProbHammer.Core`'s domain model, `LivePlayModel`, or `AttachedUnitAggregator`.
- The existing, just-shipped per-unit Keywords chips are otherwise unchanged: they stay
  non-interactive `<span>`s and gain no click behavior of their own — only their appearance changes
  (amber-tint) when flagged by an active header filter.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `live-play-view`: adds an army-wide keyword filter section to the Army Header (structure,
  sorting, casualty-aware repopulation) and its cross-page interaction with each unit's existing
  Keywords section (toggle/multi-select semantics, force-expand-without-auto-collapse, flagged-pill
  highlighting, no reload persistence).

## Impact

- `src/ProbHammer.Web/Pages/Shared/_ArmyHeader.cshtml` — new empty-placeholder section markup only;
  no new render-model fields.
- `src/ProbHammer.Web/wwwroot/js/live-play.js` — new client-side module: scan-and-rebuild the
  keyword union from rendered DOM (on load and after every `swapUnitBlock`), click/toggle handling,
  force-expand matching Keywords `<details>`, flagged-pill class management, active-filter-set
  pruning on rebuild.
- `src/ProbHammer.Web/wwwroot/css/site.css` — new selected/active pill style (reusing the existing
  `--bg3`/white "control" treatment) and reuse of the existing `.stat-tile-flagged`/`--amber-tint`
  convention for the per-unit flagged pill (no new colour tokens).
- `openspec/specs/live-play-view/spec.md` — delta covering the above.
- No changes to `ProbHammer.Core`, `ArmyRosterEnricher`, `BsdataDatasheetMapper`, or any BattleScribe
  pipeline code — Keywords data itself is untouched, only how it's surfaced/interacted with on
  `/LivePlay`.
