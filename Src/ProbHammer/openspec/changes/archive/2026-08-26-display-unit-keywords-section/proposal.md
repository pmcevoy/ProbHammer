## Why

A unit block's effective keywords (`unit.Keywords` — already the correct, already-sorted union of
Datasheet-level and present-`ModelLine`-level keywords, via `KeywordResolution.EffectiveKeywords`)
render today as a single plain comma-joined line below the Melee Weapons section, outside any
`<details>` disclosure and with no section header. It reads as page-footer chrome rather than real
datasheet content, so it's effectively invisible during play — exactly the gap that surfaced when
scoping the (currently unpopulated) per-`ModelLine` keywords vNext idea: even once that lands, its
result has nowhere legible to show up.

## What Changes

- Promote the existing bottom-of-block `.keywords` div into its own `lp-section` disclosure — same
  summary-bar/collapse convention as Statline, Ranged Weapons, Melee Weapons, and Abilities —
  titled "Keywords", positioned immediately after Melee Weapons.
- Render each keyword as its own pill, reusing the existing `.weapon-tag` chip convention (3px
  radius, per `design-tokens.md`'s established chip scale) rather than a comma-joined string.
- No change to keyword data or sourcing — `unit.Keywords` already carries the correct, sorted,
  present-scoped union; this is a rendering-only change.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `live-play-view`: "Keyword Section Rendering" changes from "rendered somewhere in the unit's
  block" to a dedicated, titled, collapsible section rendering each keyword as an individual pill;
  "Per-Section Disclosure Defaults to Collapsed" extends its named list of independently-collapsible
  sections (currently Statline, Ranged Weapons, Melee Weapons) to include the new Keywords section.

## Impact

- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` — replace the trailing `.keywords` div with a
  new `<details class="lp-section" data-section="keywords">` block, keyword pills instead of a
  joined string.
- `src/ProbHammer.Web/wwwroot/css/site.css` — retire the standalone `.keywords`/`.live-play-page
  .keywords` rules (or repurpose for the new pill layout); no new pill style needed, reuses
  `.weapon-tag`.
- No changes to `ProbHammer.Core` — `unit.Keywords`/`AttachedUnitAggregateView.Keywords` are
  unchanged.
