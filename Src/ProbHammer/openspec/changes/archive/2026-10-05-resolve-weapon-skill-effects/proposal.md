# Proposal

## Why

About 25 real abilities change a weapon's Ballistic Skill or Weapon Skill (Doctrina Imperatives,
Combat Drugs, Knight Diabolus, Spotter, Assisted Targeting, Holy Quest...). This matters a lot at the
table, but `/LivePlay` can't show any of them. Prompt v2 only lets `WeaponCharacteristic` name
S/A/AP/D, so every one of these lands in `unclassifiedResidue`, and the resolver throws for anything
other than S/AP/D. The app's half can ship now. The data arrives with the planned v3 classification
run after the Space Marine codex BSData refresh.

## What Changes

- `WeaponCharacteristicEffectResolver` resolves `"BS"` and `"WS"` against the weapon's single
  `Skill`: `"BS"` applies only to a ranged weapon, `"WS"` only to a melee weapon, and a weapon of
  the other type is skipped (not an error). Improve/Worsen/Set use the existing RollThreshold sign
  rule and the 2-6 clamp.
- `AttachedUnitAggregator` applies BS/WS effects wherever it already applies S/AP/D, including
  condition activation and choice branches. Each weapon's contribution profile therefore carries
  its modified Skill.
- `/LivePlay`'s weapon-table BS/WS cell becomes a value provenance highlight like S/AP/D: amber when
  changed, the conditional colour when only not-added effects reach it.
- A new prompt `prompts/v3/` (copied from v2) allows `BS`/`WS` in `WeaponCharacteristic`, using the
  text's own name, plus a Doctrina Imperatives few-shot example. v3 stays a draft. The other v3
  items in `vnext-ideas.md` are a later session's work.
- The classifier's prompt version becomes overridable, so a test submit can run under a
  `v3-draft` label. The later full v3 run then re-classifies those hashes instead of skipping them
  as current.
- Five hand-corrected catalogue records for testing (Knight Diabolus, Spotter, Doctrina Imperatives,
  Psychic Guidance, Assisted Targeting), each marked with a `ReviewerNote`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `weapon-characteristic-effect-resolution`: resolution covers Ballistic Skill and Weapon Skill
  through the weapon's Skill, matched to weapon type.
- `attached-unit-tracker`: the aggregator applies BS/WS weapon-characteristic effects alongside
  S/AP/D.
- `live-play-view`: Value Provenance Highlight covers the weapon-table BS/WS value.
- `ability-classification-pipeline`: a classification run can use a prompt-version label other than
  the checked-in default, for draft test runs.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/WeaponCharacteristicEffectResolver.cs`, and the aggregator's
  characteristic filter in `Domain/Roster/AttachedUnitAggregator.cs`.
- `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` (`GetWeaponScalarField`), `Pages/Shared/_UnitBlock.cshtml`
  (Skill cell), and `ValueProvenance.cs` if the Skill field needs a label.
- `tools/AbilityPipeline/prompts/v3/`, `fewshot/examples.json`, `Classifier/Program.cs` (version
  override).
- `src/ProbHammer.Web/Data/ability-classifications.json`: five re-exported records.
- `.claude/vnext-ideas.md`: drop the WS/BS entries that this change covers.
