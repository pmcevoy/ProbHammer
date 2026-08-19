# vNext Ideas — Deferred Features

Ideas and follow-on work that came up during 11e development and were deliberately deferred
rather than built. Nothing here is scheduled — this is a parking lot to draw from when picking
the next `openspec` change. Update this file whenever a new idea gets deferred, and remove an
entry once it's been turned into a change (archived changes remain the historical record).

---

## `/LivePlay` — Attached Unit view

- **Ability/weapon-keyword definitions popup.** A genuine centre-screen popup (not an inline
  `<details>` disclosure) triggered by clicking an ability name or a weapon keyword tag (e.g.
  `[SUSTAINED HITS 1]`), showing its full descriptive text. Distinct in kind from the
  weapon-contribution breakdown below — that one stays in-place in the table (native disclosure,
  tabular data); this one needs a real floating/anchored overlay (prose content, works for
  keywords that appear inline mid-row, doesn't want to push table rows around). Corresponds to
  `web-app.md`'s "Ability rendering is name-only for now; full-text-on-demand is explicitly
  deferred" note (currently `/LivePlay` renders `ability.Name` only, no way to see `ability.Text`
  at all).
- **Phase-level section toggling across all units at once** — collapse/expand every unit's
  statline/ranged/melee/abilities section together, reusing the same disclosure unit each
  section already uses individually.
- **Core/Faction/Psychic ability source tagging** — distinguish where an ability comes from,
  not just its name/scope.
- **Per-`ModelLine` keywords.** Needed so a leader's personal keyword leaves the unit's effective
  keyword union when that specific model is removed as a casualty — currently keywords are only
  tracked at the component (`Unit.Datasheet.Keywords`) level.
- **Multi-profile-weapon "select one profile" disclaimer** — some weapons have multiple firing
  profiles the player picks between; the view doesn't currently flag this.
- **Ability-driven attack modifiers** — e.g. a unit-wide "+1 Attack to melee weapons" ability
  changing a total from A28 to A35. Not modeled at all yet. Expected to be a consumer of the
  "Ability-text interpretation pass" idea below, once that exists, rather than its own bespoke
  parsing.
- **Splitting `Keywords` into unit-wide-union vs. per-component, or adding `FactionKeywords`** —
  currently left as one unioned `IReadOnlySet<string>`.
- **Split `wwwroot/css/site.css` into a `/LivePlay`-only stylesheet.** Deferred by
  `archive-10e-pipeline`: the file's 10e-only, `/LivePlay`-only (`.live-play-page`-scoped),
  and shared base rules are interleaved with no clean physical boundary, and the archive
  change's own goal was safety (no risk to the one live page), not a redesign. `site.css`
  currently still ships several hundred lines of now-dead 10e selectors (harmless — they
  never match anything once `Index`/`ArmyView` markup is gone — but worth tidying up
  eventually). Extracting `/LivePlay`'s rules into their own file would let `_Layout.cshtml`
  stop loading dead CSS and make the stylesheet's scope match the app's actual scope
  (`/LivePlay`-only).

## Domain / data pipeline

- **Rewrite `ArmyListParser.cs` for the 11e export format** — the Warhammer App export format
  changed for 11th edition; `/LivePlay` currently runs entirely on hand-built fixture data
  (`Examples/Units.cs`, `Examples/Datasheets.cs`), not a parsed army list. No 11e parser exists.
- **Rebuild `Simulation/*` (CombatSimulator, SimulationAdapter, WoundPool) against the 11e domain
  model.** Paused, not abandoned — the old design assumes one flat Toughness per defender, with
  no notion of Attached Units or multi-statline targets, so it doesn't fit the new shape as-is.
- **Wire `Simulation`/`CombatSimulator` into `/LivePlay`** — today `/LivePlay` is a static
  per-request reference view, not a dice-rolling tool; this is separate from rebuilding the
  simulator itself.
- **Ability-text interpretation pass.** A distinct pass, run *after* `BsdataDatasheetMapper
  .BuildDatasheet` (not inside it), that classifies a specific ability's `Description` text
  against a small closed vocabulary of known fixed phrasings and folds the result back into the
  domain model as a new, immutable `Datasheet`. Explicitly **not** general ability-text-to-
  behavior parsing (that stays permanently out of scope per `domain-model-11e.md`'s Deliberate
  Omissions) — this is pattern-matching known, previously-catalogued sentences, not NLU. First
  concrete driver: the InSv-caveat representation change (mapper always marks a footnoted/
  ability-linked invulnerable save `Caveated = true` and captures the resolved ability rather
  than interpreting it inline; this pass is what would later resolve the known cases and flip
  `Caveated` back to `false`) — deliberately deferred out of that change's scope, to be designed
  as its own change once the shape needed here is clearer. Longer-term motivation is the
  "Ability-driven attack modifiers" idea above (e.g. folding a "+1 Attack to melee weapons"
  ability into weapon-aggregation totals) — same class of problem, not yet designed together.
  **Revisit once the InSv-caveat change is implemented/archived — explicitly asked to be
  reminded.**

## Bigger picture

- **A full "Gameplay" bounded context** for live/mutable game state beyond casualty counts —
  turn tracking, objective control, battle-shock, and other conditions that rules text sometimes
  references ("while this model is on the battlefield...", "gets Lethal Hits while within range
  of an objective marker"). Some ability text is permanently out of scope for auto-parsing
  because it needs state (positioning, objective control) the domain has no way to represent even
  with this context built — see `.claude/domain-model-11e.md`'s Deliberate Omissions.
