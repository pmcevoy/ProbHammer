## ADDED Requirements

### Requirement: Weapon Profile Verbatim Keyword Text
A WeaponProfile SHALL retain the exact keyword text supplied by its source data, in source order,
independent of whether any individual keyword also corresponds to one of WeaponProfile's existing
typed ability flags. Rendering of a weapon's keywords SHALL read this verbatim record rather than
being reconstructed from the typed ability flags, so that a keyword whose meaning is not yet
understood is never silently omitted, and a keyword that is a context-dependent alternate spelling
of an already-modeled flag is never rendered as the flag's own canonical wording instead of its
actual source wording.

#### Scenario: A keyword that also maps to an existing flag is still retained verbatim
- **WHEN** a weapon's source keyword text includes `"Devastating Wounds"`, which also sets the
  existing `DevastatingWounds` flag
- **THEN** the produced WeaponProfile both has `DevastatingWounds` set AND retains
  `"Devastating Wounds"` in its verbatim keyword text

#### Scenario: A keyword with no corresponding flag is retained verbatim
- **WHEN** a weapon's source keyword text includes a token with no corresponding WeaponProfile
  flag (e.g. `"Hazardous"`, `"Precision"`, or `"Heavy"`)
- **THEN** the produced WeaponProfile retains that token in its verbatim keyword text, without
  asserting any existing typed ability flag on its behalf

#### Scenario: A context-dependent alternate spelling of an existing flag is retained verbatim, unaltered
- **WHEN** a melee weapon's source keyword text includes `"Cleave"` (the melee-context equivalent
  of `Blast`)
- **THEN** the produced WeaponProfile retains `"Cleave"` in its verbatim keyword text exactly as
  written, and rendering of that weapon's keywords shows `"Cleave"`, never `"Blast"`, regardless
  of whether the `Blast` flag is ever set on this weapon's behalf
