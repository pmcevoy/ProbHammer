## ADDED Requirements

### Requirement: Enhancement Ability Visual Indicator
Wherever an ability's name renders on the page, an ability whose Origin is Enhancement SHALL
render with a leading `✦` symbol immediately before its name (separated by one space), and an
ability whose Origin is not Enhancement SHALL render its name unprefixed. This applies uniformly
to every place an ability name is displayed, including a rule-text popover's title (see
`restyle-rule-popover-titlebar`), since that title reuses the same rendered name as the trigger
that opened it.

#### Scenario: An Enhancement-classified ability renders with the indicator
- **WHEN** an ability entry with Origin Enhancement renders in the Model or Unit Abilities column
- **THEN** its rendered name is prefixed with `✦ ` (the symbol, then a space, then the name)

#### Scenario: A non-Enhancement ability renders with no indicator
- **WHEN** an ability entry whose Origin is not Enhancement (Intrinsic or Optional Grant) renders
  in the Model or Unit Abilities column
- **THEN** its rendered name has no leading symbol

#### Scenario: The indicator carries through to the ability's popover title
- **WHEN** a player taps an Enhancement-classified ability's name to open its rule-text popover
- **THEN** the popover's title bar shows the same `✦`-prefixed name as the trigger that opened it
