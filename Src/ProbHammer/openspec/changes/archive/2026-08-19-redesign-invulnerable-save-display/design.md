## Context

See `proposal.md` - Why. Current rendering lives in `_UnitBlock.cshtml:82-107`, inside the Sv
stat-tile, styled by `.stat-subvalue`/`.stat-subvalue-caveated` in `site.css:678-684`. `/LivePlay`
is a fixed, single (light) visual theme — `.live-play-page` locally redeclares `--bg`/`--bg2`/
`--bg3`/`--accent`/`--text`/`--text-dim`/`--amber`/`--border` (`site.css:482-490`) rather than
adapting to the viewer's OS theme; this change works within that same fixed palette, adding no new
adaptive behavior.

The value being rendered, `InvulnerableSave` (`Domain/Catalogue/InvulnerableSave.cs`), and its
resolution from BSData text (`BsdataDatasheetMapper.ResolveInvulnerableSave`/
`ResolveCaveatAbility`), are unchanged by this design — both already expose everything the new
rendering needs, including `CaveatAbility.Text`.

## Goals / Non-Goals

**Goals:**
- Render the invulnerable save as a distinct box below the main stat-tile row, matching the real
  GW/Wahapedia card pattern, with a side label instead of a value stacked inside the Sv tile.
- Support all four non-caveated shapes the value type allows (uniform, melee-only, ranged-only,
  differing-both-known) with distinct side labels, even though real BSData text can currently only
  produce the first three.
- Give the caveated case an off-color box and the linked ability's actual condition text, so a
  player doesn't have to hunt the abilities column for it.

**Non-Goals:**
- No change to `InvulnerableSave`, `ResolveInvulnerableSave`, or `ResolveCaveatAbility` — this is a
  page-layer (Razor + CSS) change only.
- No "Ability-text interpretation pass" (`vnext-ideas.md`) — the caveat stays opaque about which
  attack type it actually restricts; only its raw text is surfaced, not parsed.
- No general full-text ability rendering elsewhere on the page — the caveat box is a single, scoped
  exception to the standing name-only rule, not a first step toward generalizing it.
- No dark/adaptive theme work — `/LivePlay` stays a fixed single visual world.

## Decisions

