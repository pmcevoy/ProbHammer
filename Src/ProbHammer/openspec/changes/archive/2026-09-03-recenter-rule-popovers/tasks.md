## 1. Remove the anchor-positioning mechanism

- [x] 1.1 Remove the `@supports (anchor-name: --a)` block from `site.css` (`.rule-popover`'s
      `position-area`/`position-try-fallbacks`/`margin` rules).
- [x] 1.2 Remove the inline `anchor-name`/`position-anchor` style attributes from the
      trigger/panel markup in `RulePopoverRenderer.BuildRulePopover`.

## 2. Centered positioning

- [x] 2.1 Add explicit `position: fixed; inset: 0; margin: auto;` to `.rule-popover` in
      `site.css`, replacing reliance on the browser's implicit `[popover]` UA default.
- [x] 2.2 Confirm the existing `max-width`/`max-height` rules still produce a consistent,
      correctly centered panel at the landscape (667x315) viewport size established in
      `implementation-notes.md`. Portrait is out of scope: `/LivePlay` renders only a
      `.rotate-prompt` (`.live-play-content` set to `display: none`) at portrait widths ≤600px
      per `live-play-landscape-only`, so no popover trigger is ever reachable there. Verified via
      `chrome-devtools-mcp` against a real imported roster: a 352x220.5px panel in the 667x315
      viewport, margins exactly (667-352)/2=157.5px and (315-220.5)/2=47.25px on each axis.

## 3. Nested popover cascading offset

- [x] 3.1 In `RulePopoverRenderer.BuildRulePopover`, add an explicit `int depth = 0` parameter
      (incremented by `RenderNestedReference` on each recursion) and render it as a
      `data-depth="{n}"` attribute on the panel `<div>` - decoupled from `shownRuleNames.Count`,
      since the weapon-tag-chip call sites pre-seed that set with their own rule name as a
      self-reference guard even though they're top-level triggers (see design.md's revised
      "Decisions").
- [x] 3.2 Add CSS rules for `.rule-popover[data-depth="1"]`, `[data-depth="2"]`, and
      `[data-depth="3"]` applying an increasing `translate()` offset (down-right, ~0.75rem per
      level); treat any depth beyond 3 the same as depth 3 (no dedicated rule needed - unobserved
      in the real corpus per `domain-model-11e.md`).
- [x] 3.3 Visually verify a nested popover's offset leaves the parent's own title bar visible
      behind it, at the landscape viewport size from 2.2 - tune the 0.75rem constant if it reads
      too cramped or the parent doesn't read as clearly still open. Verified: opening the
      "Precision" reference nested inside an open "Templar Vows" popover leaves the parent's teal
      title bar corner clearly peeking out top-left of the nested panel. 0.75rem read fine at this
      viewport, no tuning needed.

## 4. Explicit close control

- [x] 4.1 Add a close button (`popovertarget="p-{id}" popovertargetaction="hide"`) into the
      `.rule-popover-title` markup emitted by `RulePopoverRenderer.BuildRulePopover`.
- [x] 4.2 Style the close control in `site.css` - sizing/placement within the sticky title bar,
      following this project's existing token/scale conventions (no new colors or radii).
- [x] 4.3 Manually verify that dismissing a parent popover via its own close control also closes
      any nested popover open above it in the stack (design.md's "close-cascades-to-descendants"
      risk - not previously exercised through an explicit hide button, only tap-outside). Verified:
      with "Templar Vows" (parent) and "Precision" (nested) both open, clicking the parent's own
      close control closed both in one action, confirming the auto-popover top-layer stack cascades
      a hide from an ancestor to its open descendants exactly as design.md predicted.

## 5. Dimmed backdrop (added mid-implementation at direct user request)

- [x] 5.1 Add `.rule-popover::backdrop { background: color-mix(in srgb, var(--text) 45%,
      transparent); }` to `site.css` - the native `::backdrop` pseudo-element every
      `popover="auto"` element gets automatically, no JS/markup needed.
- [x] 5.2 Visually verify the compound-dim case: with a nested popover open on top of its parent,
      confirm the parent's own peeking corner (per 3.3) stays legibly distinguishable even with a
      second backdrop layered over it, since a nested popover's own `::backdrop` stacks above its
      parent in the top layer. Verified: parent's peeking corner remained clearly visible under
      the compounded dim.

## 6. Verification

- [x] 6.1 Run `dotnet test` to confirm no regression in the existing automated suite (this change
      is presentation-only, so no test changes are expected). 473 passed, 13 skipped (explicit
      corpus scans, unaffected by this change), 0 failed.
- [x] 6.2 Exercise `/LivePlay` in the `chrome-devtools-mcp` emulator at the landscape viewport size
      (portrait is out of scope - see 2.2): open a top-level popover, then a nested `[BRACKET]`
      reference popover from inside it; confirm centering, the cascading offset, and both
      dismissal paths (tap-outside and close control) all behave per the updated spec. Verified
      end-to-end against a real imported Black Templars roster (`data/gw-app-export-templars-
      latest.txt`): "Templar Vows" top-level popover, its nested "Precision" `[BRACKET]` reference,
      centering math, cascading offset, close-cascades-to-descendants, tap-outside dismissal, the
      dimmed backdrop, and the compound-dim case all confirmed (see 2.2/3.3/4.3/5.2 for specifics).
- [x] 6.3 Confirm the same behavior on a real device before calling this done, per
      `implementation-notes.md`'s standing note that the emulator is the fast iteration loop, not a
      substitute for a final real-device check. Confirmed by the user directly on their iPhone -
      works well on the small device.
