# Deliberate Omissions

- No wargear constraint/composition-rule validation, no points-cost modeling — the exporting app
  is trusted to have already produced a valid list.
- No partial wound-count tracking — alive/dead per model-line only; physical wound markers cover
  the rest at the table.
- Ability text classification (`AbilityClassificationCatalogue`, `classify-abilities-via-llm-batch`)
  resolves only to *display* state — recomputing a rendered stat/keyword-chip — never to executing
  a game mechanic. What stays a permanent exclusion, not a deferral: a condition needing
  positional/objective-control state the domain has no way to represent even in principle (the
  `never` residual-condition bucket), and actual mechanic execution (e.g. rolling Sustained Hits'
  extra hit dice), which belongs entirely to the paused `Simulation/*` subsystem below.
- `Simulation/*` (CombatSimulator, SimulationAdapter, WoundPool) is untouched but paused — not
  wired to this domain model. The one exception is `DiceExpression`, which moved from
  `Simulation/*` into `Domain.Catalogue` (see catalogue-context.md); `Simulation/*` files were
  repointed to the relocated type via `using` statement only — no behavior, method signature, or
  test change in `Simulation/*` itself.
