## ADDED Requirements

### Requirement: Popover Displays A Title Bar Naming Its Subject
Every rule/ability popover panel SHALL render a title bar above its body text, showing the same
name/label that was tapped to open it (an ability name, a weapon-keyword chip's text, or a nested
`[BRACKET]` reference's rendered label). The title bar SHALL use the same visual style as an
`.lp-section` disclosure summary's title (the section-title style used for the Statline/Ranged
Weapons/Melee Weapons headers): a filled bar spanning the popover's width, positioned flush against
its top edge.

#### Scenario: An ability popover's title bar names the ability
- **WHEN** a player taps an ability name and its popover opens
- **THEN** the popover's title bar shows that same ability's name, above the popover's body text

#### Scenario: A weapon-keyword chip's popover title bar names the keyword
- **WHEN** a player taps a resolvable weapon-keyword chip and its popover opens
- **THEN** the popover's title bar shows that same chip's text, above the popover's body text

#### Scenario: A nested reference popover's title bar names the reference
- **WHEN** a player taps a resolved `[BRACKET]` reference inside an open popover, opening a nested
  popover
- **THEN** the nested popover's title bar shows that reference's own rendered label, above its body
  text

#### Scenario: The title bar shares the section-header visual style
- **WHEN** any rule/ability popover is open
- **THEN** its title bar renders in the same visual style (background, text color, weight, size,
  case, letter-spacing) as an `.lp-section` disclosure summary's title
