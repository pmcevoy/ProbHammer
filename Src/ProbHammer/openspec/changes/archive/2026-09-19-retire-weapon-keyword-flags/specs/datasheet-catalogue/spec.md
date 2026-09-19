## MODIFIED Requirements

### Requirement: Weapon Profile Verbatim Keyword Text
A WeaponProfile SHALL retain the exact keyword text supplied by its source data, in source order,
as its sole representation of the weapon's ability keywords — WeaponProfile SHALL NOT carry any
separate typed ability flag derived from this text. Rendering of a weapon's keywords, and any
structural comparison between two weapons' keyword sets, SHALL read this verbatim record directly,
so that a keyword whose meaning is not yet understood is never silently omitted, and no keyword is
ever rendered or compared under some other canonical wording instead of its actual source wording.

#### Scenario: A keyword that also maps to an existing flag is still retained verbatim
- **WHEN** a weapon's source keyword text includes a token such as `"Devastating Wounds"` that
  names a widely-understood mechanic
- **THEN** the produced WeaponProfile retains `"Devastating Wounds"` in its verbatim keyword text
  exactly as written — WeaponProfile has no separate `DevastatingWounds` flag for this or any
  other token to also set

#### Scenario: A keyword with no corresponding flag is retained verbatim
- **WHEN** a weapon's source keyword text includes a token with no widely-understood meaning
  (e.g. `"Hazardous"`, `"Precision"`, or `"Heavy"`)
- **THEN** the produced WeaponProfile retains that token in its verbatim keyword text, with no
  flag of any kind ever asserted on its behalf, by this token or any other

#### Scenario: A context-dependent alternate spelling of an existing flag is retained verbatim, unaltered
- **WHEN** a melee weapon's source keyword text includes `"Cleave"` (a mechanic unrelated to
  `"Blast"` beyond both being value-carrying keywords)
- **THEN** the produced WeaponProfile retains `"Cleave"` in its verbatim keyword text exactly as
  written, and rendering of that weapon's keywords shows `"Cleave"`, never `"Blast"` or any other
  keyword's own wording
