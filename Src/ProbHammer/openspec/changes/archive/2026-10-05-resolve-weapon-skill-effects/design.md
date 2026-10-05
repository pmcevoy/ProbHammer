# Design

## Context

`WeaponProfile.Skill` is abstract, and each subtype computes it from its own field:
`RangedWeapon.Skill => Bs`, `MeleeWeapon.Skill => Ws`. Both are already `ScalarCharacteristicView`.
`CharacteristicModificationKinds` maps WS/BS to RollThreshold, and `CharacteristicModificationClamp`
bounds them to 2-6, so sign handling and clamping exist. Two pieces are missing: the resolver's
`GetField`/`SetField` (S/AP/D only, throws otherwise) and the prompt, whose
`WeaponCharacteristic` wording lists only `S`/`A`/`AP`/`D` (`schema.json` doesn't constrain the
string).

A corpus check of residues mentioning BS/WS found about 25 own-side changes. A further ~12 are
"ignore modifiers to BS/WS and the Hit roll" texts, which are not BS/WS changes. Two are enemy
debuffs.

## Goals / Non-Goals

**Goals:**
- A classified BS/WS effect resolves, renders and reaches the contribution profile exactly the way an
  S/AP/D effect does today.
- The v3 prompt classifies BS/WS without help, checked by a small `v3-draft` test submit.

**Non-Goals:**
- "Ignore modifiers to BS/WS and/or the Hit roll" texts. These belong with the roll-modifier effect
  kind in the prompt v3 backlog.
- A weapon selector for a model-keyword subset ("weapons of SERVITOR models in this unit", Mindlock
  ×5) and "select one unit" targeting (Accelerator Mandible, Orders, Righteous Purpose). Both stay
  in `vnext-ideas.md`.
- Enemy-weapon debuffs (Time Sink, Data-spike): a permanent boundary until a two-roster model
  exists.
- The rest of the v3 prompt backlog, and the full v3 run.

## Decisions

**The data names `"BS"`/`"WS"`, and the domain resolves both through `Skill`.**
"Improve the BS and WS of weapons equipped by this model" means "each weapon's skill". The
classifier emits two `AllWeapons` effects, `"BS"` and `"WS"`. The resolver treats `"BS"` as "this
weapon's Skill if it is ranged" and `"WS"` as "if it is melee". A weapon of the other type doesn't
match, so it isn't mutated, isn't listed in that weapon's provenance popover, and doesn't throw.
Alternative considered: a single `"Skill"` characteristic. It was rejected because the model would
have to infer the weapon type when a text names only one skill. Accelerator Mandible says "the
Weapon Skill characteristic of weapons" with no melee qualifier, so `Skill` + `AllWeapons` would
also improve that unit's guns. Literal extraction matches the prompt's existing rule ("the text's
own verb, never pre-computed") and keeps the inference in tested code.

**The type check lives at selection, not inside the resolver.** The aggregator already decides per
weapon whether an effect applies (selector match). The BS/WS type match joins that same decision, so
the "no row, no popover line" behaviour comes from the existing path. `Resolve` on a mismatched
weapon still throws, which keeps a caller bug visible. The same match also excludes a weapon whose
Skill is 0 (BSData "N/A", e.g. Torrent weapons). Otherwise an Improve would clamp it up to 2+ and
invent a skill for a weapon that auto-hits.

**Version override for draft test runs.** `IncrementalSelector` treats a record as current when its
`PromptVersion` matches the run's version. A test submit stamped `"v3"` would be skipped by the
later full v3 run even if v3 has changed since. The run takes an optional version label
(default stays the checked-in constant). The prompt directory is still selected separately, so
`v3-draft` reads `prompts/v3/`. Records collected under `v3-draft` are stale to the full run and
are re-classified automatically. Alternative considered: test under `"v3"` and delete those
records before the full run. Rejected because it relies on remembering to do so.

**Hand-fixed records: one per shape the app must handle.** Knight Diabolus (unconditional WS,
`WeaponClass(Melee)`), Spotter (`Set` BS 3+), Doctrina Imperatives (choice branches: BS in
Protector, WS in Conqueror), Psychic Guidance (BS + WS on `AllWeapons`, conditional), Assisted
Targeting (conditional aura with an existing [HEAVY] grant). Each is edited in the pipeline's
classification store with a `ReviewerNote`, then exported, following the 2026-10-04 event-trigger
precedent.

## Risks / Trade-offs

- [The full v3 run overwrites the hand fixes] → Intended. The v3 prompt and few-shot must produce
  them unaided, and the `v3-draft` test submit checks this on the same five hashes before the full
  run.
- [Collecting the `v3-draft` test overwrites the hand-fixed records in the pipeline store] → Back up
  the store before `collect` and restore it after the spot-check. The test results are for review
  only, and the label still protects any that are kept.
- [A `Set` BS (Spotter "3+") resolves worse than the weapon's own BS] → The rulebook says a Set
  replaces the value. The provenance popover shows Datasheet → Shown, so the player can see it.
  Don't add a "keep the better" rule here; that is the characteristic-modification engine idea.
- [The weapon table merges rows whose Skill now differs] → Weapon `EqualityKey` already includes
  Skill, so a modified and an unmodified copy of the same weapon stay separate rows.
