## MODIFIED Requirements

### Requirement: Weapon Section Rendering
Each unit block SHALL list every entry in the view's `Weapons`, showing the weapon's name, type (Ranged/Melee), Skill, Strength, AP, Damage, ability tags, and the aggregated total Attacks value. Ranged weapon rows SHALL additionally show Range; Melee weapon rows SHALL NOT show a Range value, since it is always the fixed literal "Melee" for that weapon type and carries no information beyond the section it's already listed under. The row SHALL NOT display a raw per-model Attacks value alongside a separate model count.

#### Scenario: Weapon count reflects aggregation across components
- **WHEN** a unit's aggregate view has a weapon entry whose total Attacks was built from contributions with different per-model Attacks values
- **THEN** the rendered row shows only that entry's aggregated total Attacks, and does not show any individual contribution's per-model Attacks value or a separate model count

#### Scenario: Ranged weapons show their Range value
- **WHEN** a unit block renders its Ranged Weapons section
- **THEN** each row shows that weapon's Range value

#### Scenario: Melee weapons omit the Range column
- **WHEN** a unit block renders its Melee Weapons section
- **THEN** no row shows a Range value or column, since it would always read "Melee"
