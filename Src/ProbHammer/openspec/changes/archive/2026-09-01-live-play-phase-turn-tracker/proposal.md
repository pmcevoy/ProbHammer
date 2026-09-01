## Why

`/LivePlay` has no notion of what phase of the game is being played or whose turn it currently is
— every statline, weapon table, and ability renders unconditionally regardless of relevance to the
current moment. A lightweight, player-set phase/turn tracker gives the page a live "now" to react
to, and independently reduces on-screen clutter during a real game by collapsing sections that
aren't relevant to the current phase (e.g. Ranged Weapons during the Fight phase).

## What Changes

- Add a phase/turn control at the top of `/LivePlay`: a two-row (My Turn / Their Turn) × five
  phase-column (Command, Movement, Shooting, Charge, Fight) clickable grid — each row's own label
  is also independently clickable as a "collapse everything" reset — exactly one of the resulting
  twelve cells is the current selection at all times.
- Persist the selected phase/turn as genuine server-side live state (a new
  `ProbHammer.Core.Domain.Roster.PhaseTurnSelection`, session-stored via a new `IPhaseTurnStore`
  mirroring `ISessionArmyListStore`'s convention) — asserted by the player, never computed or
  inferred by the app, and readable by future features that need to know the current turn/phase
  (e.g. phase/turn-aware ability highlighting).
- Use the selected phase/turn to collapse/expand each unit block's own Statline, Ranged Weapons,
  Melee Weapons, and Keywords sections, following a full turn-and-phase-aware relevance table (see
  `design.md`) — never the Army Header's own Rules or All Keywords sections, which this feature
  does not touch. During "Their Turn", selecting a phase force-resets all four sections (a full
  collapse, then expand only what's relevant); during "My Turn", only the sections a given phase
  explicitly names are touched, leaving any other section exactly as the player last left it.
- No rules interpretation: the tracker only reflects what the player asserts and reorganizes
  existing content for readability — it never gates, triggers, or auto-applies any ability or
  characteristic change.

## Capabilities

### New Capabilities
- `live-play-phase-tracker`: the phase/turn selector control, its live per-session state, and the
  section-relevance model driving collapse/expand.

### Modified Capabilities
- `live-play-view`: existing sections (Statline, Ranged Weapons, Melee Weapons, Keywords) gain
  phase-relevance-driven default collapse/expand state.

## Impact

- `ProbHammer.Web`: a new `IPhaseTurnStore` (mirrors `ISessionArmyListStore`'s session-JSON-round-
  trip pattern), a new Razor partial for the grid control (model-driven, not static), extending
  `LivePlaySyncRequest`/`LivePlayCasualtyService` with a phase/turn adjustment that persists the
  selection and reports which sections it forces, `_UnitBlock.cshtml` changes to bake initial
  section disclosure from the current selection, and `live-play.js` changes so the existing
  fragment-swap carry-forward logic is scoped to only the sections a response marks as forced.
- `ProbHammer.Core`: a small addition — `GameTurn`, `GamePhase`, and `PhaseTurnSelection` in
  `Domain.Roster` ("live game state" is already part of that namespace's stated charter), holding
  only the player-asserted turn/phase value itself. The actual Statline/Ranged/Melee/Keywords
  relevance mapping stays a `ProbHammer.Web` presentation concern — those are this page's own
  section names, not a domain concept.
- No changes to `Domain.Catalogue`/BSData ingestion.
