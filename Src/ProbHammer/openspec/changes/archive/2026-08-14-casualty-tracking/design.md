## Context

See proposal.md - Why. `/LivePlay` (`LivePlayModel.OnGet()`) currently rebuilds every unit block
from scratch on every GET, purely from `Examples.View.MyArmy()` — no session, no persistence, no
POST handler exists on this page today. `AttachedUnitAggregator` and `ModelLine.RemoveCasualties`
already implement the read side of casualties (`attached-unit-tracker`'s existing requirements);
nothing currently calls `RemoveCasualties`. `live-play.js` already has one precedent for
client-side identity keyed off unit structure: `LivePlayModel.SelectKey(componentName,
statlineName, loadoutIndex)` (`"{Component}::{Statline}"`, or with a `::{LoadoutIndex}` suffix for
a specific loadout, `-1` meaning "the whole statline") — introduced for selection-scoped weapon
filtering, scoped to one unit block's ephemeral JS state. `Examples.View.MyArmy()` and
`LivePlayModel.OnGet()`'s ordering are both deterministic (no randomness, fixed sort), so the same
request today always produces the same list of units in the same order.

`collapse-deselected-statlines` already ships a run-collapse mechanism: `live-play.js`'s
`updateRunCollapse()` toggles a `.run-collapsed` CSS class on each `.statline-cell[data-run-index]`
based purely on the client-side `deselected` Set (via `isEntryFullyDeselected`). `AttachedUnitAggregator
.BuildStatlines` currently has a separate, server-side mechanism that produces a stronger effect for
the same underlying situation ("this statline's models are gone") — it drops the
`AggregateStatlineEntry` from `Statlines` entirely once every one of its model-lines reaches 0
remaining (`if (!lines.Any(ml => ml.RemainingCount > 0)) continue;`), rather than rendering it
collapsed. Casualty tracking is the first feature that actually drives `RemainingCount` down through
real interaction, which is what exposes the conflict: an entry a player just zeroed out, and might
need to revert, would have no row left to revert from.

## Goals / Non-Goals

**Goals:**
- A stable coordinate that identifies one model-line (or a specific loadout within a multi-loadout
  statline entry) across separate requests, reusing the existing `SelectKey` convention rather than
  inventing a new one.
- Casualty adjustments applied server-side, through the domain (`ModelLine`), and rendered
  server-side (reusing `LivePlayModel`'s existing view-building code) — no aggregation or
  view-shaping logic duplicated in JavaScript.
- Casualty state readable/writable from `localStorage`, synced to the server on page load and after
  every tap, without a full page reload.

**Non-Goals:**
- No cross-device sync — casualty state is local to the browser/device that recorded it (proposal.md
  - Out of scope).
- No change to `AttachedUnitAggregator.BuildWeapons`/`BuildAbilities` — both already correctly
  exclude `RemainingCount == 0` lines; only `BuildStatlines`' drop-when-dead guard changes (see
  Decisions), and only to keep an entry listed, never to change what feeds weapon/ability
  aggregation.
- No undo history beyond the current remaining count (no "casualty log").
- No change to `live-play-selection-scoped-weapons`' filter state or its own persistence behavior
  (still ephemeral, still resets on reload) — casualty state and filter state are independent, per
  the new "Casualty Tracking Is Independent of Selection Filtering" requirement.

## Decisions

**Coordinate: `(UnitIndex, ComponentName, StatlineName, LoadoutIndex)`, extending `SelectKey` with
a unit-level qualifier.**
`SelectKey` alone is ambiguous across the whole page (it's scoped per-unit-block in JS today,
because each unit initializes its own independent `Set`), but a casualty POST is page-wide and
needs to know which unit a coordinate belongs to. `UnitIndex` is the unit's position in
`LivePlayModel.Units` (the same deterministic, already-sorted list `OnGet()` produces). This reuses
`ComponentName`/`StatlineName`/`LoadoutIndex` (`-1` = whole statline entry) unchanged from the
shipped `SelectKey` convention, rather than introducing a second identity scheme.
Alternative considered: give `ModelLine` a persisted GUID. Rejected — `Examples.View.MyArmy()` has
no storage layer to persist a generated id against, and the page's ordering is already
deterministic, so a positional coordinate is sufficient without adding state anywhere.

**Server applies absolute remaining-count values, not deltas — for every coordinate in the batch,
every request.**
The client computes each new remaining count locally (it already has the current remaining/initial
counts rendered in the DOM) and stores/sends the resulting absolute value per coordinate; the
server clamps each to `[0, Count]` and applies it directly (`ModelLine.SetRemainingCount(value)` —
see below) rather than composing a sequence of +1/-1 operations. This makes the endpoint
idempotent: a retried or duplicated POST re-applies the same absolute values with no
double-counting risk, which a delta-based ("decrement by 1") design would not guarantee under a
retry — and it's what makes resending the whole map safe on every request (see the Endpoint
decision below): replaying the same set of absolute values twice is a no-op, not a double
application.

