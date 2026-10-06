# Proposal

## Why

On an iPhone, submitting an import can land on a blank page at the site root while `/LivePlay`
still shows the previous list. Safari restores the Import tab without reloading it, and by then
the antiforgery cookie the form's token pairs with is gone (it lives only for the browser
session), so the POST fails with an empty 400. The 7-day session cookie added for Cloud Run keeps
the old list alive, which is why this only became visible recently. Separately, a new import keeps
the previous list's casualties, half-strength/Battle-shock flags, condition activations and
phase/turn, because nothing resets them.

## What Changes

- The site root opens the current list: `/` redirects to `/LivePlay`, which already falls back to
  `/Import` when the session has no list. `/` no longer serves the Import page itself.
- `/LivePlay` shows a link to `/Import` at the top of the page. `/Import` shows a link back to
  `/LivePlay` when the session has a current list, so opening Import by mistake loses nothing.
- An Import submission whose antiforgery check fails re-renders the Import page with the pasted
  text kept and a message to press Import again, instead of a blank 400. The check itself stays.
- Only a successful import resets game state: the session's phase/turn selection returns to the
  default, and `/LivePlay` clears its browser-stored casualties, half-strength, Battle-shock and
  condition activations the first time it renders the new import. Visiting `/Import`, or a failed
  import, changes nothing.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `army-list-import`: adds requirements for the site root opening the current list, an expired
  Import form being reported rather than failing blank, the Import page linking back to a current
  list, and a successful import starting a fresh game.
- `live-play-view`: adds a requirement for the link to `/Import` at the top of the page.

## Impact

- `ProbHammer.Web`: `Program.cs` (root route), `Pages/Import.cshtml(.cs)` (antiforgery handling,
  back link), `Pages/LivePlay.cshtml(.cs)` (import link, import id on the page),
  `Services/SessionArmyListStore.cs` and `Services/PhaseTurnStore.cs` (import id, reset),
  `wwwroot/js/live-play.js` (clear stored state for a new import), `wwwroot/css/site.css`.
- Sessions saved before this change carry no import id; their browser state must survive the
  deploy rather than being wiped.
- Docs: `.claude/implementation-notes.md` ("RedirectToPage Is Ambiguous On A Page With Multiple
  Routes" no longer applies once `/Import` has one route), `.claude/domain-model/army-list-import-
  pipeline.md`, `.claude/design-tokens.md`.
