## Why

Today a rule/ability popover (`rules-glossary-popovers`) shows only its body text — the name that
was tapped to open it appears nowhere inside the popover itself, only on the trigger the player has
already tapped away from. A title bar naming the subject makes a popover self-identifying once
open, and gives the ability/Enhancement-name-with-indicator work (`display-resolved-enhancements`)
a place to show the `✦` prefix inside the popover too.

## What Changes

- Every rule/ability popover panel gains a title bar above its body text, showing the same name
  that was tapped to open it (ability name, weapon-keyword chip text, or nested `[BRACKET]`
  reference label).
- The title bar SHALL use the same visual style as an `.lp-section` disclosure summary's
  `section-title` (the dark filled bar used for "Statline"/"Ranged Weapons"/"Melee Weapons"
  headers), applied to the popover's own top edge.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `live-play-view`: new requirement describing the popover title bar's content and styling.

## Impact

- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` (`BuildRulePopover`'s panel markup)
- `src/ProbHammer.Web/wwwroot/css/site.css` (new `.rule-popover-title` rule; `.rule-popover`/
  `.rule-popover-text` padding restructured to accommodate it)
- `.claude/design-tokens.md`, `PROGRESS.md`
