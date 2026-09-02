## Context

`/LivePlay` has no responsive breakpoint today (design-tokens.md: "No dedicated responsive
breakpoint today"). Real-viewport testing this session (chrome-devtools-mcp emulation at the two
confirmed real device shapes — 375x539 portrait, 667x315 landscape — cross-checked against real
iPhone screenshots) found the portrait failures are structural, not cosmetic (Leadership/Objective
Control tiles render entirely outside `.statline-tiles`' visible box; a long weapon-keyword chip
overflows into the numeric stat columns beside it), while landscape's own static header
(`.army-header`, 321px measured) already exceeds its 315px viewport before any unit renders. See
proposal.md for the full rationale for choosing landscape-only over a dual-orientation layout.

Existing conventions this design reuses rather than reinvents: the `.lp-section`
`<details>/<summary>` disclosure pattern (already used for per-unit Statline/Ranged/Melee/Keywords
and the Army Header's All Keywords section); `live-play.js`'s existing `swapUnitBlock` carry-
forward mechanism (already generalized once, for the phase/turn tracker's `forcedSections`, per
domain-model-11e.md's "Phase/Turn Tracker" section) for preserving open/closed disclosure state
across a fragment re-render.

## Goals / Non-Goals

**Goals:**
- Make landscape the fully-supported phone-class orientation for `/LivePlay`, with a graceful
  rotate-device prompt for narrow-viewport portrait rather than a broken render.
- Fit the army header (plus a sliver of the first unit) inside a real 315px-tall landscape
  viewport.
- Remove the confirmed-dead legacy simulation-page CSS while it's already being touched.
- Leave every existing design token, color, radius, and popover mechanic unchanged — this is a
  layout/disclosure change, not a re-theme.

**Non-Goals:**
- Redesigning `.statline-grid`'s row-aligned column layout to work in a narrow portrait column —
  explicitly rejected (proposal.md's "Why"); the ability-beside-its-row layout stays wide-format
  only.
- A true OS-level orientation lock. A plain browser tab cannot force device rotation (the Screen
  Orientation API's `lock()` only works for an installed/fullscreen PWA); the gate is a detect-
  and-prompt overlay, not an enforced lock.
- Popover repositioning — tracked separately in `recenter-rule-popovers`.
- Optimizing tablet-portrait layout — the gate simply doesn't block it; no claim it's tuned.
- Capping the vertical stretch a spanning `.col-unit-abilities` cell (`.spans-component`/
  `.spans-whole-unit`) forces onto the statline rows it covers, even though this is a real
  landscape-height cost distinct from the column-width fix above — deliberately deferred at the
  user's own request, pending a concrete example of the wasted space once the other fixes in this
  change are live to look at.

## Decisions

### Orientation gate: CSS-only detect-and-overlay, not a JS/server gate
`@media (orientation: portrait) and (max-width: 600px)` shows a full-viewport rotate-prompt
overlay; the roster markup still renders underneath, unconditionally, from the server's point of
view. Rotating the device re-evaluates the media query live (native browser behavior), which is
what satisfies the spec's "no reload, no state loss" requirement for free — no JS listener needed.

600px is chosen as a value comfortably between the confirmed-broken real phone width (375px, where
every bug in this change was reproduced) and a typical tablet's portrait width (768px+ for an
iPad) — consistent with this project's existing content-driven (not device-catalog-driven)
approach to breakpoints. Exact value is tunable during implementation/visual QA without changing
the spec, which only commits to "phone-class" vs "tablet-class."

Alternative considered: Screen Orientation API `lock()` for a true forced rotation. Rejected — it
only functions when the page runs installed/fullscreen (a PWA), not in an ordinary browser tab,
which is how this app is actually used.

### Army Rules section: convert from a plain `<div>` title bar to a real `.lp-section`
Today's Rules section is deliberately a plain `<div>` title bar, not a `<summary>` (design-
tokens.md: "since the two sit on structurally different elements"), because it never had a
disclosure state to toggle. Making it collapsed-by-default requires actually converting it to a
`<details class="lp-section">`/`<summary>` pair, matching the All Keywords section's own existing
markup shape exactly — a structural change, not just a default-state flag.

### Unit block collapse: the whole block becomes a `<details>`, name bar becomes `<summary>`
`.unit-block` today is a plain `<section>` containing `<h2 class="unit-name">`, the status
toolbar, and the section list. To collapse all of that behind the name bar, `.unit-block` becomes
a `<details>` and `.unit-name` becomes its `<summary>` — reusing the same full-bar-is-the-trigger
affordance every `.lp-section` summary already uses, rather than a small dedicated icon-button
(the codebase reserves icon-buttons for binary per-unit facts — Half Strength, Battle-shock — not
broad disclosure, per design-tokens.md's own unit-toolbar convention). The existing per-section
`<details class="lp-section">` elements nest inside this outer `<details>` unchanged; nested
`<details>` is valid HTML and each level's `open` state is independent, so collapsing the outer
block hides the inner sections without touching their own state, and re-expanding it restores
whatever each inner section's own state already was.

Unlike the four inner sections (governed by the phase/turn tracker's Expanded/Forced sets), the
outer unit-block collapse has no server-computed "forced" state anywhere in the spec — it is
presentation-only and entirely player-controlled. `live-play.js`'s carry-forward logic needs no
new server field for it: before swapping in a re-rendered unit block's fragment, read the old
DOM's own top-level `<details class="unit-block">.open` and reapply it unconditionally, the
simplest case of the carry-forward pattern already established for the inner sections.

### Statline grid's abilities column: merged into one, not conditionally 2-vs-3-column
**Superseded mid-implementation**, after the real-device round-trip (task 7.4), at the user's own
explicit request. The original decision here (computing per-unit-block whether *any* row-group had
a Model Abilities entry, and switching `.statline-grid` between a 2- and 3-column template
accordingly) was implemented and verified first, then replaced once the user, looking at the
result, reconsidered *why* Model and Unit Abilities were ever two separate columns in the first
place: to distinguish model-scoped from unit-scoped abilities visually. The user decided that
distinction doesn't need its own column — a single stacked list of both is sufficient — while
floating a *future* per-ability scope marker (mirroring the Enhancement "✦" prefix) as the way to
surface the distinction again if wanted later.

The replacement: `_UnitBlock.cshtml` always renders one `.col-abilities` cell per row-group/span
(row-bound, `spans-component`, and `spans-whole-unit` alike), containing `ModelAbilities` then
`UnitAbilities` concatenated (`.Concat()`, preserving the old left-to-right column order as the
stacking order) — never two separate cells. `.statline-grid` becomes a single fixed 2-column
template (`minmax(0,2fr) minmax(0,1fr)`), with no per-unit conditional at all. This also fully
subsumes the original dead-column fix it replaces: with only one abilities column, "a unit has no
Model Abilities anywhere" stops being a case that needs detecting, since there's no longer a
second column that could go empty.

Deliberately **not** changed: the domain/view-model split itself. `Ability.Scope`,
`StatlineBlockViewModel.ModelAbilities`/`UnitAbilities`, and the `ComponentAbilitySpanViewModel`/
`WholeUnitAbilitySpanViewModel` equivalents are untouched — only the Razor rendering layer merges
the two lists when building markup. This keeps the scope data available with no domain rework
needed if the future marker idea above gets picked up.

Alternative considered (for the now-superseded 2-vs-3-column version): a CSS `:has()` selector to
detect and hide an empty column dynamically. Rejected in favor of a server-computed decision — this
project already found one newer CSS feature (anchor-positioning) confirmed unreliable on a real
target browser despite reporting support (design-tokens.md's Popovers section), so a server-
computed value followed this codebase's own established "authoritative decision on the server,
dumb rendering client-side" pattern. Moot now that the column split itself is gone.

### Name-bar and section-title font sizes: both reduced, exact values left to visual QA
Three sizes shrink together, not just the two name bars toward a fixed section-title target:
`.lp-section summary`'s own `0.82rem` comes down too, and `.army-header-name` /
`.unit-block .unit-name` move down from `1rem` to a value that stays a step above whatever the
(now smaller) section-title size ends up being, so the name bar still reads as a higher-priority
heading than a mere section title. Not captured as a spec requirement (pure sizing has no
observable-behavior scenario worth testing); all three values tuned together during the same
emulator pass used to verify the height budget.

### Legacy CSS removal: the confirmed block outright, the ambiguous edges by diff
`.unit-card` (and all its descendants/modifiers, including `selected-defender`) is confirmed fully
orphaned and removable outright, matching the rest of the lines 64-500 block. The four scattered
declarations chained onto the OLD, unprefixed `.weapon-table`/`.weapon-row` base selectors
(`abilities-cell`, `selected-attacker`, `weapon-type-locked`) are individually confirmed dead, but
the base selectors themselves (`.weapon-table`, `.weapon-table th`, `.weapon-table td`,
`.weapon-table td.weapon-name`) are NOT confirmed dead — every element the live page renders under
`.live-play-page .weapon-table` also matches the bare, unprefixed `.weapon-table` selector, so it
may still be silently contributing base cascade properties the newer `.live-play-page .weapon-table`
rule doesn't override. Before removing the bare selectors, diff their declared properties against
`.live-play-page .weapon-table`'s own rule set property-by-property; remove only what's fully
covered, keep (or fold into the prefixed rule) anything that isn't.

## Risks / Trade-offs

- **[Risk]** A CSS-only gate can't force rotation — a player with an OS-level portrait lock sees
  the prompt indefinitely. → **Mitigation**: accepted and intended; the prompt *is* the desired
  experience for a phone deliberately locked to portrait, not a gap to close.
- **[Risk]** Removing the bare `.weapon-table`/`.weapon-row` selectors without the property diff
  above could silently regress the live weapon table (properties it relied on via cascade, never
  restated on the prefixed rule). → **Mitigation**: property-by-property diff before removal (see
  Decisions above); do not remove until confirmed fully covered.
- **[Risk]** Converting `.unit-block` from `<section>` to `<details>` could break any existing
  selector or `querySelector` call that assumes its element type (e.g. an element-qualified
  `section.unit-block` selector, if one exists). → **Mitigation**: grep for element-qualified
  `.unit-block` usages in `site.css` and `live-play.js` before the swap and update any found;
  `<details>` is block-level by default so no visual regression is expected once qualified
  selectors are corrected.

## Migration Plan

Presentation-layer only; no persisted data (localStorage carries only casualty/status values, not
disclosure UI state) needs migrating. Deploy via the existing `docker compose up --build` flow.
Rollback is a plain revert-and-rebuild — no feature flag, matching this project's existing
solo/small-scale deployment practice.

Verification: re-run this session's own overflow-scan script (`evaluate_script` walking every
element for `scrollWidth > clientWidth`) against the emulator post-change to confirm zero
remaining overflow at 375x539, plus a direct height check confirming `.army-header` fits within
315px at 667x315 with a unit sliver visible; finish with one real-iPhone screenshot round-trip in
both orientations, matching the verification discipline already established this session.
