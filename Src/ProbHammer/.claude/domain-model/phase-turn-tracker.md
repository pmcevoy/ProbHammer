# Phase/Turn Tracker

Full requirements: `openspec/changes/live-play-phase-turn-tracker/`. A player-set phase/turn
control for `/LivePlay`, plus a section-relevance model that drives which of each unit block's own
Statline/Ranged Weapons/Melee Weapons/Keywords sections open by default — never the Army Header's
own Rules or All Keywords sections, which this feature doesn't touch. No rules interpretation: the
tracker only reflects what the player asserts (never computed/inferred) and only reorganizes
existing content for readability.

```
ProbHammer.Core.Domain.Roster.GameTurn = Mine | Theirs
ProbHammer.Core.Domain.Roster.GamePhase = Command | Movement | Shooting | Charge | Fight
PhaseTurnSelection(GameTurn Turn, GamePhase? Phase)   // Domain/Roster/PhaseTurnSelection.cs - see
                                       // record's own doc comment. Placed in Domain.Roster (not
                                       // ProbHammer.Web) specifically so a future Core-level
                                       // capability (e.g. phase/turn-aware ability highlighting) can
                                       // consume it directly with no rework - this record holds only
                                       // the raw asserted value.

IPhaseTurnStore / PhaseTurnStore      // ProbHammer.Web/Services/PhaseTurnStore.cs - see interface's
                                       // own doc comment. Registered as a singleton alongside
                                       // ISessionArmyListStore. Rides the same non-persistent
                                       // session backing that store already does - a lost session
                                       // (see live-play-view's "Live Play Redirects Without An
                                       // Active Import") loses the recorded selection too, an
                                       // accepted, matching limitation rather than a new one.
```

