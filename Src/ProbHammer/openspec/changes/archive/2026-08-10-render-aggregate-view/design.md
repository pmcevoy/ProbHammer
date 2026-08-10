## Context

`ProbHammer.Web` already references `ProbHammer.Core` (`ProbHammer.Web.csproj`), so no new project reference is needed. `Index`/`ArmyView` are built around the 10e pipeline (`ArmyListParser` → `Enricher` → session JSON → `ArmyView.cshtml`); none of that touches the 11e `Domain.Catalogue`/`Domain.Roster` types. This change adds a page that reads the 11e domain model directly and has no dependency on the 10e pipeline, session, or `SessionJson`.

The data for this change is intentionally static: `ProbHammer.Core.Domain.Examples.View.MyArmy()` already returns `List<AttachedUnitAggregateView>` built from the hand-entered example army (`Examples/Units.cs`, `Examples/Datasheets.cs`). The page's `OnGet` can call this directly with no async work, no DI beyond what Razor Pages provides for free, and no error path for missing/malformed input (there is none — it's compiled example data).

## Goals / Non-Goals

**Goals:**
- Render every `AttachedUnitAggregateView` in `View.MyArmy()` on one page, one unit-block per view.
- Cover all five components of the view per unit: statlines, weapons (name, type, A/Skill/S/AP/D, live count), unit-scoped abilities, model-scoped abilities (grouped under their owning model-line), effective keywords.
- Keep the page visually distinct from `Index`/`ArmyView` (different route, no shared session/session-dependent partials) so it's obviously a separate, in-progress surface.

**Non-Goals:**
- No casualty interaction (tap-to-remove, spinner) — deferred to a follow-on change once this render is validated.
- No wiring to a real pasted army list — there is no 11e `ArmyListParser`; that's separate, deferred work.
- No reuse of `_UnitCard.cshtml` — that partial is coupled to `UnitProfile` (10e Contracts shape) and to session-driven weapon-selection JS; the two data shapes (`UnitProfile` vs `AttachedUnitAggregateView`) are different enough that forcing a shared partial now would mean bending one or the other. A new, small partial is added instead — noted as a candidate for later convergence, not attempted here.
- No JS interactivity (no click-to-select weapons, no highlighting) — purely server-rendered HTML.

## Decisions

**New route `/LivePlay`, not a mode on `ArmyView`.** `ArmyView` renders session-stored `List<UnitProfile>` (10e Contracts). Bolting an 11e-shaped render onto the same page would mean branching the whole page on which domain model is in play. A separate page keeps the two fully decoupled, matching how `.claude/domain-model-11e.md` already treats the two models as coexisting-but-independent.

**`LivePlayModel.OnGet()` calls `Examples.View.MyArmy()` directly — no service layer, no DI registration.** The data is compiled-in example data, not a runtime dependency; introducing an interface/service for it would be indirection with no current second implementation. If a later change wires in real parsed data, that's the natural point to introduce a service seam — premature here.

**A small `AttachedUnitAggregateView` → view-model mapping happens in `LivePlay.cshtml.cs`, not in `ProbHammer.Core`.** The view already exposes everything needed (`Statlines`, `Weapons`, `UnitScopedAbilities`, `ModelScopedAbilities`, `Keywords`); the only work is grouping `ModelScopedAbilities` by `ModelLine` for display, which is a presentation concern, not a domain one. Keeping it in the page model avoids adding rendering-shaped code to `ProbHammer.Core.Domain`.

**Weapon row columns mirror `_UnitCard.cshtml`'s existing weapon table** (Name, Range, A, Skill, S, AP, D) plus a live Count column, using `WeaponProfile.Skill`'s computed property so Ranged/Melee weapons share one column. This keeps the visual language consistent with the 10e page for anyone comparing the two, without sharing markup.

**No test coverage for the Razor markup itself**, consistent with `Index`/`ArmyView` (neither has view tests today). If the `ModelScopedAbilities`-grouping logic in the page model grows non-trivial, it gets extracted into a testable helper and covered with unit tests against hand-built `AttachedUnitAggregateView` fixtures — anticipated but not required unless the grouping turns out more complex than a simple `GroupBy`.

## Risks / Trade-offs

- **Static data means this page can't yet be used at the actual table.** → Accepted: the proposal explicitly scopes this to proving the render works before any parser/session wiring is designed.
- **A second, separate weapon-table markup (this page's + `_UnitCard.cshtml`'s) means future styling drift.** → Accepted for now per Non-Goals; flagged as a convergence candidate once the 11e page's shape stabilizes (e.g. once casualty interaction is added and the two pages' feature sets are closer).
- **Route `/LivePlay` is a placeholder name** — if the eventual live-game-state feature ends up shaped differently, the route may need renaming. Low cost: single `[Route]`/filename change, no other page links to it yet.

## Open Questions

- None blocking — deferred items (casualty interaction, real data wiring, markup convergence with `_UnitCard.cshtml`) are explicitly out of scope per Non-Goals above.
