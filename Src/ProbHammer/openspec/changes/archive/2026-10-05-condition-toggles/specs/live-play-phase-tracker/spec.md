## MODIFIED Requirements

### Requirement: No Rules Interpretation
The phase/turn selection SHALL only reflect what the player asserts and SHALL only affect which
existing sections default open or closed. It SHALL NOT gate, trigger, compute, or auto-apply any
change to a unit's statline, weapon, ability, casualty, half-strength, Battle-shocked, or condition
activation state.

#### Scenario: Selecting a phase/turn cell changes no game data
- **WHEN** a player selects any phase/turn cell
- **THEN** no unit's Statline values, weapon totals, abilities, casualty counts, half-strength
  status, or Battle-shocked status change as a result — only section disclosure state changes

#### Scenario: Re-rendered unit blocks keep recorded state after a phase change
- **WHEN** a player has marked casualties, set a Battle-shocked status and activated a condition,
  and then selects a phase/turn cell
- **THEN** every re-rendered unit block still shows those casualties, that status and that
  activation
