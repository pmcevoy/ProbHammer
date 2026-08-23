## 1. Markup

- [x] 1.1 In `_UnitBlock.cshtml`'s `BuildRulePopover`, add a `<div class="rule-popover-title">
      {triggerHtml}</div>` immediately before the existing `<div class="rule-popover-text">`,
      inside the panel.

## 2. Styling

- [x] 2.1 In `site.css`, add `.rule-popover-title` with the same visual values as `.lp-section
      summary` (background `--bg3-tint`, `color: #fff`, `font-weight: 600`, `font-size: 0.82rem`,
      `text-transform: uppercase`, `letter-spacing: 0.02em`, `padding: 0.35rem 0.6rem`), plus
      `border-radius: 4px 4px 0 0` and `position: sticky; top: 0;`.
- [x] 2.2 Remove `.rule-popover`'s own `padding`; add `padding: 0.6rem 0.75rem` directly onto
      `.rule-popover-text`, preserving that rule's other declarations.

## 3. Verification and docs

- [x] 3.1 Manually verify via `dotnet run`: open an ability popover, a weapon-keyword-chip popover,
      and a nested `[BRACKET]` reference popover; confirm each shows a title bar naming its subject
      in the section-header style, and that a long popover's title stays pinned while its body
      scrolls.
- [x] 3.2 Run the full test suite; confirm no regression.
- [x] 3.3 Update `.claude/design-tokens.md`'s Popovers section and `PROGRESS.md` with a summary of
      this change.
