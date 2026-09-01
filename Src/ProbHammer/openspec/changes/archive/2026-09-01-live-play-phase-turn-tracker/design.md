## Context

`/LivePlay` is fully rebuilt server-side on every request from the session's stored
`ParsedArmyList`/`BsRoster` (`.claude/domain-model-11e.md` — "Session-Backed Import"); no
server-held roster graph exists between requests, but the session itself (`ISessionArmyListStore`,
`SessionArmyListStore.cs`) already holds one plain-JSON-round-tripped value keyed off
`HttpContext.Session` — a precedent this change follows for a second, independent value.

Casualty marks and the player-set Half Strength/Battle-shocked statuses follow a different,
purely client-driven pattern: `localStorage` holds the durable state, `live-play.js`'s
`syncLivePlayState()` POSTs the *entire* current map to `/api/live-play/casualties` on every
change, and the server (which never stores this state itself) re-renders only the affected
`_UnitBlock` fragments (`LivePlayCasualtyService`, `LivePlayModel.RebuildRosterWithStatus`). That
pattern exists because the server is otherwise stateless between requests and because those two
flags need no cross-request server memory of their own — the client always resends the full
picture. Phase/turn is different on both counts: the user has asked for genuine server-side
tracking (so a future capability can read "what's the current phase" without the client having to
resend it), and the relevance rule the user specified is asymmetric in a way that needs the
*previous* render's own per-section, per-unit disclosure state as an input — something no
client-resent map can reconstruct on its own (see Decision 2).

Each unit block's Statline/Ranged Weapons/Melee Weapons/Keywords sections are independently
collapsible `<details class="lp-section" data-section="...">` elements (`_UnitBlock.cshtml`), today
always server-rendered collapsed (`live-play-view`'s "Per-Section Disclosure Defaults to
Collapsed"). `live-play.js`'s `swapUnitBlock` already carries forward whichever sections were
`open` on the old DOM node across a casualty/status-triggered fragment swap, so a manually-opened
section isn't visually snapped shut by an unrelated adjustment — this change generalizes that
carry-forward to be selective (Decision 2) rather than removing it.

## Goals / Non-Goals

**Goals:**
- Persist the current phase/turn selection as real server-side state, associated with the same
  session as the imported army, readable by this and future `/LivePlay` server-side rendering.
- Implement the user-specified relevance rule exactly: a row-label or Their-Turn selection forces a
  full reset of all four sections; a My-Turn phase selection touches only the specific sections that
  phase names, leaving every other section — including ones a player toggled by hand — untouched.
- Never touch the Army Header's own Rules or All Keywords sections.

**Non-Goals:**
- No phase/turn-aware ability highlighting or any other consumer of the new server-side value —
  that's explicitly future work this change only needs to not preclude.
- No per-unit or per-section persistence of manual toggles across a page reload — that has never
  existed for this page and stays out of scope; "leave untouched" only ever applies within one
  already-loaded page's lifetime (see Decision 2's discussion of the fresh-render case).
- No new domain rules engine — the relevance table is a fixed, hand-written lookup, not a
  general-purpose expression system.

## Decisions

### 1. `PhaseTurnSelection` is a small `ProbHammer.Core.Domain.Roster` type; the relevance table stays in `ProbHammer.Web`
`GameTurn` (`Mine`/`Theirs`), `GamePhase` (`Command`/`Movement`/`Shooting`/`Charge`/`Fight`), and
`PhaseTurnSelection(GameTurn Turn, GamePhase? Phase)` (a `null` Phase representing a row-label
selection) live in `Domain.Roster` — that namespace's own charter already includes "live game
state" alongside army-list composition, and placing the raw asserted value there (not in
`ProbHammer.Web`) is what makes it usable by a future Core-level capability (e.g. matching an
ability's own extracted turn/phase relevance against the live selection) with no rework.

The Statline/Ranged Weapons/Melee Weapons/Keywords **relevance table** itself — the Expanded/Forced
sets from the proposal's table — stays in `ProbHammer.Web` (as static methods on `LivePlayModel`,
matching that file's existing convention of hosting this page's own static view-building logic).
Those four names are this page's own section names, not a domain concept; `ProbHammer.Core` has no
reason to know they exist.