**`ModelLine` gains `SetRemainingCount(int value)`, clamped to `[0, Count]`; `RemoveCasualties`
becomes a thin wrapper over it.**
`SetRemainingCount` is the one primitive both directions need (a decrease and an increase are the
same operation from the domain's point of view — moving the remaining count to a new clamped
value). `RemoveCasualties(int count) => SetRemainingCount(RemainingCount - count)` keeps the
existing method and its existing tests (`ModelLineTests.cs`) working unchanged.
Alternative considered: a separate `RestoreCasualties(int count)` mirroring `RemoveCasualties`.
Rejected — the server-side call site always has an absolute target value (see above), never a
relative "add N back" amount, so a setter matches the actual call shape more directly than a second
delta method would.

**Endpoint: accepts a batch of `(coordinate, remainingCount)` adjustments, returns rendered HTML
per affected unit, not JSON.**
`LivePlayModel`'s view-building (`BuildUnitBlock`, `GroupStatlines`, `CompressLoadoutLabels`,
`BuildContributionBreakdown`, ...) is exactly the logic this whole change avoids porting to
JavaScript (see the earlier Blazor-WASM discussion this proposal grew out of — the conclusion that
held regardless of that decision: aggregation/view-shaping logic stays server-side). The endpoint
reuses that same code — extracting the current per-unit `<section class="unit-block">` markup in
`LivePlay.cshtml` into a Razor partial (e.g. `_UnitBlock.cshtml`) invoked both by the page's
existing `@for` loop and by the new endpoint — and renders it to a string per affected `UnitIndex`.
The client only ever swaps pre-rendered HTML into the DOM; it never re-derives a count, a label, or
a merge/split decision itself.

**Every request carries the full current `localStorage` map, not just the newest change — a tap is
not a special case.** Caught while implementing the rebuild step: the server has no state between
requests, so it always reconstructs the roster from scratch via `View.MyArmyRoster()` (see below)
and replays whatever batch the request carries on top of that pristine build. If a tap's request
carried only its own single newest adjustment, the rebuild would apply that one change to a fresh
pristine roster and silently lose every earlier casualty from the same session — there is no
server-side accumulation to fall back on. So the client updates its in-memory `localStorage` map on
every tap (adding/overwriting that one coordinate) and then POSTs the *entire* map, exactly as it
already does on page load — one request shape, no tap/load distinction in what's sent. The server
always determines which `UnitIndex`es differ from a pristine rebuild and returns fragments only for
those; on a tap this is almost always the single unit just adjusted (its earlier-session
adjustments are already reflected in the map being resent), and on page load it's whichever units
the stored map affects. A browser with an empty map sends an empty batch and gets an empty
response, so the normal GET-rendered page is left untouched.

**`BuildStatlines`' drop condition narrows from "no survivors" to "never referenced at all."**
Change `if (!lines.Any(ml => ml.RemainingCount > 0)) continue;` to `if (lines.Count == 0) continue;`.
This is the minimal edit that keeps the "never show a statline name the roster didn't actually take"
invariant (`lines.Count == 0` — no model-line references the name at all) while dropping the
now-unwanted "and also skip it once it's fully dead" behavior. `RemainingCount`/`InitialCount` on
the surviving entry are unaffected — both are already summed across `lines` regardless of any
individual line's count, so a fully-dead entry naturally reports `RemainingCount: 0` against its
unchanged `InitialCount`.
Alternative considered: keep the drop behavior for the general case and special-case casualty
tracking to reconstruct a zeroed entry separately for rendering. Rejected — that would mean two
divergent readings of "what statlines does this unit have," one for `/LivePlay`'s casualty UI and
one for the domain's own aggregate view, which is exactly the kind of drift `attached-unit-tracker`
already goes out of its way to avoid elsewhere (e.g. `Loadouts` already lists a fully-removed
model-line rather than hiding it, for the same "stay revertible/auditable" reason).

