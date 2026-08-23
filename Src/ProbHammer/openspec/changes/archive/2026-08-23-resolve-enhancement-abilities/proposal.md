## Why

`BsdataDatasheetMapper.BuildDatasheet` walks a datasheet's entire BSData descendant tree and
materializes *every* `"Abilities"`-typeName profile it finds into the public, always-rendered
`Datasheet.Abilities` list — with no distinction between an intrinsic fact about the datasheet
(e.g. "Vengeful Exhortation") and an optional, player-selectable ability nested inside its own
`"type": "upgrade"` selection entry (an Enhancement, or an Enhancement-shaped wargear grant like
Impulsor's "Shield Dome"). `WeaponProfile` already has exactly this protection
(`ResolveWeaponProfile`/`TryResolveWeaponProfile`, on-demand only, never enumerated) — `Ability`
never got it.

Confirmed live against a real pasted export (`data/gw-app-export-templars.txt`): Black Templars'
"Crusade Ancient" incorrectly shows "Thirst for Glory" (a Space-Wolves-only Enhancement, never
selected and structurally unreachable for this roster's detachment), and "Sword Brethren Squad"
incorrectly shows both "Fervent Exemplars" and "Inheritors of Sigismund" — two real Black Templars
Enhancements, neither selected in the export, on a unit that (per the real 40K rules) isn't even a
Character eligible to receive an Enhancement at all. `Unit.Enhancements` — populated straight from
the parsed export's Enhancement name strings — is never actually cross-referenced against the
catalogue, so even a correctly-selected Enhancement (e.g. Helbrecht's "Ferocity") carries no
resolved rules text today; it's inert.

## What Changes

- `Datasheet.Abilities` (the public, always-rendered list) SHALL contain only abilities found
  directly on the entry that owns a resolved `Statline` — genuine, unconditional facts about the
  datasheet. Every `"Abilities"`-typeName profile found nested inside a `"type": "upgrade"`
  selection entry anywhere in the walk (Enhancements and Enhancement-shaped wargear grants like
  Shield Dome) moves to a separate, on-demand-only index, mirroring `WeaponProfile`'s existing
  `ResolveWeaponProfile`/`TryResolveWeaponProfile` pattern exactly.
- `ArmyRosterEnricher`'s existing wargear-name-falls-back-to-an-ability resolution (the Shield Dome
  case) repoints onto this new on-demand index instead of scanning the (now intrinsic-only) public
  `Abilities` list — its observable behavior is unchanged.
- New capability: the system resolves each name in a parsed unit's `Enhancements` against the same
  on-demand index (same "resolve by name, fail with a did-you-mean diagnostic on a miss"
  convention already used for units/weapons), and attaches the resolved `Ability` to the `Unit` —
  replacing today's inert raw-string carry-through. **BREAKING**: `Unit.Enhancements`'s element
  type changes from a raw `string` to a resolved `Ability`.
- An ability resolved through the on-demand index is classified as an Enhancement (found within a
  selection group actually named "Enhancements") or a plain optional ability grant (Shield Dome),
  so a future `/LivePlay` rendering pass can visually distinguish an Enhancement from any other
  ability without re-deriving that distinction. This change lays the domain groundwork only —
  the `/LivePlay` visual treatment itself is out of scope here.

## Capabilities

### Modified Capabilities
- `datasheet-catalogue`: `Datasheet.Abilities` narrows to intrinsic-only abilities; a new
  on-demand ability resolution requirement is added, mirroring the existing on-demand weapon
  profile requirement; `Ability` gains a classification distinguishing an Enhancement from a
  plain optional ability grant.
- `catalogue-json-ingestion`: the ability-extraction walk changes from "every `Abilities`-typeName
  profile becomes part of the datasheet's ability list" to "only entry-direct profiles do; profiles
  nested inside a `type: upgrade` selection entry are extracted into the on-demand index instead,
  classified by whether their containing selection group is named `Enhancements`."
- `army-roster-enrichment`: a new Enhancement Name Resolution requirement resolves each parsed
  unit's Enhancement names against the on-demand ability index; the existing wargear name
  resolution requirement is tightened to document its existing (previously unspecified)
  ability-fallback behavior, since this change repoints its implementation.
- `roster-model`: `Unit`'s Enhancements field changes from a list of raw strings to a list of
  resolved `Ability` objects.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/Datasheet.cs` — new on-demand ability index/lookup API.
- `src/ProbHammer.Core/Domain/Catalogue/Ability.cs` — new classification field.
- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/BsdataDatasheetMapper.cs` — `WalkEntry`/`WalkGroup`/
  `ProcessProfile` split intrinsic vs. on-demand ability collection.
- `src/ProbHammer.Core/Domain/Roster/ArmyRosterEnricher.cs` — repointed wargear-ability fallback;
  new Enhancement name resolution step.
- `src/ProbHammer.Core/Domain/Roster/Unit.cs` — `Enhancements` element type change (**BREAKING**).
- Test fixtures/data: `data/gw-app-export-templars.txt` (already added, reproduces the bug) becomes
  the regression fixture for both the Crusade Ancient and Sword Brethren Squad cases.
