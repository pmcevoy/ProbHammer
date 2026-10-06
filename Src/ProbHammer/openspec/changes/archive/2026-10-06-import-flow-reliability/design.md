# Design

## Context

- `Program.cs` gives `/Import` a second route (`AddPageRoute("/Import", "")`). With two routes,
  the Import form's `asp-page="/Import"` renders `action="/"`, so every submission posts to the
  root. That is why a failed submission shows a blank root page.
- Antiforgery validation is the Razor Pages default for POST. A missing or mismatched cookie
  returns 400 with an empty body (reproduced locally: dropping the cookie gives `400 size=0`).
- The session cookie lasts 7 days on Cloud Run (`Cookie.MaxAge`); the antiforgery cookie lasts
  until the browser closes.
- Game state lives in two places. The server session holds the list (`SessionArmyListStore`,
  key `ArmyImport`) and phase/turn (`PhaseTurnStore`, key `PhaseTurn`). The browser's localStorage
  holds `probhammer.livePlay.casualties`, `.halfStrength`, `.battleShocked` and `.activations`.
  `live-play.js` reads them on `DOMContentLoaded` and posts them to the server on its first
  `syncLivePlayState()`.

## Goals / Non-Goals

**Goals:**
- Never show a blank page for a failed import.
- Tie browser game state to the import it belongs to.

**Non-Goals:**
- Turning antiforgery off for Import.
- Moving casualty/status/activation state off localStorage.
- Navigation beyond the two links (no menu or header bar).

## Decisions

**Root: a minimal-API redirect instead of a second page route.** Replace `AddPageRoute("/Import",
"")` with `app.MapGet("/", () => Results.LocalRedirect("/LivePlay"))`. `/LivePlay` already
redirects to `/Import` without a list, so the root needs no session logic of its own. With one
route again, `asp-page="/Import"` renders `action="/Import"`, and the "RedirectToPage Is Ambiguous"
implementation note no longer applies. Checking the session at `/` and redirecting to either page
was rejected because it duplicates `/LivePlay`'s own fallback.

**Antiforgery: validate manually on Import only, and re-render on failure.** Mark `ImportModel`
`[IgnoreAntiforgeryToken]`, inject `IAntiforgery`, and start `OnPostAsync` with
`IsRequestValidAsync(HttpContext)`. On failure, set `ErrorMessage` ("This page had expired - press
Import again.") and `return Page()`. `ExportText` is already bound, so the paste survives, and
rendering the page issues a fresh token and cookie, so the next press succeeds. This keeps the
same check the default filter does; only the failure response changes. Alternatives:
- Dropping antiforgery on Import. Rejected: a forged POST could replace a user's list.
- Handling the failure globally with a filter or status-code page. Rejected as too broad for one
  form, and it couldn't keep the pasted text.

**Import identity: a GUID saved with the list.** `SessionArmyListStore.Save` also writes a new
`ImportId` (GUID) under its own session key. `LivePlayModel` exposes it and `LivePlay.cshtml` puts
it on `<main data-import-id>`. A session saved before this change has a list but no id; `OnGet`
assigns one and saves it, so it is stable from then on. Putting the id on `StoredArmyImport` was
rejected: it's a domain type shared by both import pipelines, and the id is web-session plumbing.

**Browser reset: `live-play.js` compares ids before touching stored state.** First step in the
`DOMContentLoaded` handler, before `initUnitBlock`: read `probhammer.livePlay.importId`.
- If it holds a different id, remove the four state keys.
- In every case, store the page's id.

A missing stored id (state recorded before this change) keeps the existing state. That meets the
"session saved before imports were distinguishable" clause, since the first deploy must not wipe a
game in progress. Clearing from the Import POST response was rejected: a 302 renders no page to run
script on, and a stored id also covers a restored tab.

**Phase/turn reset: on successful import, server-side.** `ImportModel.OnPostAsync` calls a new
`IPhaseTurnStore.Clear(session)` right after `sessionStore.Save`. `LivePlay` then falls back to
`PhaseTurnSelection.Default`, as it does for a session that never recorded one.

**Links: plain anchors in a slim bar.** `/LivePlay`: an `.page-nav` row above `_ArmyHeader`
inside `.live-play-content`, with "Import a new list" linking to `/Import`. `/Import`: the same
`.page-nav` row above the heading, with "Back to current list" linking to `/LivePlay`, rendered
only when `ISessionArmyListStore.Load` returns a list. Styled with existing tokens: `--accent`
link text (as `.rule-reference` links already use), `--border` bottom rule, `0.78rem`, hover fade.
No new colours.

## Risks / Trade-offs

- [The import link sits inside `.live-play-content`, hidden by the portrait orientation gate] →
  Acceptable. `/LivePlay` is unusable in portrait anyway, and `/Import` stays reachable by URL.
- [Two tabs on different imports of the same session would keep clearing each other's state] →
  The session holds one list, so a second tab always renders the same import; no conflict in
  practice.
- [The first `/LivePlay` render after a new import may briefly show server-rendered pristine
  counts before the first sync] → It already renders pristine counts before syncing today; the
  clear just makes that render final.
