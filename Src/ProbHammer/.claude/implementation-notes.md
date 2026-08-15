# Implementation Notes & Gotchas

Defensive knowledge accumulated during development. Import this file when debugging or working on the relevant subsystem.

> Entries specific to the archived 10th-edition pipeline (Session JSON serialisation, BSData XML
> parsing, `Enricher`-side name resolution, `Simulation/*`, and — once it was confirmed the 11e
> `ArmyListParser` work is a full reimplementation rather than a refactor — the two 10e-format
> `ArmyListParser.cs` gotchas) moved to `legacy/10e-pipeline/.claude/implementation-notes.md`
> alongside that code. What remains below is confirmed still applicable regardless of which
> pipeline touches it: the AP sign convention (verified against the live 11e domain model) and
> generic C#/Razor gotchas (verified still exercised by `/LivePlay`'s own templates).

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

`@Model.InvulnerableSave++` and `@Model.FeelNoPain+++` are parsed as C# postfix increment expressions. Use `@(Model.InvulnerableSave)++` and `@(Model.FeelNoPain)+++` to get the `++`/`+++` as literal HTML text. `/LivePlay`'s `_UnitBlock.cshtml` applies this to `@(block.Statline.InSv)++` for the same reason.