**Section-relevance table** (`ProbHammer.Web.Pages.LivePlayModel`, not `ProbHammer.Core` - those
four section names are this page's own Razor partial concept, not a domain one):

```
UnitBlockSection = Statline | Ranged | Melee | Keywords   // LivePlay.cshtml.cs
LivePlayModel.ExpandedSections(PhaseTurnSelection) -> IReadOnlySet<UnitBlockSection>
                                       // the sections that render OPEN for a given selection - a
                                       // row-label selection expands nothing; Phase == Fight
                                       // expands {Statline, Melee} regardless of Turn (My Turn/
                                       // Fight and Their Turn/Fight name the identical Expanded set
                                       // in the spec's table); My Turn/Shooting is the one
                                       // Turn-sensitive case beyond that (expands {Statline,
                                       // Ranged}; Their Turn/Shooting does not); every other phase
                                       // expands {Statline} alone.
LivePlayModel.ForcedSections(PhaseTurnSelection) -> IReadOnlySet<UnitBlockSection>
                                       // the sections a selection actually DICTATES the state of -
                                       // a superset of ExpandedSections (a Forced-but-not-Expanded
                                       // section is dictated closed). A row label or any Their Turn
                                       // phase forces all four; My Turn/Shooting and My Turn/Fight
                                       // force {Statline, Ranged, Melee}; every other My Turn phase
                                       // forces {Statline} alone. Keywords is in no My Turn phase's
                                       // Forced set - only a row label or a Their Turn phase ever
                                       // touches it.
LivePlayModel.SectionName(UnitBlockSection) -> string
                                       // the lowercase token each section renders as in
                                       // _UnitBlock.cshtml's own data-section attribute and
                                       // live-play.js's forcedSections handling - the one place
                                       // this mapping is defined.
```

Unit-tested directly against all twelve selection states (two row labels, ten Turn/Phase pairs) —
`LivePlayPhaseTurnRelevanceTests` — no DOM/HTTP involved.

**Server computes the table; client applies a scoped carry-forward, not a full reset.** The user's
own rule ("My Turn only collapses the sections a phase specifically names; Their Turn always
collapses everything first") needs each unit's each section's *current* rendered state for every
section outside the Forced set — state that exists only in the live DOM (a manual per-unit,
per-section toggle has never been persisted anywhere on this page). So:
- **Server** (`LivePlayModel.OnGet`, `LivePlayCasualtyService.SyncAsync`) computes `Expanded`/
  `Forced` from the current `PhaseTurnSelection` and renders every unit-block fragment with each
  section's initial `open` state set from `Expanded` — `UnitBlockRenderModel.ExpandedSections`
  (nullable, defaulting to "nothing expanded" so a pre-existing call site that predates this
  feature keeps compiling/rendering unchanged) threads this into `_UnitBlock.cshtml`'s four
  `<details data-section="...">` elements. The sync response additionally reports the Forced set
  once, page-wide (`LivePlaySyncResponse.ForcedSections`, string-encoded via `SectionName`) — empty
  whenever the request carried no `PhaseTurnAdjustment`.
- **Client** (`live-play.js`) generalizes `swapUnitBlock`'s existing "carry forward every open
  section" behavior: a `forcedSections` Set parameter excludes those sections from what gets
  carried forward, so the fresh server markup's own baked-in state wins for them instead; every
  other section still carries forward the old DOM's `open` state exactly as before. An empty/absent
  set (the casualty/status-only sync case) is byte-for-byte today's unchanged behavior.

**Rendering** (`_PhaseTurnTracker.cshtml`, new partial): one page-wide 2-row (My Turn/Their Turn) ×
5-phase-column grid plus each row's own selectable label (twelve plain `<button>`s total, not
popover triggers — this control asserts state, it doesn't explain a rule), model-driven from
`PhaseTurnSelection` so the correct cell is marked `.is-active` server-side on first paint with no
flash-of-wrong-cell. Each phase cell carries `data-turn="mine|theirs"` +
`data-phase="command|movement|shooting|charge|fight"`; each row-label cell carries `data-turn`
only. Since `live-play-touch-target-improvements`, rendered directly by `LivePlay.cshtml` (model
`Model.PhaseTurn`) between the `_ArmyHeader` partial and the unit-block loop — no longer inside
`_ArmyHeader.cshtml` at all (`ArmyHeaderRenderModel` no longer carries a `PhaseTurn` field), so the
tracker is structurally outside the Army Header and unaffected by it becoming collapsible (see
"Army header" in rules-glossary-and-popovers.md). Still one control for the whole page, never one per unit block, unlike the
per-unit status toolbar.

**Unit-block collapse override** (`live-play-touch-target-improvements`): every phase/turn
selection also fully overrides every unit block's own collapsed/expanded state, client-side only
(`live-play.js`, no server involvement) — a row-label selection collapses every unit block to its
name bar (a compact roster list); a specific phase-column selection instead expands every unit
block, so that phase's own Forced/Expanded inner sections are actually visible rather than forced
open server-side yet hidden inside a still-collapsed block. `syncPhaseTurn(turn, phase)` computes
`unitBlocksOpen = phase !== null` (already known before the fetch is sent, since a row-label cell
carries no `data-phase`) and threads it through `applySyncResponse` into `swapUnitBlock`'s
`unitBlocksOpen` parameter — a tri-state override (`null` default preserves `swapUnitBlock`'s
original per-block open/closed carry-forward, used by the unrelated casualty/status-only sync
path; `true`/`false` forces every block open/closed outright). Applied on every phase/turn
selection, not just a one-time transition — reselecting the same cell re-applies it, so a
manually-toggled block doesn't survive the next click either direction.

**Sync endpoint**: `LivePlaySyncRequest` gained an optional `PhaseTurnAdjustment(GameTurn Turn,
GamePhase? Phase)` field, handled by the existing `/api/live-play/casualties` endpoint rather than
a new route (keeps the "one POST, one rebuild, one set of re-rendered fragments" guarantee).
`LivePlayCasualtyService.SyncAsync`, when a `PhaseTurnAdjustment` is present: saves it via
`IPhaseTurnStore` before rebuilding, expands the affected-unit-index set to every unit in the
roster (any unit's disclosure state can change, not just one a casualty/status batch happened to
also touch), and returns the newly selected cell's own `ForcedSections`. The endpoint's response
envelope changed from a bare `Dictionary<int, string>` fragment map to
`LivePlaySyncResponse(Fragments, ForcedSections)` — a breaking shape change for any client reading
it directly; `live-play.js`'s `syncLivePlayState`/new `syncPhaseTurn` both destructure it via a
shared `applySyncResponse` helper. `Program.cs` registers a global
`JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` on `HttpJsonOptions` so `GameTurn`/`GamePhase`
round-trip over this endpoint as their lowercase names (`"mine"`, `"command"`) rather than raw
integers, matching the grid's own `data-turn`/`data-phase` attribute values with no separate
client-side encoding table.
