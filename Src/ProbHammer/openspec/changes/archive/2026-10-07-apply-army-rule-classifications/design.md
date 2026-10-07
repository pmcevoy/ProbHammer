# Design

## Context

`AttachedUnitAggregator` gates every classified effect through two checks, shared by the Statline
path, the weapon path and `BuildActivatableConditions`:

- `TryGetApplicableClassification` admits a record only when its target is `Self` or
  `AttachedUnit`, or the ability's Origin is `DetachmentRule`.
- `IsBearerOf` then picks rows. `AttachedUnit` targets and `DetachmentRule` origin get every row;
  otherwise the entry's own `(ComponentName, StatlineName)` must match.

`PromoteArmyRuleAbilities` always reports an ArmyRule ability as one entry with
`ComponentName: null`. So a keyword- or unconditionally-targeted ArmyRule record is rejected by the
first check, and a `Self`-targeted one passes it but matches no row in the second. Current ArmyRule
records with effects that gain from this (`ability-classifications.json`):

| Rule | Target | Effects (all bucket `never`) |
|---|---|---|
| Waaagh! | Keyword `Orks` | InvulnerableSave 5+/5+; WeaponKeywordGrant Ranged `Assault` |
| Synapse | Keyword `Tyranids` | WeaponCharacteristic Melee S +1 |
| Templar Vows | Keyword `Adeptus Astartes` | WeaponKeywordGrant Melee `Precision` (choice branch 0/0), plus a 4-option choice group |
| Harbingers of Dread | Self | NamedAbilityGrant (no consumer yet) |

Every other Keyword/Unconditional ArmyRule record has no effects.

## Goals / Non-Goals

**Goals:**
- Mirror the Detachment Rule exception exactly, in the same two places.

**Non-Goals:**
- Evaluating the keyword predicate itself (e.g. confirming a unit is `ORKS`). Presence of the Army
  Rule on the unit is the evidence, as it already is for display.
- Riled Up as a unit state; that is the separate vnext item this unblocks.

## Decisions

**Treat ArmyRule origin as WholeUnit-scoped, like DetachmentRule.** Extend both checks'
`DetachmentRule` condition to `DetachmentRule or ArmyRule` (one shared predicate, used by both).
The rules' own wording is about "that unit", and an attached unit is one unit. The alternative,
limiting rows to the promoted entry's `ContributingComponentNames`, was considered and rejected:
it would also narrow the already-working attached-unit-targeted Army Rules (Dark Pacts, Doctrina
Imperatives, Martial Ka'tah), and it diverges from the existing exception for no case seen in
real data.

**Fix the corrupted records at the source.** The v2 batch output has a JSON `\r` escape plus junk
(`actes1`, `actal`, `actor`) where an apostrophe belongs, in 3 of 3,828 records: Templar Vows (two
choice options and its residue), Mindlock and Radiant Mantle (residue only). The corpus input is
clean and the strings are the model's own paraphrases, so this is a rare model glitch, not a
decoding bug in `collect`. Restore each apostrophe in `tools/AbilityPipeline/data/classifications.json`
with a `ReviewerNote`, as earlier hand corrections were done, then run the classifier's `export`
to rewrite `src/ProbHammer.Web/Data/ability-classifications.json`. Patching only the exported file
was rejected: the next export would put the corruption back. A permanent guard test was
considered and deferred to the prompt v3 resubmit, which regenerates every record anyway.

## Risks / Trade-offs

- [Templar Vows adds a conditional `[PRECISION]` chip to every Black Templars melee weapon until a
  vow is chosen] → Intended: it's the same not-added treatment every conditional grant already
  gets, and choosing a vow in the popover is now possible.
- [The prompt v3 resubmit produces the same corruption] → A reminder in `vnext-ideas.md`'s prompt v3
  entry to scan the new output for control characters before exporting.
- [An Army Rule attached to a unit that doesn't actually satisfy its keyword] → Same exposure as
  today's display of that rule; not introduced here.
