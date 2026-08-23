## Context

`Unit.Enhancements` (`IReadOnlyList<Ability>`) is populated by `ArmyRosterEnricher` today but read
by no rendering code. `AttachedUnitAggregator.BuildAbilities` builds `AggregateAbilityEntry` rows
from two existing sources per present component: `component.Datasheet.Abilities` (component-wide,
`StatlineName: null`) and each present `ModelLine`'s own `Abilities` (`StatlineName:` that line's
name). `_UnitBlock.cshtml` renders an ability name at four call sites (per-run Model/Unit columns,
and per-component-span Model/Unit columns), each via `BuildRulePopover(WebUtility.HtmlEncode(ability.Name), ...)`.

`Ability.Origin` (`AbilityOrigin.Intrinsic | Enhancement | OptionalGrant`) already exists on every
`Ability`, including ones resolved into `Unit.Enhancements` — no new domain field is needed to
detect "this is an Enhancement" at render time.

See proposal.md for motivation and scope.

## Goals / Non-Goals

**Goals:**
- Make an applied Enhancement visible on `/LivePlay`, using the exact same component-wide ability
  reporting shape the Datasheet-sourced case already established.
- Distinguish an Enhancement from a plain ability visually, using `Ability.Origin` alone (no new
  data).

**Non-Goals:**
- Determining a genuine per-Enhancement Model/Unit scope from ability text — deferred to the
  future ability-text classification pass (`.claude/vnext-ideas.md`).
- Any change to how OptionalGrant abilities (e.g. Impulsor's "Shield Dome") are reported or
  displayed — they are already reported via `ModelLine.Abilities` (not `Unit.Enhancements`) and
  keep their existing unprefixed rendering.

## Decisions

**Enhancement entries join `BuildAbilities` as a third, component-wide source, alongside
Datasheet.Abilities.** Both are `StatlineName: null` and both require the same `component.IsPresent`
guard (a Datasheet ability has no per-statline gate to fall through when every model-line dies;
neither does a Unit-wide Enhancement). This means an Enhancement automatically participates in the
existing `ComponentAbilitySpans` grouping in `LivePlayModel` with zero changes to that file — it
already spans every rendered statline row of its owning component and collapses/expands with the
same fully-dead/fully-deselected rule as any other component-wide ability. Considered giving
Enhancements their own `AggregateAbilityEntry`-like shape or a dedicated span list; rejected as an
unjustified parallel structure — an Enhancement's Origin is enough for a renderer to treat it
differently without needing its own routing path through `LivePlayModel`.

**The `✦` indicator is applied once, in one helper, and reused for both the ability-column name
and the popover title.** `_UnitBlock.cshtml` gains a small `AbilityDisplayName(Ability)` function
(`Origin == Enhancement ? "✦ {Name}" : Name`) called at all four existing ability-name render call
sites in place of the bare `ability.Name`. Since `BuildRulePopover`'s `triggerHtml` parameter is
already reused verbatim for both the trigger button's label and (per
`restyle-rule-popover-titlebar`) the popover's title bar, prefixing the display name once at the
call site is sufficient — no separate plumbing needed for the popover case.

**Column placement is unaffected.** An Enhancement's `Ability.Scope` is already `Unit` (BSData
gives no per-ability scope signal, so `BsdataDatasheetMapper` sets `Scope: AbilityScope.Unit`
unconditionally for every ability it maps, Enhancement or not) — so it renders in the existing Unit
Abilities column through the existing Scope-based column split, with no new column-placement logic
required. This is the "defaults to Unit scope" behavior the proposal describes; it falls out of
the mapper's pre-existing behavior rather than anything new this change adds.

## Risks / Trade-offs

[An Enhancement whose real-world text is Model-scoped renders in the Unit column, which is
factually wrong for that specific Enhancement] → Accepted for now, per direct user instruction;
tracked in `.claude/vnext-ideas.md` as an output the future ability-text classification pass
should produce. Not blocking this change, since today's alternative is showing nothing at all.
