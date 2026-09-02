# Implementation Notes & Gotchas

Defensive knowledge accumulated during development. Import this file when debugging or working on the relevant subsystem.

---

## AP Sign Convention

AP is stored as a **negative integer** matching the game value (e.g. AP-2 → `-2`). This
convention originated in the archived 10e pipeline (`WeaponVariantProfile.Ap` /
`SimWeaponProfile.Ap`, `AbilityProcessor.EffectiveSave` — see
`legacy/10e-pipeline/.claude/implementation-notes.md` for those specifics) and carries over
unchanged into the live 11e domain model: `WeaponProfile.Ap` (`Domain/Catalogue/WeaponProfile.cs`)
also uses negative integers (`Examples/Datasheets.cs` weapons are authored as `-1`/`-2` etc.). Any
future save-resolution logic built on the 11e model should keep `effectiveSave = save - ap`.

---

## Static Classes and ILogger

Static classes cannot be used as type parameters for `ILogger<T>`. Use `ILoggerFactory.CreateLogger("Name")` for loggers inside static classes.

---

## Razor Issues

### Partial Tag Helper model binding

Non-string tag helper attributes require the `@` prefix; `model="unit"` passes the string literal `"unit"`, `model="@unit"` passes the variable.

### Razor email-address heuristic

Razor treats `@` as a **literal character** (not a code expression start) when it is immediately preceded by a word character (letter, digit, or underscore). This mimics email address handling. `/LivePlay`'s `_UnitBlock.cshtml` statline template uses inline stat labels — `@(block.Statline.T)`, `@(block.Statline.Sv)+` — deliberately wrapped for exactly this reason: the `T`, `v` immediately before `@` would otherwise trigger this heuristic and render as the literal string `T@Model...` instead of the interpolated value.

Fix: always use explicit `@(expr)` syntax when a word character precedes `@`:

```razor
T@(Model.Toughness) &nbsp;Sv@(Model.Save)+&nbsp;W@(Model.Wounds)
```

### Razor and WH40K game notation

`@Model.SomeValue++` and `@Model.FeelNoPain+++` are parsed as C# postfix increment expressions. Use `@(Model.SomeValue)++` and `@(Model.FeelNoPain)+++` to get the `++`/`+++` as literal HTML text. `/LivePlay`'s `_UnitBlock.cshtml` applies this to `@(inv.MeleeInSv)++` (`inv` being `block.Statline.InSv`) for the same reason.

---

## Mobile Viewport Emulation (Testing on a Windows Dev Machine)

This app's real audience is a phone browser at the table (see root `CLAUDE.md`'s Project
Purpose), so mobile layout work needs a trustworthy emulator on the Windows dev machine —
round-tripping every CSS tweak to a real iPhone for a screenshot is too slow to iterate with.
Established during mobile layout debugging on `ftr/11th-edition-view`.

**Confirmed real-device viewport dimensions** (the user's iPhone, cross-checked against
`mybrowsersize.com`, which reports the actual CSS viewport a page receives — already net of the
browser's own top/bottom chrome, not the device's full screen resolution):
- **Portrait: 375×539**
- **Landscape: 667×315**

Use these two exact sizes for any mobile layout check on this project, not a generic "iPhone"
preset — a preset's assumed chrome height won't match the real figures above.

**Use `chrome-devtools-mcp`, not `firefox-devtools-mcp`, for viewport emulation.**
`chrome-devtools-mcp`'s `emulate` tool does a true CDP device-metrics override, decoupled from
the actual browser window — confirmed to hit all three sizes above (and 375×667) exactly, via
`evaluate_script` reading `window.innerWidth`/`innerHeight` back. Viewport string format:
`"375x539x2,mobile,touch"` (portrait), `"667x315x2,mobile,touch,landscape"` (landscape) — the
`x2` is device pixel ratio, `mobile`/`touch` matter for `@media` queries and touch-target
behavior. Registered at user scope via `claude mcp add --scope user --transport stdio
chrome-devtools -- npx chrome-devtools-mcp --isolated` (`--isolated` gives a temp, auto-cleaned
Chrome profile — no separate profile setup needed). A newly-added MCP server's tools only appear
after a full Claude Code session restart.

`firefox-devtools-mcp` was tried first and rejected for this purpose: `set_viewport_size` resizes
the actual OS window rather than overriding device metrics, Firefox enforces a hard ~500–516px
minimum window width that's above both target widths, and browser-chrome overhead (~16px
width/~94px height) eats into the resulting content viewport unpredictably. Firefox's own
Responsive Design Mode (accurate device-metrics override, reachable manually via the hamburger
menu → More Tools) is not reachable through the MCP server at all — `take_snapshot`/`click_by_uid`
only ever see page content, never browser chrome UI, regardless of which Firefox window is
involved.

**Font fidelity is required for the emulator to be trustworthy, not just viewport size.** Even
with an exact viewport match, `font-family: system-ui` resolves to a genuinely different typeface
per platform — Segoe UI on this Windows dev machine, San Francisco on a real iPhone (every iOS
browser, including Firefox and Chrome, is required to render via Apple's WebKit engine, not its
own native engine — still true as of Sept 2026 despite EU DMA/UK CMA pressure toward alternative
engines). Different typefaces have different glyph metrics, so identical CSS at an identical
viewport width can genuinely wrap text differently between the Windows/Chrome emulator and a real
iPhone — confirmed directly on `/Import`: one sentence wrapped to one line on Windows/Chrome and
two lines on a real iPhone at the same 375px width. This is why the project now bundles a single
self-hosted variable font (Inter — see `.claude/design-tokens.md`'s "Typography" section) instead
of depending on `system-ui`: it closes this gap outright (identical glyph metrics on every engine
that loads the file), rather than leaving mobile layout checks in the emulator unreliable and
forcing a real-device screenshot to confirm every change.

Even with viewport size and font both matched, this is still Chrome/Blink emulating a
WebKit-driven device, not a perfect substitute for one — treat the emulator as the fast iteration
loop, and a real-device screenshot as the final confirmation before calling a mobile layout change
done, not as something no longer needed at all.

**WebKit's automatic text-size-adjust ("font boosting") is invisible in the Chrome emulator too,
and can make sibling elements sharing one `font-size` rule render at visibly different sizes on a
real iPhone.** Found via `live-play-landscape-only`'s real-device round-trip: `.unit-name` summary
bars all share one `font-size: 0.88rem` rule, but a real iPhone screenshot showed the one unit name
long enough to wrap onto two lines rendering noticeably larger than every single-line name beside
it. Root cause: with a `width=device-width` viewport meta tag (`_Layout.cshtml`) and no explicit
override, mobile Safari (and other WebKit/Blink-on-iOS browsers, per the engine constraint above)
applies a content-dependent heuristic that inflates a text block's *rendered* size above its
declared CSS `font-size` when it judges the text "too small to read" relative to its container —
the boost amount varies with the specific text's own line count/width, so two elements with
byte-identical CSS can genuinely render at different sizes. `chrome-devtools-mcp`'s viewport
emulation never reproduces this (it's a WebKit/Blink-mobile-only heuristic, not something CDP
device-metrics override triggers), so it was invisible through this project's entire emulator
verification pass and only surfaced on the real-device screenshot. Fixed globally via `html {
-webkit-text-size-adjust: 100%; text-size-adjust: 100%; }` in `site.css`, which disables the
heuristic outright so every declared `font-size` renders literally everywhere — the correct fix
for an app whose own compact, hand-tuned type scale (design-tokens.md's "Scale") already accounts
for readability at the target viewport sizes and doesn't want the browser second-guessing it.
