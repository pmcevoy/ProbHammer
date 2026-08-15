# Implementation Notes & Gotchas

Defensive knowledge accumulated during development. Import this file when debugging or working on the relevant subsystem.

> Entries specific to the archived 10th-edition pipeline (Session JSON serialisation,
> BSData XML parsing, `Enricher`-side name resolution, `Simulation/*`) moved to
> `legacy/10e-pipeline/.claude/implementation-notes.md` alongside that code — see
> `archive-10e-pipeline`. What remains below still applies to live code: `Parsing/ArmyListParser.cs`
> is kept and unmodified pending an 11e rewrite; the AP convention and the generic C#/Razor gotchas
> apply to the current codebase (including the 11e domain model and `/LivePlay`) regardless of
> which pipeline touches them.

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

## Army List Parser — iOS Current Format

`◦` (U+25E6) is **always** a weapon regardless of indent depth. Check for it before the indent-based `•` branching in `ClassifyBulletLine`. Failing to do this causes weapons to be misclassified as model entries on deeply indented lines.

---

## Android Detachment Field

The detachment line appears **after** the force-size line in Android exports (unlike iOS where it appears before). After consuming the force-size line, if `detachment` is still empty, scan forward for the next non-empty, non-points-header line.

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

`@Model.InvulnerableSave++` and `@Model.FeelNoPain+++` are parsed as C# postfix increment expressions. Use `@(Model.InvulnerableSave)++` and `@(Model.FeelNoPain)+++` to get the `++`/`+++` as literal HTML text. `/LivePlay`'s `_UnitBlock.cshtml` applies this to `@(block.Statline.InSv)++` for the same reason.