**Alternative considered:** put the whole relevance table in Core too (e.g. a
`SectionRelevance.Resolve(PhaseTurnSelection)` domain method), on the theory that "what's relevant
right now" is itself a domain fact. Rejected — the table's *keys* are Razor partial section names,
not a rules-of-40k concept; a future ability-highlighting feature will consume `PhaseTurnSelection`
directly, not this page's section-visibility table.

### 2. Server computes the rule table; client applies a *scoped* carry-forward, not a full reset
The user's own rule ("My Turn only collapses the sections a phase specifically names; Their Turn
always collapses everything first") requires knowing each individual unit's each individual
section's *current* rendered state for every section outside the Forced set — state that exists
only in the live DOM (a manual per-unit, per-section toggle has never been persisted anywhere, on
this page, even before this change). That rules out computing the "final" state entirely
server-side and just re-rendering it: the server has no record of, say, "this one unit's Keywords
section is manually open right now."

So the split is:
- **Server** (`LivePlayModel`) computes, from the just-updated `PhaseTurnSelection`, the two sets
  from the proposal's table: `Expanded` (open) and `Forced` (the sections this selection actually
  dictates at all). It renders every unit-block fragment with those Forced sections baked in
  (open/closed per Expanded), and the sync response additionally reports the Forced set itself
  (once, page-wide — the same set applies to every unit).
- **Client** (`live-play.js`) generalizes the existing `swapUnitBlock` carry-forward: instead of
  always carrying forward every section's prior `open` state, it now carries forward only the
  sections **outside** the response's reported Forced set, and takes the fresh server markup as-is
  for every section **inside** it. A casualty/status-only sync (no phase/turn change) reports an
  empty Forced set, so every section carries forward — byte-for-byte today's existing behavior,
  unchanged.