**Run-collapse gets a second, server-computed trigger alongside the existing client-only one; the
two never merge into a single boolean.**
`StatlineBlockViewModel` (the page-layer run-grouping type `GroupStatlines` already builds) gains a
computed flag — e.g. `IsFullyDead => Entries.All(e => e.RemainingCount == 0)`, using the summed
`RemainingCount` `BuildStatlines` already puts on each `AggregateStatlineEntry` — and
`_UnitBlock.cshtml` renders `.run-collapsed` directly on a dead run's `.statline-cell` in its
initial markup, alongside the existing purely-client-computed collapse for the deselection case.
`live-play.js`'s `updateRunCollapse()` (which only ever runs after a selection change, never touches
markup it didn't already render) must not overwrite a server-rendered `.run-collapsed` with `false`
when a run is dead-but-currently-selected — it ORs its own deselection-based `runCollapsed` value
with a `data-dead="true"` attribute read from the DOM (set alongside the server-rendered class)
rather than resetting the class from scratch. This keeps the two triggers independent per the spec
("Reselecting a fully-dead entry does not expand its run"): selection changes can only ever add or
remove the deselection contribution to a run's collapsed state, never the dead one.
Alternative considered: compute "is this run collapsed" as one combined value entirely server-side,
re-rendering on every selection change too. Rejected — selection state is deliberately client-only
and ephemeral (`live-play-selection-scoped-weapons`'s own non-goal), and routing every filter click
through a server round-trip just to keep one boolean server-authoritative would undo that.

**Casualty control placement: single-loadout header XOR loadout line, never both — and its click
handlers stop propagation before the row's own selection-toggle listener sees the event.**
A multi-loadout entry's header renders a *summed* remaining/initial count with no single
`ModelLine` behind it, so the control can't live there — it renders per-loadout-line instead for a
multi-loadout entry, and on the header itself only for a single-loadout entry (which *is* its one
`ModelLine`). Both header lines and loadout lines are already whole-row click targets for the
`live-play-selection-scoped-weapons` selection toggle (`.statline-toggle`/`.loadout-toggle`,
`data-select-key` click listener on the row); nesting the casualty `−`/`+` buttons inside that same
row means a tap on either button would otherwise bubble up and also fire the row's selection
toggle. Both buttons' click handlers call `event.stopPropagation()` before doing anything else —
same shape as the existing Clear-filter button's `event.preventDefault()` inside `<summary>`, just
stopping propagation to an ancestor's listener instead of a native default action.
Visual/interaction shape: reuses the page's existing `− +1 +` step-control language
(`.claude/web-app.md`), scaled down to two buttons (no middle value label — the row's own
`(remaining/initial)` text already shows the value) right-aligned on the row via `margin-left:
auto`. Bounds (0 remaining for `−`, initial count for `+`) use the native `disabled` attribute
rather than a silent no-op, matching the spec's "no effect" wording with visible feedback for why.

**Client re-initializes a swapped-in unit block through one consolidated `initUnitBlock(unitEl)`,
covering both selection state and provenance-toggle wiring.**
Replacing a `<section class="unit-block">`'s markup discards *all* of its live event listeners, not
only `initUnitSelection`'s selection `Set`. `live-play.js` today wires two independent things at
`DOMContentLoaded`: `initUnitSelection` (per unit block, the `live-play-selection-scoped-weapons`
filter state) and a separate, page-wide `querySelectorAll('.weapon-name-toggle')` pass that gives
every weapon row its provenance-breakdown expand/collapse click handler
(`add-live-play-weapon-provenance`). A swap that only re-runs `initUnitSelection` would leave the
swapped-in node's `.weapon-name-toggle` buttons with no click handler at all — provenance expansion
would silently stop working on any unit that's had a casualty marked. To close this, both are
folded into one `initUnitBlock(unitEl)` function, scoped to a single unit element (the
page-wide `.weapon-name-toggle` query becomes `unitEl.querySelectorAll('.weapon-name-toggle')`),
called once per unit block at `DOMContentLoaded` and again on that one unit element after every
casualty-triggered swap. This gives that unit's provenance toggles fresh handlers, without touching
any other unit block's still-live state or listeners.

**Selection (deselected-key) state survives a swap too — it lives outside `initUnitSelection`'s own
closure.** Corrected after hands-on use: the first implementation let `initUnitSelection` create a
fresh, empty `deselected` `Set` on every call, so a casualty tap on *any* entry in a unit reset
*every* entry's filter in that same unit back to fully-selected — directly contradicting the
already-written "Casualty Tracking Is Independent of Selection Filtering" requirement's own
"Marking a casualty does not change the current filter" scenario, not a judgment call open for
debate. Fixed by moving the `Set` into a module-scope `deselectedByUnit` `Map` keyed by
`data-unit-index`, populated (or reused) at the top of `initUnitSelection` rather than constructed
fresh each call. This is safe because a casualty adjustment never changes a select-key's shape
(`ComponentName`/`StatlineName`/`LoadoutIndex` — `LivePlayModel.SelectKey`) — only `RemainingCount`
does — so a persisted `Set` always still matches the swapped-in markup. A real page reload still
resets every unit to fully-selected, since `deselectedByUnit` is a fresh empty `Map` on each page
load — only in-session swaps within the same load now preserve it.

**Initial page load: pristine content paints first, then the stored casualty state is applied.**
The server has no access to `localStorage` at GET time, so the first paint is always the pristine
army; `live-play.js` posts the stored casualty map on `DOMContentLoaded` and swaps in the affected
units' HTML once the response arrives. See Risks below for the resulting brief flash.

**`swapUnitBlock` carries forward each `<details>` section's open/closed state across a swap.**
Found via hands-on browser testing, not the design discussion: fresh server-rendered markup always
renders every `<details class="lp-section">` closed (`Per-Section Disclosure Defaults to
Collapsed`), so a naive full-node replacement visually snapped an expanded Statline section shut on
every casualty tap. `swapUnitBlock` now records which sections (by `data-section`) were open on the
old node before replacing it, and re-applies `open` to the matching sections on the new node before
inserting it — independent of, and unrelated to, the already-accepted selection-filter reset (this
is purely about disclosure open/closed state, which was never part of that trade-off).

## Risks / Trade-offs

- [A tap-triggered POST carrying only its own single newest adjustment would, against a stateless
  server that always rebuilds from a pristine roster, silently discard every earlier casualty from
  the same session — caught while implementing the rebuild step, not from a repro] → Every POST
  (tap or load) sends the full current `localStorage` map, never just the newest change; see the
  Endpoint decision above. Idempotent absolute-value application (also above) is what makes
  resending the whole map on every request safe rather than wasteful-but-risky.
- [First paint always shows the pristine army before the stored casualty state has round-tripped to
  the server, producing a brief visible "flash" back to marked-dead counts on every reload] →
  Accepted for v1: the round-trip is to the same machine/LAN this app already assumes (per the
  earlier architecture discussion), so the window is small; if it proves distracting in practice, a
  follow-up could hide unit blocks behind a lightweight loading state until the initial sync
  response lands, without changing this design's data flow.
- [The first implementation let `initUnitSelection` build a fresh `deselected` `Set` on every call,
  so swapping a unit block's markup on a casualty tap reset *every* entry's filter in that unit,
  not just the adjusted one — reported by hands-on play as jarring mid-combat (deselecting several
  entries to focus on one ongoing fight, then having them all reselect from an unrelated casualty
  tap on a different entry), and on inspection a direct contradiction of the already-written
  "Casualty Tracking Is Independent of Selection Filtering" requirement's own "Marking a casualty
  does not change the current filter" scenario] → Fixed: selection state now lives in
  `deselectedByUnit` (see the Decisions section above), persisted across a swap of the same unit.
  Page-load-ephemeral filter state (`live-play-selection-scoped-weapons`'s existing behavior) is
  unaffected — only in-session swaps now preserve it.
- [`live-play.js` wires provenance-breakdown expand/collapse (`.weapon-name-toggle`) as a separate,
  page-wide pass at `DOMContentLoaded`, independent of `initUnitSelection` — a swap that only
  re-ran `initUnitSelection` would leave the swapped unit's provenance toggles dead with no click
  handler, silently breaking `add-live-play-weapon-provenance` on any unit that's had a casualty
  marked. Caught during design review, not from a repro.] → Both are folded into one
  `initUnitBlock(unitEl)` (see Decisions above), called on the swapped node after every
  casualty-triggered swap so neither concern can be re-wired without the other.
- [Two independent triggers (`data-dead`, deselection) drive the same `.run-collapsed` class; a
  future edit to `updateRunCollapse()` that resets the class from a freshly-computed boolean instead
  of ORing with the server-rendered `data-dead` flag would silently make a dead run's collapse
  reversible by reselecting it, contradicting "Reselecting a fully-dead entry does not expand its
  run"] → Keep the dead flag on its own `data-*` attribute, never folded into the `deselected` Set
  itself, so the two remain textually separate in `live-play.js` and a future edit to one is less
  likely to accidentally touch the other; covered by the corresponding spec scenario either way.
- [A coordinate's `UnitIndex` depends on `LivePlayModel.OnGet()`'s sort order staying stable; if a
  future change made that order depend on live/mutable state (e.g. sorting by current remaining
  model count instead of initial count) a stored `localStorage` coordinate could silently point at
  the wrong unit after that change shipped] → `OnGet()`'s current sort keys (`IsAttachedUnit`,
  initial total model count, `Name`) are all static, pristine-army properties, not
  remaining-count-dependent, so this isn't a live risk today; flagged so a future change to the sort
  key deliberately considers it rather than breaking it silently.

## Open Questions

None — the remaining unknowns (exact control affordance/icon for the casualty stepper, exact
`localStorage` key name, exact wire shape of the adjustment batch) are presentation/implementation
details deferred to implementation, not decisions that would change the specs or task breakdown.
