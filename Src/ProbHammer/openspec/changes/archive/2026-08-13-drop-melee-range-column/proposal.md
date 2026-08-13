## Why

Melee weapons always show "Melee" in the Range column on `/LivePlay` — a value that's true by
definition of the weapon's Type and carries no information the player doesn't already have from
the section header ("Melee Weapons"). Dropping it for melee rows only saves a column of pure
noise on a page meant to be read at a glance on a phone; Ranged weapons keep the Range column
since range there is a real, varying number.

## What Changes

- The Melee Weapons table on `/LivePlay` no longer has a Range column. The Ranged Weapons table is
  unaffected.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `live-play-view`: "Weapon Section Rendering" no longer requires a Range value for Melee weapon
  rows (it's always the fixed literal "Melee" for that weapon type); Ranged rows still show Range.

## Impact

- `src/ProbHammer.Web/Pages/LivePlay.cshtml` — Melee weapon table drops its Rng `<th>`/`<td>`.
- No domain or view-model changes — `WeaponProfile.Range` is still 0 for melee weapons underneath;
  only the rendered column is removed.