This means the *rule table* lives in exactly one place (C#, testable in isolation), while the
*DOM-state carry-forward* — which inherently needs live per-unit DOM state — stays where that state
actually lives (the client), generalized rather than duplicated.

**Alternative considered:** track every unit's every section's open/closed state server-side (e.g.
in Session, alongside `PhaseTurnSelection`), so the server alone could compute and render the true
final state on every request. Rejected — this would need a per-unit-index, per-section persisted
map that changes on every manual toggle (a new POST on every single disclosure click, where today
that's a free, zero-network client-side action), a much larger surface for no benefit: nothing
outside this one page's own live DOM ever needs a manual toggle's value, and it already doesn't
survive a reload today (Non-Goals).

**Alternative considered:** keep computing the full relevance table client-side in JS (the original
draft of this design, before turn-awareness was specified), reading current DOM state directly with
no server round-trip at all. Rejected once phase/turn needed to be genuine server-side state
(Decision drivers, Goal 1) — the rule table would otherwise exist in two places (JS for applying it
live, C# for anything future server-side code wants), and the user asked specifically for
server-side tracking.

### 3. One endpoint, extended: `PhaseTurnAdjustment` joins `LivePlaySyncRequest`
`LivePlaySyncRequest` gains an optional `PhaseTurnAdjustment(GameTurn Turn, GamePhase? Phase)`
field, handled by the existing `/api/live-play/casualties` endpoint
(`LivePlayCasualtyService.SyncAsync`) rather than a new route — this keeps the existing "one POST,
one rebuild, one set of re-rendered fragments" guarantee that endpoint's own doc comment already
states (avoiding two independent requests that could race each other's fragment swap). When present,
the handler:
1. Saves the new `PhaseTurnSelection` via the new `IPhaseTurnStore` (mirrors `ISessionArmyListStore`
   exactly — a session-JSON round trip under its own key).
2. Expands the affected-unit-index set to every unit in the roster (every unit's disclosure state
   can change), unioned with whatever casualty/status adjustments were also in the same request.
3. Includes the computed Forced-section set once in the JSON response, alongside the existing
   per-unit fragment map.

A request with no `PhaseTurnAdjustment` behaves exactly as today (empty Forced set, full
carry-forward, unchanged response shape plus one new always-present but empty field).

### 4. Initial GET needs no carry-forward distinction
On a fresh page load there is no prior render to carry anything forward from, so `LivePlayModel.
OnGet()` just renders each section's initial `open` attribute directly from the current selection's
Expanded set (ignoring Forced entirely — an unnamed section's sensible fresh-load default is simply
closed, the same baseline this page has always used). The Mine-vs-Theirs carry-forward asymmetry
(Decision 2) is therefore a live-page-interaction concern only; initial SSR needs one function
(`ExpandedSections`), not two.

### 5. One control, rendered inside `_ArmyHeader.cshtml`, not one per unit block
This is page-wide state, not per-unit — unlike the per-unit status toolbar (Half Strength/
Battle-shock/Reset Casualties, one per unit block), exactly one selector renders for the whole page.
It belongs inside `_ArmyHeader.cshtml` itself, between the existing `.army-header-meta` div and the
Rules `<details data-section="army-rules">` block — a new `_PhaseTurnTracker.cshtml` partial,
rendered via `<partial name="_PhaseTurnTracker" model="Model.PhaseTurn"/>` at that exact point.
`ArmyHeaderRenderModel` gains a third field, `PhaseTurnSelection PhaseTurn`, populated by
`LivePlayModel.OnGet()` alongside `Header`/`Glossary` (both already come from one
`ArmyRosterBuildResult`-driven build; `PhaseTurn` is the one field on this record sourced from
`IPhaseTurnStore` instead). The sync endpoint never re-renders `_ArmyHeader` — only `_UnitBlock`
fragments — so no second wiring point is needed there.

The grid itself is model-driven (`PhaseTurnSelection`), so the server marks the correct one of
twelve cells (`data-turn="mine|theirs"` + `data-phase="command|movement|shooting|charge|fight"` for
the ten phase cells; `data-turn` only, no `data-phase`, for the two row-label cells) as currently
selected on first render, with no flash-of-wrong-cell. Clicking any cell POSTs its `{turn, phase}`
(phase omitted/null for a row-label cell) via a new `syncPhaseTurn()` function in `live-play.js`,
moves the `.is-active` class to the clicked cell immediately (unambiguous — it's exactly what was
clicked, no need to wait on the response for this part), and swaps in every returned unit fragment
using the scoped carry-forward from Decision 2.

## Risks / Trade-offs

- **[Risk]** `IPhaseTurnStore` rides the same `ISession`/`AddDistributedMemoryCache` backing
  `ISessionArmyListStore` already uses, which does not survive an application restart and is tied
  to a non-persistent session cookie by default. → **Mitigation**: this is an accepted, *matching*
  limitation, not a new one — `live-play-view`'s "Live Play Redirects Without An Active Import"
  requirement already sends the player back to `/Import` under those exact conditions, at which
  point losing the phase/turn selection too is a non-issue (the whole page's state is starting
  over). No attempt is made to give phase/turn independent durability the imported roster itself
  doesn't have.
- **[Risk]** The scoped carry-forward (Decision 2) is a materially different code path from today's
  always-carry-forward `swapUnitBlock`, and a bug there could silently drop a manual toggle it
  should have preserved (or fail to force a section it should have). → **Mitigation**: the
  Forced-set computation is one small, table-driven function in C#, directly unit-testable against
  all twelve selection states without touching the DOM/JS at all; the JS-side change is a single
  generalization of one existing function (an added `forcedSections` parameter), not new logic
  invented at the client.
- **[Trade-off]** A row-label selection or a Their-Turn phase always force-closes Keywords even if a
  player had it manually open moments before — the same accepted trade-off the original design
  flagged, now precisely scoped by the user's own rule rather than an assumption.
