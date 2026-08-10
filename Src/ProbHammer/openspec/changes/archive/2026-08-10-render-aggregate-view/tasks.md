## 1. Page Scaffolding

- [x] 1.1 Add `Pages/LivePlay.cshtml` and `Pages/LivePlay.cshtml.cs` to `ProbHammer.Web`, following the existing `Index`/`ArmyView` Razor Page pattern
- [x] 1.2 `LivePlayModel.OnGet()` calls `ProbHammer.Core.Domain.Examples.View.MyArmy()` and exposes the resulting `List<AttachedUnitAggregateView>` to the page
- [x] 1.3 Confirm the page is reachable at `/LivePlay` with no navigation link required yet (direct URL is sufficient for this change)

## 2. Statline and Weapon Rendering

- [x] 2.1 Render one block per `AttachedUnitAggregateView`, in `MyArmy()`'s returned order
- [x] 2.2 Render each block's `Statlines` (name + M/T/Sv/W/Ld/Oc), grouping entries with identical M/T/Sv/W/Ld/Oc values into a single row that lists all contributing names together; entries with unique values render alone
- [x] 2.3 Verify against `Units.AssaultIntercessorSquad()` (or similar) that "Assault Intercessor" and "Assault Intercessor Sergeant" collapse into one statline row when their values match
- [x] 2.4 Render each block's `Weapons` as a table: Name, Type, Range, A, Skill, S, AP, D, Count — matching `_UnitCard.cshtml`'s column set plus Count
- [x] 2.5 Verify against `Units.SwordBretheren_Marshal()` (or similar) that a weapon shared by Bodyguard and attached Leader renders as one row with the summed count

## 3. Ability and Keyword Rendering

- [x] 3.1 Render `UnitScopedAbilities` as one combined list per block
- [x] 3.2 Group `ModelScopedAbilities` by owning `ModelLine` and render each group under its model-line's heading, separate from the combined list
- [x] 3.3 Render `Keywords` as a single list per block
- [x] 3.4 Handle empty `Statlines`/`Weapons`/`UnitScopedAbilities`/`ModelScopedAbilities`/`Keywords` without throwing or leaving broken markup (e.g. a unit with no unit-scoped abilities)

## 4. Verification

- [x] 4.1 Run the app locally, navigate to `/LivePlay`, and visually confirm every unit from `Examples.View.MyArmy()` renders with correct statlines, weapons (including aggregated counts), abilities (unit- vs model-scoped placement), and keywords
- [x] 4.2 Confirm `Index`/`ArmyView` are unmodified and still function (no shared markup/session state introduced)
- [x] 4.3 `dotnet build` and full test suite pass with no new failures
