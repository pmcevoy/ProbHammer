## Context

See `proposal.md` - Why. Relevant existing mechanics this design builds on directly:

- Every unit block's Keywords section (`display-unit-keywords-section`) is a plain `<details
  class="lp-section" data-section="keywords">` containing a `.weapon-tags` div of non-interactive
  `<span class="weapon-tag">{keyword}</span>` pills, sourced from `AttachedUnitAggregateView
  .Keywords` (`KeywordResolution.EffectiveKeywords`), which is already casualty-aware (only
  currently-present components/model-lines contribute).
- `_ArmyHeader.cshtml` already renders a Rules section (Army/Detachment columns) above the
  unit-block loop; this change adds a sibling section beneath it.
- `live-play.js`'s `swapUnitBlock` already replaces one unit's whole block on a casualty sync and
  carries forward which `data-section` values were `open` before the swap - this already covers
  "keep a force-expanded Keywords section open across a casualty edit" for free (see Decisions).
- `deselectedByUnit` is the existing precedent for page-lifetime-but-not-reload-persistent,
  per-unit client state living outside any single init closure so it survives a swap.
- `--bg3`/white is the page's existing "filled control" look (unit-toolbar active icons, section
  title bars); `--amber-tint`/`.stat-tile-flagged` is the existing "passive flagged value" look
  (caveated InSv box, Battle-shocked OC tile). `design-tokens.md`'s existing rule: a control gets
  active-state ink directly; a value never does, even when flagged.

## Goals / Non-Goals

**Goals:**
- Let a player pick one or more keywords once, from a single army-wide list, and immediately see
  every unit currently carrying any of them, without opening each unit by hand.
- Keep this entirely a rendering/interaction feature: zero changes to `ProbHammer.Core`, zero new
  fields on any Razor render model, zero changes to the already-shipped per-unit Keywords markup or
  its non-interactive behavior.
- Keep the filter state casualty-aware (tracks what's actually visible right now) without adding a
  second server-side keyword aggregation alongside `KeywordResolution.EffectiveKeywords`.

**Non-Goals:**
- No true search/text-filter box for keywords - a fixed, pre-populated pill list only.
- No hiding of non-matching units - only expansion + highlighting; every unit stays rendered
  regardless of filter state (unlike the existing per-unit weapon/statline selection filter, which
  does visually exclude deselected rows).
- No persistence of active filters across reload (see Decisions), and no attempt to keep a
  currently-selected-but-now-absent keyword visible or restorable.
- No AND/intersection matching mode for this iteration - see Decisions.

## Decisions

**1. The union is computed by scanning rendered DOM, not by a new server-side aggregation.**
`BuildArmyHeader`'s existing `BuildArmyRules` aggregation is deliberately casualty-*independent*
(an ArmyRule is an identity fact). This feature is the opposite: explicitly casualty-*dependent*
("active" units only). Rather than writing a second, differently-scoped aggregation in
`LivePlayModel`/`AttachedUnitAggregator` and inventing a way to push its result into the casualty-
sync JSON response, the union is derived client-side by reading every currently-rendered unit's own
`.weapon-tag` text inside `[data-section="keywords"]` - the same "recompute from what's actually in
the DOM right now" approach `recomputeWeaponSections()` already uses for weapon totals. This keeps
the feature 100% client-side and additive; `ProbHammer.Web`'s Razor render models are untouched.
Alternative considered and rejected: extend `ArmyHeaderViewModel`/`BuildArmyHeader` with a live
union and thread it through the casualty-sync response as a new header fragment. Rejected as
meaningfully more surface area (new response shape, new server aggregation, a second definition of
"currently active keywords" that must stay in lockstep with `EffectiveKeywords`) for no behavioral
benefit over reading the DOM that already reflects that exact truth.

**2. Matching keys off rendered chip text directly - no new `data-keyword` attribute.**
Because per-unit chips gain no click behavior and no new state of their own, JS only needs to
*read* each chip's text (trimmed, case-folded) to decide whether to add a flagged CSS class. This
means the already-shipped `_UnitBlock.cshtml` Keywords markup needs zero changes - satisfying the
proposal's "existing per-unit chips are otherwise unchanged" claim exactly, not just approximately.
Same normalization risk class as `RuleGlossary.Normalize`'s existing text-based matching; not worth
a stronger identity scheme for this feature.

**3. Active-filter state is a page-scope `Set<string>` of keyword text, non-persistent.**
Lives at `live-play.js` module scope, alongside (not inside) `deselectedByUnit` - same reasoning:
must survive a `swapUnitBlock` on any one unit without resetting, but must NOT survive a real page
reload/navigation. This mirrors the per-unit selection filter's own precedent
(`Selection State Does Not Persist Across A Page Reload`) rather than the casualty/half-strength/
battle-shock `localStorage` precedent - deliberate, matches the "quick in-session check" framing
the feature was scoped with, not durable game state.

**4. Visual states split along the existing control-vs-value line, not a shared third style.**
The header's own pill is a real `<button>` control: active state reuses the page's existing filled
`--bg3`/white look (new class, e.g. `.army-keyword-chip.is-active`, not a new colour token). The
*same* keyword's pill inside a matching unit's own Keywords section is flagged with the existing
`--amber-tint` "value" convention (new class reusing that token, analogous to
`.stat-tile-flagged`). Two different classes for two different roles, both built from tokens
`design-tokens.md` already defines. Alternative considered and rejected: make every rendered
instance of a keyword (header and per-unit alike) the same clickable control, sharing one visual
state. Rejected - it would require converting the just-shipped non-interactive per-unit `<span>`
pills into buttons (churning that markup) and would collide with the very control-vs-value
distinction this design otherwise preserves (a per-unit pill would need to be simultaneously "a
control" and "a flagged value" depending on which other keyword is active at the time).

**5. No auto-close, therefore no "opened by filter" bookkeeping is needed at all.**
Since deselecting a filter never collapses a section it opened, applying a filter only ever needs
to *set* `open = true` on a matching Keywords `<details>` - a one-way ratchet, never toggled back.
This is a real simplification over the alternative (tracking which sections were opened
specifically *because of* a filter, vs. manually, so a later auto-close only affects the former) -
that bookkeeping simply isn't needed under the chosen behavior.

**6. `swapUnitBlock`'s existing open-section carry-forward covers re-opening for free; re-applying
flags after a swap does not.** A Keywords section force-opened by an active filter is genuinely
`open` in the DOM at swap time, so the existing carry-forward logic (`openSections` snapshot before
replacing markup) reopens it on the freshly-swapped-in node with no changes needed. What *isn't*
free: the swapped-in node's Keywords chips are brand-new DOM with no flagged class applied yet, and
the header's own union may have changed (a keyword may have dropped out or reappeared). So
`syncLivePlayState()` calls one page-wide `applyActiveKeywordFilters()` pass once after all
fragments in a sync batch are swapped (not once per unit) - it (a) rescans every currently-rendered
unit's Keywords chips to rebuild the header pill list, (b) intersects the active-filter `Set` with
that fresh keyword set (pruning anything no longer present - see Decision 7), and (c) re-applies
flagged classes and force-opens matching sections across every unit currently on the page, not just
the swapped one (an active filter can match units that weren't part of this particular sync).

**7. Pruning and reappearance both fall out of "always rebuild from scratch," no special-case
code.** Every rebuild intersects the active `Set` against the freshly-scanned keyword set. A
keyword that drops out is simply no longer in that intersection, so its filter reads as inactive
from that point on with no explicit "clear this filter" step. A keyword that later reappears is
just a normal, not-yet-active entry in the freshly-scanned set - it renders unselected, which is
exactly "reappears inactive" with no extra logic.

**8. Multi-select uses OR matching only; AND/intersection is out of scope for this iteration.**
Simpler mental model, and covers the primary driving use case (a single-keyword stratagem/ability
check) without ambiguity about how multiple active keywords combine. A compound check (e.g.
"CHARACTER INFANTRY") still works today via two sequential single-keyword checks; it just can't be
expressed as one combined highlight. Revisit only if real play surfaces a concrete need.

## Risks / Trade-offs

- **[Risk]** Text-based matching (trim + case-fold) could theoretically conflate two distinct
  keywords that render identically. → **Mitigation**: accepted; same risk class as
  `RuleGlossary.Normalize`'s existing text-keyed matching, and BSData keyword names are effectively
  unique category names in practice.
- **[Risk]** Section renders empty with JavaScript disabled (no server-rendered fallback content).
  → **Mitigation**: accepted; consistent with every other interactive mechanism already on this
  page (casualty tracking, selection filtering, provenance toggles all require JS today).
- **[Risk]** A page-wide DOM rescan runs on every casualty sync, even when the sync affected only
  one unit far from any active filter. → **Mitigation**: real army lists on this page are small (a
  handful to a few dozen units); this is the same cost class as the existing per-sync per-unit
  listener re-initialization, not a new order of magnitude.

## Migration Plan

Purely additive UI change; no data migration, no persisted-state format change, no feature flag
needed. Ships as a normal commit once implemented and manually verified against a real multi-unit,
multi-casualty `/LivePlay` session.
