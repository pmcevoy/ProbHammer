## MODIFIED Requirements

### Requirement: Statline Section Rendering
Each unit block SHALL render one row per statline entry in the view's `Statlines`, in the order returned, showing the statline's name, M/T/Sv/W/Ld/Oc values (and InSv when present), and its remaining/initial model count. Each row SHALL render a nested loadout breakdown listing every entry in that statline's `Loadouts`, showing each loadout's weapon list and its own remaining/initial count.

#### Scenario: Squad with a distinct sergeant statline sharing the same values
- **WHEN** a unit's aggregate view contains two statline entries with identical M/T/Sv/W/Ld/Oc values but different names (e.g. "Assault Intercessor" and "Assault Intercessor Sergeant")
- **THEN** the rendered block shows two separate statline rows, one per name, neither merged with the other — this reverses the by-value collapsing `render-aggregate-view` introduced, which is no longer compatible with per-name current/initial count tracking

#### Scenario: Statlines with differing values stay on separate rows
- **WHEN** a unit's aggregate view contains two statline entries whose M/T/Sv/W/Ld/Oc values differ from each other
- **THEN** the rendered block shows two separate statline rows, each under its own name

#### Scenario: Loadout breakdown lists every contributing model-line
- **WHEN** a statline entry's `Loadouts` contains two entries with different weapon lists and counts
- **THEN** the rendered row's nested breakdown shows both, each with its own weapon list and remaining/initial count

#### Scenario: Invulnerable save renders when present
- **WHEN** a statline's `InSv` value is set
- **THEN** the rendered row displays that invulnerable save value

#### Scenario: Invulnerable save is omitted when absent
- **WHEN** a statline's `InSv` value is unset (the default)
- **THEN** the rendered row does not display an invulnerable save value

### Requirement: Weapon Section Rendering
Each unit block SHALL list every entry in the view's `Weapons`, showing the weapon's name, type (Ranged/Melee), Range, Skill, Strength, AP, Damage, ability tags, and the aggregated total Attacks value. The row SHALL NOT display a raw per-model Attacks value alongside a separate model count.

#### Scenario: Weapon count reflects aggregation across components
- **WHEN** a unit's aggregate view has a weapon entry whose total Attacks was built from contributions with different per-model Attacks values
- **THEN** the rendered row shows only that entry's aggregated total Attacks, and does not show any individual contribution's per-model Attacks value or a separate model count
