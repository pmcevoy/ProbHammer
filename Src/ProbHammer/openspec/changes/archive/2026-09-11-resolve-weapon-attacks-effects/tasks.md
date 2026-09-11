## 1. Domain: resolve an Attacks-characteristic effect into a per-model amount

- [x] 1.1 Add `["A"] = CharacteristicModificationKind.Plain` to `CharacteristicModificationKinds`;
  update its own doc comment, which currently states Attacks is deliberately excluded from this
  table entirely — clarify it's now included for delta resolution only, still excluded from
  `WeaponCharacteristicEffectResolver`'s own profile-mutation path.
- [x] 1.2 Add a small resolution helper (e.g. a static method on `CharacteristicModificationResolver`
  or a new sibling) that, given a `WeaponCharacteristicEffect` naming Attacks, returns its signed
  per-model amount via `ResolveDelta(CharacteristicModificationKinds.Of("A"), effect.Verb,
  effect.Amount)` — throwing for a `Set` verb (already `ResolveDelta`'s own behavior, no new code
  needed there).
- [x] 1.3 Unit tests (`CharacteristicModificationResolverTests` or a new sibling): an Improve effect
  resolves to a positive amount; a Worsen effect resolves to a negative amount; a Set effect throws.

## 2. Domain: aggregation — record Attacks contributions per contribution, fold into TotalAttacks

- [x] 2.1 Add `AttacksContribution(Ability SourceAbility, int Amount)` and
  `WeaponContribution.AttacksContributions: IReadOnlyList<AttacksContribution> = []`
  (`AttachedUnitAggregateView.cs`).
- [x] 2.2 Add a new `AttachedUnitAggregator` method, a sibling to `ResolveContributionProfile`, that
  finds every present, bearer-scoped (per Target-Scoped Application), selector-matched, non-caveated
  ability whose baseline entry has a `WeaponCharacteristicEffect` naming Attacks, and returns the
  resolved `AttacksContribution` list for one contribution — reusing `IsBearerOf`/
  `WeaponSelectorMatches`/`TryGetWeaponEffectEntry(isCaveated: false)` unchanged.
- [x] 2.3 Narrow `ResolveContributionProfile`'s existing characteristic filter to
  `.Where(e => e.Characteristic is "S" or "AP" or "D")` (Attacks is handled by task 2.2's new method
  instead, never by this one) — design.md D2.
- [x] 2.4 Widen `FindUnresolvedAbilities`'s existing characteristic filter to
  `.Where(e => e.Characteristic is "S" or "AP" or "D" or "A")` — a caveated Attacks match still
  surfaces via the existing, unchanged unresolved-ability-reference mechanism — design.md D2.
- [x] 2.5 Wire task 2.2's new method into `BuildWeapons`, setting each `WeaponContribution`'s
  `AttacksContributions`. Update the `TotalAttacks` accumulation: replace
  `profile.A.Scale(modelLine.RemainingCount)` with `(profile.A +
  attacksContributions.Sum(c => c.Amount)).Scale(modelLine.RemainingCount)`, reusing
  `DiceExpression`'s existing `+`/`Scale` operators — design.md D1. `WeaponContribution.
  PerModelAttacks` itself stays exactly as today (the base value, from the now-narrowed
  `ResolveContributionProfile` which never touches `"A"`).
- [x] 2.6 Unit tests (`AttachedUnitAggregatorTests` or `WeaponCharacteristicEffectRosterTests`'
  sibling): a matched, non-caveated Attacks effect adds a recorded contribution without changing
  `PerModelAttacks`, and the entry's `TotalAttacks` reflects base+amount for that contribution; a
  recorded Attacks contribution does not split or merge an otherwise-identical group (regression
  against the existing "Same weapon profile... combined" scenario); a matched effect naming both
  Attacks and a structural characteristic (e.g. Chance for Glory's real S+A+AP+D four-way list,
  hand-built as an uncaveated fixture since the real entry is caveated) mutates the structural
  characteristic and records the Attacks contribution independently; a hypothetical unresolvable
  characteristic (e.g. a fixture naming `"WS"`) leaves that characteristic unapplied without
  throwing, while a co-occurring Attacks/structural characteristic still resolves.

## 3. View layer: three-tier ability-contribution rendering

- [x] 3.1 Widen `BuildContributionBreakdown`'s uniformity check (`c.PerModelAttacks ==
  group[0].PerModelAttacks`) to also require matching `AttacksContributions` (by source-ability
  identity and amount) before collapsing a group to one row — design.md D3.
- [x] 3.2 Add the group-wide-vs-row-bound/partial tiering logic: for each distinct `(Ability,
  Amount)` pair appearing across an `AggregateWeaponEntry`'s `Contributions`, determine whether it
  reaches every current contribution (group-wide) or only some (row-bound/partial) — design.md D3.
  Expose the result to the render layer (e.g. a list of group-wide ability-contribution lines on
  `WeaponRowViewModel`, and a per-`WeaponContributionRow` list of the row-bound/partial ones that
  reach it).
- [x] 3.3 Update `WeaponRowViewModel.ShowsBreakdownTrigger` to also fire when any contribution
  carries a non-empty `AttacksContributions` — design.md D4.
- [x] 3.4 Unit tests (`LivePlayModelTests`): a row-bound Attacks contribution nests under its own
  contributor row with the correct `(Count×Amount)`; a group-wide contribution (reaching every
  current contributor identically) renders once, not repeated per row; a partial-reach contribution
  nests under only the rows it reaches; two contributions sharing base `PerModelAttacks` but
  differing in `AttacksContributions` render as separate breakdown rows; `ShowsBreakdownTrigger`
  fires for a single-`ModelLine` unit whose only weapon entry carries an `AttacksContributions` entry
  and nothing else.

## 4. Template: render the ability-contribution lines

- [x] 4.1 In `_UnitBlock.cshtml`'s weapon contribution-breakdown partial: render each row-bound/
  partial ability-contribution line nested directly beneath its contributor row, showing the source
  ability name (as a popover trigger, reusing `BuildRulePopover`) and its `(Count×Amount)`.
- [x] 4.2 Render a group-wide ability-contribution line once, above the breakdown, using the same
  trigger/styling convention.
- [x] 4.3 Confirm no marker/legend rendering is added for Attacks anywhere in the collapsed
  (unexpanded) entry row — the total stays a plain number, per design.md D4.
- [x] 4.4 Manual verification against a running `docker compose up` instance (`firefox-devtools`
  MCP): import a real captured export from a faction carrying Scorpion Tail or Writhing Tentacles
  (Chaos Space Marines / Death Guard — real, uncaveated corpus evidence per proposal.md), confirm the
  affected weapon entry's breakdown shows the base contributor row plus the nested ability-
  contribution line with the correct amount, and that the entry's collapsed total already includes
  it. If no bundled export exercises this datasheet, verify deterministically through the real
  BSData pipeline instead (mirroring `resolve-weapon-characteristic-effects` task 5.1's precedent),
  and note which path was used.

## 5. Full-suite verification

- [x] 5.1 Run the full test suite (`dotnet test` or the Rider MCP equivalent) and confirm no
  unrelated regression.
