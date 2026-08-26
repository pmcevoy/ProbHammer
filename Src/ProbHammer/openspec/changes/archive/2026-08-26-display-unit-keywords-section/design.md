## Context

See `proposal.md` - Why/What Changes for motivation. Current state: `_UnitBlock.cshtml` renders
`unit.Keywords` as a single `<div class="keywords">@string.Join(", ", unit.Keywords)</div>` after
the Melee Weapons `<details>`, styled by `.keywords`/`.live-play-page .keywords` in `site.css`
(dim small-caps text, top border, no header). The Statline/Ranged Weapons/Melee Weapons sections
each already follow one consistent markup shape: `<details class="lp-section" data-section="...">`
with a `<summary><span class="section-title">...</span>...</summary>`, guarded by an
`@if (unit.X.Any())` that omits the whole block when empty. Weapon ability keywords already render
as individual chips via `.weapon-tags` (flex-wrap row container) / `.weapon-tag` (the chip itself,
3px-radius bordered pill, `--text-dim` on `--bg2`, uppercase) - see `site.css` around line 1114.

## Goals / Non-Goals

**Goals:**
- Match the existing section markup/disclosure shape exactly, so Keywords reads as a peer of
  Statline/Ranged Weapons/Melee Weapons, not a bolted-on extra.
- Reuse `.weapon-tags`/`.weapon-tag` verbatim for layout and pill styling - no new CSS class for
  the pill itself.

**Non-Goals:**
- No change to how `unit.Keywords` is computed or sourced (`AttachedUnitAggregateView.Keywords` /
  `KeywordResolution.EffectiveKeywords` - untouched).
- No popover/interactivity on keyword pills. Weapon-tag chips are interactive only because they
  resolve against `RuleGlossary` (a weapon ability keyword is a real rule reference); a unit
  keyword ("Infantry", "Character", "Fly") is not a `RuleGlossary` entry and attempting a lookup
  would be a new, unrequested behavior, not a rendering-shape change.
- No work on populating `ModelLine.Keywords` itself (separate vNext idea) - this change only makes
  whatever `unit.Keywords` already contains legible.

## Decisions

**Markup: a fourth `lp-section`, not a new pattern.** Add
`<details class="lp-section" data-section="keywords">` immediately after the Melee Weapons
`<details>`, guarded by `@if (unit.Keywords.Any())` (mirrors the existing Statline/Ranged/Melee
guards and the spec's "Empty Sections Render Without Error" requirement - the section is omitted
entirely, not rendered empty). Title "Keywords" via the same `<span class="section-title">` the
other three summaries use. Alternative considered: keep it a plain non-collapsible strip, just
restyled - rejected per prior discussion, since every other unit-block section is collapsible and a
one-off static section would break that consistency (and forecloses the deferred "toggle all
sections at once" idea, which assumes every section behaves alike).

**Pills: reuse `.weapon-tags`/`.weapon-tag` directly, no new class.** Render each keyword as
`<span class="weapon-tag">@keyword</span>` inside a `<div class="weapon-tags">` container - the
exact same two classes weapon ability chips already use. A keyword pill is non-interactive (see
Non-Goals), so it takes the plain base look, not `.weapon-tag-resolved`'s button/hover treatment
(the shape `.weapon-tag-unresolved` already has, minus the dimmed opacity, since a keyword pill is
not "unresolved" - it just was never a candidate for resolution). Alternative considered: a
dedicated `.keyword-pill` class - rejected per prior discussion (visual-consistency answer), and
also avoids maintaining two near-identical pill definitions that would need to be kept in sync by
hand.

**Old CSS is removed, not repurposed.** `.keywords` and `.live-play-page .keywords` (`site.css`
~line 183 and ~1245) are deleted once the plain-text div they style is gone - the new section needs
no bespoke container rule, since `.weapon-tags`'s existing `display: flex; flex-wrap: wrap; gap:
0.25rem` already is the right layout for a row of pills.

## Risks / Trade-offs

- Reusing `.weapon-tag` couples keyword-pill appearance to any future restyle of weapon ability
  chips → accepted deliberately; that coupling is the explicit point (one visual language for "a
  short bordered fact chip" on this page, not two).
- `.weapon-tag`'s `text-transform: uppercase` will apply to multi-word keywords too (e.g. "DEEP
  STRIKE", "GRENADES") → low risk, no different from how weapon ability tags already render
  multi-word keywords today.

## Migration Plan

Single-page, stateless rendering change - no data migration, no persisted state affected, no
rollback complexity beyond reverting the markup/CSS edit.
