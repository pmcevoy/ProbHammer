# Tasks

## 1. Resolve BS/WS effects in the domain

- [x] 1.1 Extend `WeaponCharacteristicEffectResolver.GetField`/`SetField` with `"BS"` (ranged only, via `RangedWeapon.Bs`) and `"WS"` (melee only, via `MeleeWeapon.Ws`); a skill against the other weapon type throws. Verify: new `WeaponCharacteristicEffectResolverTests` for Improve (4→3), Worsen (3→4), clamp at 2, Set 3, and the mismatched-type rejection.
- [x] 1.2 Add an effect-aware weapon match in `AttachedUnitAggregator` (selector match plus BS→ranged/WS→melee) and use it in `MatchedWeaponEffects` and the ability-relevance check at `:86`. Verify: a test where `AllWeapons` BS and WS effects reach only their own weapon type, and a conditional BS effect records no not-applied effect on a melee weapon.
- [x] 1.3 Widen the characteristic filters in `ResolveContributionProfile` (`:406`) and `FindNotAppliedEffects` (`:449`) to include `"BS"`/`"WS"`. A not-applied skill effect carries its resolved threshold delta. Verify: aggregator tests for an unconditional WS mutation (Knight Diabolus shape), a Set BS 3 with original preserved, and a Skill mutation splitting otherwise-identical weapons into separate entries.
- [x] 1.4 Update `.claude/domain-model/statline-flag-rules.md` (or whichever topic file documents weapon-effect application) for BS/WS through `Skill` and the type rule. Verify: the doc names both, and the exclusion text in `CharacteristicModificationKind`/resolver doc comments no longer says S/AP/D only.

## 2. Render the Skill cell as a provenance highlight

- [x] 2.1 Add a `"Skill"` field to `LivePlayModel.WeaponScalarFieldOrder`/`GetWeaponScalarField` (returning `profile.Skill`). Map not-applied `"BS"`/`"WS"` effects onto it in `ValueProvenanceBuilder.ForWeapon`, with a `+` suffix, a title of `BS · <name>` or `WS · <name>` by weapon type, and a pending line that shows the resulting threshold (as `PendingScalarChange` does for RollThreshold) rather than a signed delta. Verify: `LivePlayModelTests` cases for an applied and a not-applied skill provenance.
- [x] 2.2 Replace the plain `<td>@(weapon.Skill.Value)+</td>` in both weapon tables of `_UnitBlock.cshtml` with `RenderWeaponValueCell(row, "Skill")`, keeping the `+` suffix. Verify: a Razor render test where the WS cell is a `.provenance-tile` popover trigger for an applied effect, `provenance-cond` for a not-added one, and plain when untouched.
- [x] 2.3 Update `.claude/design-tokens.md`'s Value Provenance Highlight section to list BS/WS among the weapon values. Verify: the doc text.

## 3. Pipeline: version label and v3 prompt

- [x] 3.1 Split the classifier's prompt directory from its version label: `submit` takes optional `--prompt <dir>` and `--label <label>` (label defaults to the prompt directory, which defaults to the checked-in `v2`). The label drives `IncrementalSelector` and is stamped on the pending batch, so `collect` records it. Verify: `submit --prompt v3 --label v3-draft <hashes>` prints the label, and `status`/`collect` carry it through. A unit test that `IncrementalSelector` treats a `v3-draft` record as stale under `v3`.
- [x] 3.2 Create `prompts/v3/` as a copy of v2. Widen the `WeaponCharacteristic` wording to `S`/`A`/`AP`/`D`/`BS`/`WS`: use the skill the text names, one effect per named skill, never widen one skill to the other, a fixed `N+` is `set`, and "ignore modifiers to BS/WS" is not a skill change. Update `schema.json` only if `schema` regenerates a difference. Verify: `Classifier schema` and a diff of v2→v3 showing only these edits.
- [x] 3.3 Add a Doctrina Imperatives few-shot example (choice branches carrying BS in Protector and WS in Conqueror) to `fewshot/examples.json`. Verify: `Classifier check-fewshot` passes.
- [x] 3.4 Update `.claude/domain-model/ability-classification-catalogue.md` (pipeline section) for the `--prompt`/`--label` options and the v3 draft status. Verify: the doc text.

## 4. Hand-fixed test records

- [x] 4.1 In `tools/AbilityPipeline/data/classifications.json`, add BS/WS effects (with a `ReviewerNote` saying so) to Knight Diabolus (WS +1 melee), Spotter (BS set 3, ranged), Doctrina Imperatives (BS +1 ranged on option 0, WS +1 melee on option 1), Psychic Guidance (BS +1 and WS +1, `AllWeapons`, conditional), and Assisted Targeting (BS +1 ranged, conditional). Remove the BS/WS sentence from each residue. Run `Classifier export`. Verify: the five records in `src/ProbHammer.Web/Data/ability-classifications.json` carry the effects, and the full test suite passes.
- [x] 4.2 Run the app against a roster carrying at least Knight Diabolus and a Doctrina Imperatives unit (`/run`, chrome-devtools at 667×315). Verify by DOM inspection: the WS tile is amber with a Knight Diabolus popover line; Doctrina shows BS/WS in the conditional colour, and selecting Protector turns BS amber and leaves WS plain.

## 5. v3-draft test submit

- [x] 5.1 Submit the five hand-fixed hashes plus Accelerator Mandible and one "ignore modifiers" record (e.g. Inescapable Accuracy) with `--prompt v3 --label v3-draft`. Back up `data/classifications.json` first: `collect` overwrites the hand-fixed records. Once the spot-check is done, restore the backup so the hand fixes remain what `export` writes. Verify: the user spot-checks each result against the hand fix in the usual format (one first, then the rest), Accelerator Mandible yields WS only, and Inescapable Accuracy yields no BS effect. Record any prompt tweaks made in `prompts/v3/`.
- [x] 5.2 Remove the WS/BS entries this change covers from `.claude/vnext-ideas.md` (the deferred-coverage bullet and the prompt v3 "BS/WS" bullet). Leave the Servitor-subset selector, "select one unit" targeting, and the "ignore modifiers" texts as their own entries. Verify: the file diff.
