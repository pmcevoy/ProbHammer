## Context

`_UnitBlock.cshtml`'s `BuildRulePopover(triggerHtml, triggerClass, ruleText, shownRuleNames)` builds
both a trigger `<button>` and its paired `<div class="rule-popover" popover="auto">` panel; the
panel today contains only `<div class="rule-popover-text">{bodyInline}</div>`. `triggerHtml` is
already the exact, pre-rendered-safe HTML used as the trigger's own inline content — an
`HtmlEncode`'d name for a top-level ability/chip trigger, or an emphasis-rendered label for a
nested `[BRACKET]` reference (see `RenderNestedReference`).

`site.css`'s `.lp-section summary` rule (background `--bg3-tint`, `color: #fff`, `font-weight: 600`,
`font-size: 0.82rem`, `text-transform: uppercase`, `letter-spacing: 0.02em`, `padding: 0.35rem
0.6rem`, `border-radius: 3px`) is the section-header style this change's title bar reuses. There is
no standalone `.section-title` rule — that class is bare markup, styled entirely through its parent
`.lp-section summary` selector — so this change defines its own `.rule-popover-title` rule carrying
the same values rather than trying to share a selector across two structurally different elements
(a `<summary>` vs. a plain `<div>`, one interactive/toggling, one not).

See proposal.md for motivation and scope.

## Goals / Non-Goals

**Goals:**
- Every popover panel shows a title bar naming what was tapped, styled like the page's existing
  section headers.
- Zero new data plumbing — the title bar's content is the same `triggerHtml` already passed into
  `BuildRulePopover`.

**Non-Goals:**
- Making the title bar itself interactive (e.g. a close button) — dismissal stays exactly as it is
  today (tap outside, per the existing "Popover Dismissal" requirement).
- Changing trigger button styling — only the panel gains new content/structure.

## Decisions

**Reuse `triggerHtml` verbatim for the title bar; no new parameter.** `BuildRulePopover` already
receives exactly the HTML that should appear as the title — passing it into a second element inside
the panel is a one-line change (`<div class="rule-popover-title">{triggerHtml}</div>`) with no
signature change and no new call-site plumbing. This is also why `display-resolved-enhancements`'
`✦` indicator (applied to `triggerHtml` at the call site, upstream of `BuildRulePopover`) "just
works" inside the popover title too, with no coordination needed between the two changes beyond
sequencing.

**`.rule-popover` becomes an unpadded shell; padding moves to `.rule-popover-title` and
`.rule-popover-text` individually, each keeping its own.** The title bar needs to span the panel's
full width flush to the top/sides (matching how `.lp-section summary` spans its own container), which
a single outer `padding: 0.6rem 0.75rem` on `.rule-popover` would prevent. `.rule-popover-title`
gets `border-radius: 4px 4px 0 0` (matching `.rule-popover`'s own `4px` card-scale radius on just
the top corners); `.rule-popover`'s `overflow-y: auto` (already present, for long text) clips both
children to its own rounded corners regardless, so the explicit radius on the title is a
belt-and-suspenders match for browsers that don't clip via `overflow` cleanly.

**Title bar is `position: sticky; top: 0` within the popover's own scroll container.** A long rule
text can make `.rule-popover` (capped at `max-height: 70vh`, `overflow-y: auto`) scroll internally;
keeping the title pinned during that scroll matches how the page's own `.lp-section` section
headers behave (always visible, never scrolled past) and costs nothing extra since the title bar's
background is already opaque.

## Risks / Trade-offs

[A nested reference's rendered label can itself carry emphasis markup (bold/italic/small-caps
spans) — embedding it a second time inside the title bar duplicates that markup, which is
harmless but slightly redundant styling inside an already-uppercase, already-bold title bar] →
Accepted: the title bar's own `text-transform: uppercase`/`font-weight: 600` dominate visually
regardless of any inner emphasis span, and using the exact same rendered label (rather than a
separately-derived plain-text version) keeps this change to the one-line reuse described above.
