## Context

See proposal.md - "Why" for the motivation (unreliable anchor-positioning on a real device, tight
landscape vertical budget). Today `.rule-popover` positions via a per-instance
`anchor-name`/`position-anchor` CSS custom-property pair emitted by `RulePopoverRenderer`, gated
behind `@supports (anchor-name: --a)`; outside that block it falls back to the browser's own
implicit `[popover]` UA default (`inset: 0; margin: auto`), which happens to already center it.
Nested `[BRACKET]` reference popovers (`RulePopoverRenderer.RenderNestedReference`) reuse the exact
same `BuildRulePopover` call, so today they anchor next to the specific reference text that opened
them - never overlapping their parent.

## Goals / Non-Goals

**Goals:**
- Deterministic, browser-independent centering that does not depend on an implicit UA default
  remaining what it is today, nor on anchor-positioning support.
- An explicit, native close control with zero hand-rolled JS, consistent with this popover
  mechanism's existing "native platform feature" bias (declarative `popovertarget`, ancestor-aware
  auto-popover light-dismiss).
- A nested popover stays visually distinguishable from its parent once both are centered, so
  `live-play-view`'s "Nested Reference Popover" requirement (parent "remains open and visible")
  still holds literally, not just as DOM/dismissal state.

**Non-Goals:**
- No change to glossary resolution, cycle detection (`shownRuleNames`), or which triggers exist.
- No change to the ancestor-aware light-dismiss stacking mechanism itself - only to placement.
- No new JavaScript - the existing mechanism (native `popover="auto"`, `popovertarget`) already
  covers everything this change needs.

## Decisions

**Explicit fixed/centered positioning, not a reused implicit default.** `.rule-popover` gets
`position: fixed; inset: 0; margin: auto;` unconditionally, replacing both the `@supports
(anchor-name: --a)` block and the reliance on the UA's own implicit `[popover]` default. The
rendered result is the same box the fallback already produced today, but stated explicitly rather
than left to "whatever the browser does when nothing else positions it" - the exact kind of
assumption that broke down for anchor-positioning on a real device (CSSOM-confirmed supported, not
actually resolving correctly). `RulePopoverRenderer` stops emitting `anchor-name`/`position-anchor`
inline styles entirely.

**Nested popovers cascade with a small fixed offset, keyed off an explicit depth parameter - not
derived from `shownRuleNames.Count`.** The original plan was to derive depth from that set's size
(0 for top-level, N for N levels of nesting) with no new parameter, since it's already threaded
through for cycle detection. Implementation surfaced a real conflict: the weapon-keyword-chip call
sites (`_UnitBlock.cshtml`'s two `weapon-tag-resolved` triggers) pre-seed `shownRuleNames` with
that chip's own rule name as a self-reference guard, even though the chip itself is a top-level
trigger, not a nested one - deriving depth from the set's count would have wrongly offset those two
(otherwise-correctly-centered) top-level popovers. `BuildRulePopover` instead takes an explicit
`int depth = 0` parameter, incremented by one on each `RenderNestedReference` recursion - decoupled
from the cycle-guard set entirely, so a pre-seeded `shownRuleNames` no longer affects placement. The
panel renders with `data-depth="{n}"`; CSS
defines an increasing `translate()` offset per depth (down-right, 0.75rem/level), capped at the
depth-3 value for anything deeper (domain-model-11e.md confirms 3 is the deepest nesting seen live
against the real corpus; deeper is unobserved and treated as the same case as depth 3, not given
its own rule). The offset is small enough that the parent's own sticky title bar
(`.rule-popover-title`, full width, distinctly colored) peeks out from behind each deeper panel -
giving the player a visible, tappable "this is still open, tap here to get back to it" cue, and
satisfying "tapping outside the second popover but inside the first closes only the second" with an
actual visible target to tap, not just a technically-reachable but invisible one.

Alternatives considered: exact re-center on top of the parent (rejected - silently waters down
"remains open and visible" to "remains open" with no way for the player to tell); anchoring the
nested popover to a screen edge instead of centering it (rejected - introduces a second popover
placement pattern for no strong need, when this page consistently reuses one pattern rather than
adding a new one).

**Close control is a native declarative dismiss, not scripted.** The close button is
`<button popovertarget="p-{id}" popovertargetaction="hide">×</button>`, placed inside
`.rule-popover-title`'s sticky bar. No JS: the Popover API's own auto-popover stack already hides
a popover's open descendants when an ancestor is explicitly hidden (the same stacking behavior tap-
outside-both already relies on today), so closing a parent via its own × also closes any nested
popover open above it in the stack, with no extra wiring.

**A dimmed backdrop uses the native `::backdrop` pseudo-element, not a hand-built overlay.** Every
`popover="auto"` element already gets one automatically in the top layer while open, so
`.rule-popover::backdrop { background: color-mix(in srgb, var(--text) 45%, transparent); }` is the
entire implementation - no extra markup, no JS, no stacking-context management. Color derived from
`--text` via `color-mix` rather than a flat black, matching this codebase's existing "named,
derived token rather than a bare rgba()" convention (`--amber-tint`, `--bg3-tint`). Added mid-
implementation at direct user request (not in the original proposal), after confirming the one real
interaction risk it raises: a nested popover's own backdrop stacks *above* its parent in the top
layer, so opening a nested reference compounds a second dim over the parent's already-established
peeking title bar. Verified visually (not just assumed) that the parent's peeking corner stays
legibly distinguishable even under the compounded dim - captured as its own spec scenario ("A
nested popover's own dimming does not hide its still-open parent") rather than left unstated.

## Risks / Trade-offs

- [Risk] A depth-3+ nested popover's cumulative offset (up to ~2.25rem / 36px) combined with
  `max-height: 70vh` could push its bottom edge close to the viewport's bottom edge on the 315px-
  tall landscape viewport → Mitigation: the offset is small relative to the ~220px height budget at
  that viewport size; `overflow-y: auto` already means body content scrolls rather than getting
  clipped unreadably. Confirm visually during implementation using the project's established
  emulator + real-device landscape check (`implementation-notes.md`'s Mobile Viewport Emulation
  section), not assumed correct from CSS alone.
- [Risk] Relying on `popovertargetaction="hide"` cascading a hide to open descendant popovers is a
  specific behavior of the auto-popover top-layer stack, not independently re-verified here (it's
  the same mechanism tap-outside-both already exercises, but never through an explicit hide button
  before) → Mitigation: verify manually (open a nested popover, tap the parent's × control, confirm
  both close) as part of implementation, not assumed from spec reading alone.
- [Risk] The exact offset magnitude (0.75rem/level) is a starting estimate, not derived from any
  hard constraint → Mitigation: tune against a real-device landscape screenshot if it looks
  cramped or the parent's title bar doesn't read as clearly "still there" - this is a CSS constant,
  not a spec-level fact, so adjusting it later needs no spec change.
