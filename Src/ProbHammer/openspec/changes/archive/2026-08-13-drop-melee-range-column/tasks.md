## 1. Markup

- [x] 1.1 Remove the `Rng` `<th>` and the `Melee` `<td>` from the Melee Weapons table in
      `LivePlay.cshtml`; leave the Ranged Weapons table unchanged

## 2. Documentation

- [x] 2.1 Update `PROGRESS.md` once implementation lands, per this project's CLAUDE.md
      doc-maintenance rule

## 3. Verification

- [x] 3.1 Run the full test suite (`dotnet test`) and confirm all tests pass
- [x] 3.2 Manually load `/LivePlay` and visually confirm Melee Weapons no longer shows a Range
      column, and Ranged Weapons is unaffected — confirmed via Firefox DevTools MCP
      (`firefox-devtools-mcp`): Assault Intercessor Squad card shows Melee Weapons with columns
      Weapon/A/WS/S/AP/D only, Ranged Weapons unaffected
