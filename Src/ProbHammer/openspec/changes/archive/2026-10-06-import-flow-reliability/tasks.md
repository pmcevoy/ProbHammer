# Tasks

## 1. Site root redirects to /LivePlay

- [x] 1.1 Replace `AddPageRoute("/Import", "")` in `Program.cs` with a `MapGet("/")` local
  redirect to `/LivePlay`. Verify with `ImportFlowTests`: `/` with a list lands on `/LivePlay`, `/`
  without one lands on `/Import`, and the Import form's action renders as `/Import`.
- [x] 1.2 Remove the "RedirectToPage Is Ambiguous On A Page With Multiple Routes" note from
  `.claude/implementation-notes.md` (`/Import` has one route again), and update the root-route
  mention in `.claude/domain-model/army-list-import-pipeline.md`. Verify no doc still says `/`
  serves Import.

## 2. Expired Import form re-renders instead of a blank 400

- [x] 2.1 Mark `ImportModel` `[IgnoreAntiforgeryToken]`, inject `IAntiforgery`, and make
  `OnPostAsync` validate first; on failure set the expired message and return `Page()` without
  importing. Verify with an `ImportFlowTests` test that posts a valid token without its cookie:
  200, the message and the pasted text are in the page, and an existing session list is unchanged.
- [x] 2.2 Add a test that submits again from that re-rendered page (its fresh token and cookie)
  and is redirected to `/LivePlay`. Verify it passes, and that a POST with no token at all still
  imports nothing.

## 3. Successful import starts a fresh game

- [x] 3.1 Save a new `ImportId` GUID in `SessionArmyListStore.Save` and add a loader for it.
  `LivePlayModel.OnGet` assigns and saves one when a list exists without an id, and
  `LivePlay.cshtml` renders it as `data-import-id` on `<main>`. Verify with tests: two imports give
  two different ids, two renders of one import give the same id, and a pre-existing session gains
  a stable id.
- [x] 3.2 Add `IPhaseTurnStore.Clear` and call it in `ImportModel` after a successful save only.
  Verify with tests: phase/turn returns to default after a successful import and is unchanged
  after a failed or expired one.
- [x] 3.3 In `live-play.js`, before any unit-block init on `DOMContentLoaded`, compare
  `probhammer.livePlay.importId` with the page's id. Clear the four state keys only when a
  different id is stored, then store the page's id. Verify in the running app: casualties marked
  on one list are gone after importing another; they survive a reload; they survive visiting
  `/Import` and returning without importing.
- [x] 3.4 Update `.claude/domain-model/army-list-import-pipeline.md` (import id, reset on success)
  and the session/localStorage notes it references. Verify the casualty-persistence description
  mentions the reset.

## 4. Navigation links

- [x] 4.1 Add a `.page-nav` bar with an "Import a new list" link above `_ArmyHeader` on
  `/LivePlay`, and a "Back to current list" link on `/Import` shown only when the session has a
  list. Style both with existing tokens in `site.css`. Verify with tests: the LivePlay link
  renders, the Import back link renders only with a list, and following the Import link changes
  no state.
- [x] 4.2 Add a `.page-nav` entry to `.claude/design-tokens.md` (existing tokens only). Verify it
  names no new colour.

## 5. Integration check

- [x] 5.1 In the running app at the landscape phone viewport (667×315): open `/` with and without
  a list; import, mark casualties, open Import via the link, go back; import a second list and see
  a clean state. Then simulate the stale-tab case by deleting the antiforgery cookie before
  submitting, and confirm the message appears with the paste kept and a second press imports.
