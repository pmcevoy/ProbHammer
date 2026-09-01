## 1. Core: Phase/Turn Domain Types

- [x] 1.1 Add `GameTurn` (`Mine`, `Theirs`) and `GamePhase` (`Command`, `Movement`, `Shooting`,
      `Charge`, `Fight`) enums under `ProbHammer.Core.Domain.Roster`.
- [x] 1.2 Add `PhaseTurnSelection(GameTurn Turn, GamePhase? Phase)` under the same namespace — a
      `null` Phase represents a row-label-only selection. Add a `Default` static (`Mine`,
      `Command`).

## 2. Web: Session-Backed Store

- [x] 2.1 Add `IPhaseTurnStore`/`PhaseTurnStore` in `ProbHammer.Web/Services/`, mirroring
      `SessionArmyListStore.cs` exactly (`Save(ISession, PhaseTurnSelection)`,
      `Load(ISession) -> PhaseTurnSelection?`, plain `System.Text.Json` round trip under its own
      session key).
- [x] 2.2 Register `IPhaseTurnStore` as a singleton in `Program.cs`, alongside
      `ISessionArmyListStore`'s existing registration.

## 3. Web: Relevance Table

- [x] 3.1 Add `LivePlayModel.ExpandedSections(PhaseTurnSelection)` — returns the Expanded set from
      `live-play-phase-tracker`'s "Section Relevance By Turn And Phase" table.
- [x] 3.2 Add `LivePlayModel.ForcedSections(PhaseTurnSelection)` — returns the Forced set from the
      same table.
- [x] 3.3 Unit-test both functions against all twelve selection states (two row labels, ten
      Turn/Phase pairs) directly from the spec's table — no DOM/HTTP involved.

## 4. Web: Initial Page Render

- [x] 4.1 `LivePlayModel.OnGet()`: load the current `PhaseTurnSelection` via `IPhaseTurnStore`
      (default when absent), compute `ExpandedSections`, and thread a per-section disclosure value
      into each unit block's render model.
- [x] 4.2 `_UnitBlock.cshtml`: set each of the four `<details data-section="...">` elements' `open`
      attribute from the passed-in disclosure value, replacing the current unconditional
      "collapsed" markup.
- [x] 4.3 `_PhaseTurnTracker.cshtml` (new partial): render the 2-row x 5-phase-column grid plus the
      two row-label cells, model-driven from `PhaseTurnSelection` so the currently selected cell is
      marked server-side on first paint. This is one page-wide control, not one per unit block.
- [x] 4.4 Add `PhaseTurnSelection PhaseTurn` as a third field on `ArmyHeaderRenderModel`, populated
      by `LivePlayModel.OnGet()` alongside `Header`/`Glossary`. In `_ArmyHeader.cshtml`, render
      `<partial name="_PhaseTurnTracker" model="Model.PhaseTurn"/>` between the existing
      `.army-header-meta` div and the Rules `<details data-section="army-rules">` block.

## 5. Web: Sync Endpoint

- [x] 5.1 Add `PhaseTurnAdjustment(GameTurn Turn, GamePhase? Phase)` and add it as an optional field
      on `LivePlaySyncRequest`.
- [x] 5.2 `LivePlayCasualtyService.SyncAsync`: when a `PhaseTurnAdjustment` is present, save it via
      `IPhaseTurnStore` before rebuilding, expand the affected-unit-index set to every unit in the
      roster, and include the computed `ForcedSections` set once in the JSON response alongside the
      existing per-unit fragment map. Leave the no-adjustment path's response shape and behavior
      unchanged (empty Forced set).

## 6. Client: Scoped Carry-Forward and Grid Wiring

- [x] 6.1 Generalize `swapUnitBlock` in `live-play.js` to accept a `forcedSections` set: sections in
      that set take their `open` state from the fresh markup as rendered; every other section
      carries forward the old DOM's `open` state exactly as today. An empty/absent set must produce
      byte-for-byte today's existing behavior for casualty/status-only syncs.
- [x] 6.2 Add `syncPhaseTurn(turn, phase)`: POSTs a `PhaseTurnAdjustment` to
      `/api/live-play/casualties`, moves `.is-active` to the clicked cell immediately, and swaps in
      every returned unit fragment using the response's reported Forced set (6.1).
- [x] 6.3 Wire click handlers on all twelve grid cells (ten phase cells + two row labels) to call
      `syncPhaseTurn`.

## 7. Manual Verification

- [x] 7.1 Run the app (`docker compose up` or the `run` skill) against a real imported roster at
      `/LivePlay` and confirm, cell by cell against the spec's table: My Turn/Command|Movement|
      Charge only ever touch Statline (a manually-opened Keywords or Ranged/Melee section survives
      selecting any of these); My Turn/Shooting and My Turn/Fight force exactly their three named
      sections and leave Keywords alone; every Their Turn phase and both row labels force-reset all
      four sections even over a manual toggle; the Army Header's Rules and All Keywords sections are
      never affected by any of the above; the selection survives a page reload but resets to My
      Turn/Command after a fresh import (simulating session loss).

      Verified live via `dotnet run` + Firefox devtools against the real `data/gw-app-export.txt`
      roster: default selection is My Turn/Command (Expanded={Statline}); My Turn/Shooting expands
      Ranged, force-closes a manually-opened Melee, and leaves a manually-opened Keywords untouched;
      Their Turn/Fight force-resets all four (closing the still-open Keywords, expanding Statline+
      Melee) even over manual toggles; the selection (Their Turn/Fight) survived a full page reload,
      alongside the Army Rules section's own unrelated `open` state and the All Keywords section's
      own unrelated closed state, both untouched; selecting the "My Turn" row label force-collapsed
      all four sections; a subsequent Battle-shock status toggle (no PhaseTurnAdjustment) left
      manually-set Ranged/Keywords-open, Statline/Melee-closed state exactly unchanged, confirming
      casualty/status-only syncs keep full carry-forward.