**Box placement and shape, twice refined after seeing it live.** First pass: a new element below
`.statline-tiles`' existing row, with the value inside a box and the label as a sibling beside it
(not stacked above, unlike M/T/Sv/W/Ld/Oc). Direct user feedback on that live render drove a second
pass: the InSv element **is now literally a `.stat-tile`** (label on top, value below, inside one
bordered box) — not a separate box-plus-label pair — so it matches the other Statline attributes'
layout exactly, not just their color/border language. This also fixed a real side effect the first
pass had introduced: the wide `.insv-row` (spanning grid columns 3 through the row's end to fit a
horizontal "InSv vs ⚔ / [icon]" label) was letting its content force *every* column it spanned
wider, visibly bloating the padding of the W/Ld/Oc tiles above it. Confining the InSv tile to a
single grid column (`grid-column: 3`, matching Sv exactly — see "Alignment" below) means only that
one column's auto-width can grow, leaving the other five tiles at their original compact size.
Silhouette settled live: the plain rounded rect (matching `.stat-tile`'s `3px` radius) was kept
rather than a shield, reading fine as part of the same tile family once seen rendered.

**Alignment, resolved after a live look and explicit user feedback**: the tile sits in the same CSS
Grid column as the Sv tile (`.statline-tiles` changed from `flex-wrap` to a 6-column `grid`,
`.insv-tile`/`.insv-caveat-text` placed via `grid-column`), so it lines up directly under Sv rather
than starting at the row's left edge (under M) — matching the real GW/Wahapedia layout precisely,
which a flex-wrap row could not do since it has no way to pin a second-row element under a specific
first-row sibling's column.

**Tile compactness, tightened after user feedback.** `.stat-tile`'s `padding`/`min-width` predate
this change (shared by all six M/T/Sv/W/Ld/Oc tiles, not InSv-specific), but the user judged them
"very spacious" once looking closely at the finished page — `padding: 0.2rem 0.4rem` /
`min-width: 2.6rem` tightened to `padding: 0.05rem 0.2rem` / `min-width: 1.8rem`, applying uniformly
to every stat-tile on the page, not just the new InSv one.

**Caveated box color.** Off-color background using a light tint of the existing `--amber` token
(`#a86a1d`), not the saturated token color itself — with `--text` (black) kept as the foreground,
matching the user's explicit call ("black on a light amber background") and the page's existing
convention that stat values are always dark text on a light surface. `--amber` itself stays the
foreground color used elsewhere on the page (casualty-reset icon, filter badge) — this is a new,
separate token (e.g. a `--amber-tint` custom property), not a reinterpretation of the existing one,
so those other uses are untouched. Exact tint value verified live alongside those existing amber
usages on the same rendered page before finalizing, for contrast and to confirm it doesn't read as
the same signal as the unrelated filter/casualty amber accents.

**Icons, resolved live and then relocated across two feedback rounds.** ⚔ (U+2694 Crossed Swords,
Unicode text-presentation-by-default) for melee — confirmed rendering text-coloured rather than as a
full-color emoji. The ranged glyph took three live attempts: ⌖ (Position Indicator), ◎ (Bullseye),
and ⊙ (Circled Dot) were each tried and each rendered ambiguously in this browser/OS font stack — a
diamond, what read as an "info" icon, and a plain dot, none legible as "ranged/target". Settled on a
small inline SVG crosshair (circle + four ticks) instead, matching this file's own filter-flag icon
precedent for the identical reason (a font/emoji glyph varies by platform and can't be trusted to
read as intended). Re-confirmed a third time in a direct side-by-side (⌖ vs. the SVG, same tile
styling, rendered simultaneously) after the user asked why the SVG was needed at all — the diamond
reading was not a one-off impression.

The *label* changed after direct user feedback on the first live render: rather than a side label
that varied with the save's shape (`InSv vs ⚔ / [icon]`, `InSv vs ⚔`, bare `InSv`, ...), the label
is now **always** just "InSv" (or "InSv*" when caveated) — matching M/T/Sv/W/Ld/Oc's own labels,
which never encode a value's shape either. Any attack-type icon instead rides inside the tile's
*value* line, immediately next to the number it qualifies: melee-only shows `⚔ 4+`; ranged-only
shows the crosshair icon + `5+`. Uniform and caveated both show the bare value with no icon at all,
since neither has an attack-type distinction to make — and this was deliberately checked live
against a mocked-up ranged-only tile once the user raised it (thinking ahead to a possible future
where the still-deferred ability-text-interpretation pass flips a caveated save like Canis Rex's to
`Caveated: false`, ranged-only): a bare `5+` (uniform) and an icon-qualified `⌖ 5+` (ranged-only)
are already visually distinct today, since the icon's presence/absence is exactly what the
melee-only/ranged-only vs. uniform branches already differ on — no code change was needed to confirm
this, only a side-by-side look.

**Differing-both-known layout, settled after two live mockup rounds.** First shipped as one line
joined by a `/` (`⚔4+ / [icon]5+`, later respaced to `⚔ 4+ [icon] 5+`), in a smaller
`.stat-value-compact` font so the line wouldn't force the Sv column much wider than its neighbours.
User feedback ("still not great") prompted mocking up two alternatives live, side by side with the
shipped version, before touching any real markup: (A) icon-over-value pairs side by side with a `/`
between them, and (B) two full stacked rows (ranged above melee), no separator needed since the line
break already does that job. **(B) was chosen** — it needs no `/` at all, each row reads as one
complete fact rather than a column pairing to track, and the user specifically liked that ranged-
above-melee mirrors the page's own Ranged Weapons-before-Melee Weapons section ordering (itself
following the game's shooting-before-melee phase order). `.stat-value-compact` changed from a single
smaller-font line to a `flex-direction: column` pair of rows.

**Caveated ability text.** Renders in italics beneath the tile, at the unit block's normal reading
width (not constrained to the tile's own narrow column) — real caveat text runs a full sentence
(e.g. Canis Rex's "This model has a 5+ invulnerable save against ranged attacks.") that won't fit
inside a tile-width column. This is the first place in the unit block rendering `Ability.Text`
rather than only `Ability.Name` — call this out explicitly in a code comment at the render site and
in `domain-model-11e.md`, so a future reader doesn't mistake it for the start of general full-text
rendering (that stays the deferred `vnext-ideas.md` popup idea, unaffected by this change).

**Collapse behavior.** The invulnerable save tile and its caveat text live inside the same
run-collapse region as the M/T/Sv/W/Ld/Oc stat-tile (see the updated `live-play-view` spec's
Statline Section Rendering requirement) — collapsing a fully-dead or fully-deselected run hides
them together with the stat-tile, no separate collapse logic needed.

**Fixtures.** Two additions to `Examples/Datasheets.cs`/`Examples/Units.cs`:
- A Howling Banshee-based unit, using real verified BSData values traced during this change's
  exploration: `M8" T3 Sv4+ W1 Ld6+ OC1`, source InSv text `"4+* / 5+"` (resolves, unchanged by
  this design, to `InvulnerableSave(5, 5, Caveated: true, ...)`), linked ability real text
  `"Models in this unit have a 4+ invulnerable save against melee attacks."` (base rules id
  `9fa6-3128-6c5b-55f6`, `Warhammer 40,000.json`). Exercises the caveated render case against real,
  different-from-`CanisRex()` source text and wording, giving two independent real examples rather
  than one.
- A second, explicitly synthetic unit exercising the differing-both-known case (e.g.
  `InvulnerableSave(4, 5, Caveated: false, CaveatAbility: null)`), documented in its own doc comment
  as hand-constructed rather than sourced from real BSData — mirroring `CanisRex()`'s own precedent
  of stating provenance in its doc comment — since no real corpus text can produce this shape today
  (traced during exploration: `ResolveInvulnerableSave` always collapses a footnoted `/`-split to
  one duplicated digit, and a parenthetical shape always zeroes the other side, so two independently
  known non-zero differing values never arise from parsed text as the mapper is written today).
- `CanisRex()` itself is unchanged — its existing text is genuinely accurate to real data and stays
  as-is.

## Risks / Trade-offs

- **Showing ability text here could be misread as a precedent for showing it everywhere.** →
  Explicit code comment plus a `domain-model-11e.md` note stating the scope boundary.
- **Long caveat sentences could wrap awkwardly at this page's compact type scale.** → Verify against
  the two real sentences now in hand (Canis Rex's and the new Howling Banshee fixture's, which
  differ in length and wording) during `/opsx:apply`, not just one.
- **A light-amber box background must stay legible and distinct from the page's existing amber
  accents** (casualty-reset icon, filter badge use `--amber` as a foreground color on a light
  background already) — → verify live, on the same rendered page, alongside those existing uses.
- **No real corpus data exercises the differing-both-known case** — → covered by the explicitly-
  synthetic fixture; the render logic still supports it as forward-looking, matching the value
  type's own shape, not asserting it's real today.
- **Realized during live verification, not anticipated by this design**: `.statline-tiles`'
  pre-existing `max-height: 6rem` (sized for the old, single-line inline layout) silently clipped
  the caveated case's ability-text line off the bottom of the card — confirmed via a real bounding-
  box check (the element was present, visible, and correctly positioned in the DOM, just cut off by
  the container's `overflow: hidden`). → Fixed by raising `max-height` to `12rem`, comfortably
  covering the tiles row plus the new InSv row plus a wrapped caveat-text line at this page's
  compact type scale.

## Open Questions

None remaining — both questions this section originally raised (ranged-attack glyph/box silhouette;
light-amber tint value) were resolved live during `/opsx:apply`; see "Side-label icons, resolved
live" and "Caveated box color" under Decisions above.
