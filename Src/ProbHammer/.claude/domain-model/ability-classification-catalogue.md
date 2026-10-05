# Ability-Classification Catalogue

Full requirements: `openspec/changes/archive/2026-10-03-adopt-llm-ability-classifications/` (this consumer) and
`openspec/changes/archive/2026-10-03-classify-abilities-via-llm-batch/` (the offline pipeline that produces the data).
The runtime's only source of "what does this rule/ability text do".

```
AbilityTextKey.Normalize(text) / .Hash(text)   // Domain/Catalogue - SHA-256 hex of the normalized
                                               // text. The pipeline's extractor keys its corpus on
                                               // the same method; any drift empties every lookup.
AbilityClassificationCatalogue                 // Load(path) (missing -> Empty), FromRecords,
                                               // FromTexts (tests), TryGet(abilityText, out ...)
AbilityClassificationRecord(Hash, Text, Names, Classification)
AbilityClassification
  Target            : RuleTarget               // Self | AttachedUnit | Keyword(Keywords, all-of) |
                                               // Unconditional
  Effects           : ClassifiedEffect[]       // (Effect, ResidualConditionBucket, ConditionText?,
                                               //  ChoiceBranch?)
  ChoiceGroups, Phases : GamePhase[], TurnOwnership : GameTurn?, UsageLimit?,
  CoverageStatus, UnclassifiedResidue
  IsUnconditional(effect) / UnconditionalEffects<T>() / ConditionalEffects<T>()
RuleEffect          // Scalar | InvulnerableSave | WeaponCharacteristic - consumed;
                    // FeelNoPain | WeaponKeywordGrant | NamedAbilityGrant | NamedAbilityRemoval -
                    // data only, no consumer yet
```

**The file** is `src/ProbHammer.Web/Data/ability-classifications.json`, written by
`dotnet run --project tools/AbilityPipeline/Classifier -- export` (run `resolve` first after a
vocabulary refresh). The export substitutes each resolved name's canonical BSData spelling
(`"heavy bolters"` -> `Heavy bolter`, `[BLAST 1]` -> `BLAST 1`), reshapes the classifier's flat
invulnerable-save effect into Core's `InvulnerableSave` value, and drops review metadata. It is
written by convention, not by reference (the classifier doesn't reference Core) -
`AbilityClassificationCatalogueTests.TheCheckedInCatalogue_LoadsAndEveryKeyIsTheHashOfItsOwnText`
is the drift guard. An unknown effect/target `kind` fails the load rather than being skipped.

**Prompt versions.** `submit` reads `prompts/<--prompt>/` (default `v2`, the version the catalogue was
built with) and stamps results with `--label` (default: the prompt name). `IncrementalSelector`
re-classifies any record whose stamp differs from the run's label, so a draft test
(`submit <hashes> --prompt v3 --label v3-draft`) is redone by the final `--prompt v3` run.
`prompts/v3/` is a draft: it adds `BS`/`WS` to `WeaponCharacteristic` (`resolve-weapon-skill-effects`),
and the other v3 items in `.claude/vnext-ideas.md` are still to come. `fewshot/examples.json` is
shared by every prompt version.

**Unconditional rule** - the one applicability check every consumer shares: an effect applies on its
own only when its `ResidualConditionBucket` is `None`, it has no `ChoiceBranch`, and the record has
no `UsageLimit` and no `TurnOwnership`. `Phases` never gates (it's "when to remind the player", not a
condition). `CoverageStatus` never gates either - a `partial`/`unclassifiable` record's captured
effects are used as-is (user decision).

**Activation** (`condition-toggles`) - a consumer also applies a conditional effect the player has
activated on the unit (`ICombatUnit.ConditionActivations`). `EffectStates.Of` (`Domain/Roster`) is
the one three-way seam: `Applied` (unconditional or activated), `Suppressed` (an unselected option of
a choice group with a selection) or `NotApplied`. A non-choice effect is activated by its condition
text (`EffectStates.ConditionKey`, `""` when it has none); a choice-branch effect by its option being
selected, even when the option has its own condition text.

**Consumers** (all in `AttachedUnitAggregator` unless noted):

| Consumer | Uses |
|---|---|
| `ResolveCaveatedInvulnerableSaves` | first unconditional `InvulnerableSave` effect of the caveat's linked ability (not in `Datasheet.Abilities`, so the Extractor walks each statline's caveated InSv to get these texts into the corpus) |
| `ApplyStatlineFlagRules` | `Applied` `Scalar`/`InvulnerableSave` effects applied; `NotApplied` ones recorded as `AggregateStatlineEntry.NotAppliedEffects` - see statline-flag-rules.md |
| `ResolveContributionProfile` / `ResolveAttacksContributions` | `Applied` `WeaponCharacteristic` S/AP/D/BS/WS and A; `Applied` `WeaponKeywordGrant`s rewrite the contribution's keywords and are recorded as `KeywordGrant`s |
| `FindNotAppliedEffects` | `NotApplied` `WeaponCharacteristic` S/AP/D/BS/WS/A effects -> `NotAppliedWeaponEffect`s (characteristic, signed per-model amount, `EffectCondition`) on the contribution and its entry |
| `FindNotAppliedKeywordGrants` | `NotApplied` `WeaponKeywordGrant`s that would change the keywords -> `NotAppliedKeywordGrant`s on the contribution and its entry |
| `DetachmentRuleInboundAbilityResolver` | a Detachment rule with a `Keyword` target attaches to every unit carrying all its keywords (case-insensitive) |

The applied/not-applied split is per effect, so one ability can both mutate Strength and record a
conditional Damage boost as a not-applied effect on the same weapon. A Detachment-rule ability with
a keyword target reaches both Statline values and every component's weapons (origin exception,
shared via `TryGetApplicableClassification`).

**Not consumed yet** (each is a separate future change, see `.claude/vnext-ideas.md`): `Phases`/
`TurnOwnership` for phase highlighting, and the three remaining data-only effect kinds.

**Tests** build catalogues with `ClassificationFixtures` (`Entry(text, target, effects,
conditional)` / `Catalogue([...])`), including `RealCorpusExcerpt` - real ability texts the
real-BSData tests resolve against until the full-corpus catalogue is exported and those tests can
read the checked-in file instead.
