## Why

`ProbHammer.Core`'s Catalogue context (`Datasheet`, `Statline`, `WeaponProfile`, `Ability` —
see `datasheet-catalogue`) exists only as hand-authored fixtures (`Domain/Examples/Datasheets.cs`).
There is no way to populate it from real rules data. A cloned copy of the official BSData 11th
Edition repository (JSON, one file per faction/sub-faction) is available locally and has been
directly inspected against three captured GW-app exports. This change builds the loader that
turns those JSON files into `Datasheet` objects resolvable by name, so a future enrichment step
(out of scope here — see Impact) has something real to resolve parsed army-list entries against.

## What Changes

- Add a BSData 11e JSON reader that deserializes a single faction/sub-faction catalogue file
  (the BattleScribe `catalogueSchema` shape: `sharedSelectionEntries`, `entryLinks`,
  `catalogueLinks`, `categoryLinks`, `profiles`/`characteristics`, `costs`).
- Add recursive `catalogueLinks` closure resolution: starting from one file, follow every
  `importRootEntries: true` link outward (transitively — an imported catalogue may itself import
  further catalogues, confirmed for both `Imperium - Space Marines.json` and
  `Chaos - Chaos Space Marines.json` against the real data) to build the full set of files a given
  faction's roster can draw from.
- Add name-to-`Datasheet` resolution over that closure, with the file nearer the resolution's
  starting point always winning over a same-named entry only reachable through an import (matches
  the precedence directly observed between `Imperium - Black Templars.json`'s local `Crusader
  Squad` definition and the `Imperium - Space Marines.json` import it also carries).
- Add mapping from BSData JSON shapes to the existing `Domain.Catalogue` types: `Unit`-typeName
  profile characteristics → `Statline` (including `InSv`, which 11e JSON now exposes as a
  first-class characteristic rather than requiring text extraction); `Ranged Weapons`/`Melee
  Weapons`-typeName profiles → `RangedWeapon`/`MeleeWeapon`; `Abilities`-typeName profiles →
  `Ability` (Text carried through verbatim, never parsed for behavior, per `datasheet-catalogue`'s
  existing "No Wargear Constraint or Points Modeling" stance); squad-vs-single-model detection
  (statline nested on child entries vs. on the entry itself).
- Add parsing of the weapon `Keywords` free-text characteristic (e.g. `"Anti-infantry 4+,
  Devastating Wounds"`, `"Sustained Hits 1"`, `"-"`) into `WeaponProfile`'s existing ability flags,
  for tokens whose mapping is already established. `WeaponProfile` gains a verbatim, always-
  populated record of the source keyword text (see Capabilities) so that a token with no
  established mapping — either a genuinely new mechanic (`Hazardous`, `Precision`, `Heavy`, none
  of which have a corresponding flag today) or a context-dependent alternate spelling of an
  *already*-modeled flag (`Cleave` as melee's equivalent of `Blast`; `Close Combat` as a
  generalization of `Pistol`) — is never silently lost, even though this change does not attempt
  to recognize either kind of alias. See `design.md`.
- Explicitly ignore BSData's composition/validation apparatus (`constraints`, `modifiers`,
  `conditionGroups`, `associations`/"Led by" eligibility) — matches the project's standing
  "trust the exporting app, no composition-rule validation" stance; this loader only needs to
  resolve names to rules data, not validate that a chosen roster is legal.

## Capabilities

### New Capabilities
- `catalogue-json-ingestion`: reading BSData 11e JSON catalogue files, resolving a faction's full
  `catalogueLinks` closure, and mapping resolved entries into `Domain.Catalogue` objects
  (`Datasheet`, `Statline`, `WeaponProfile`, `Ability`), queryable by name.

### Modified Capabilities
- `datasheet-catalogue`: `WeaponProfile` gains a verbatim, always-populated record of its source
  keyword text, independent of which (if any) existing typed ability flags a given token also
  maps to — see `design.md` for why rendering must not be reconstructed from flags alone.

## Impact

- **New code**: `ProbHammer.Core.Domain.Catalogue.Bsdata` (see `design.md`) containing the JSON
  reader, closure resolver, and mapper. No existing `Domain/Catalogue` types change shape as a
  result of this change alone, aside from the `WeaponProfile` addition under Modified
  Capabilities.
- **Not in scope**: the export-parser text grammar (already explored separately); the enrichment
  step that combines a parsed export with this loader's resolved `Datasheet`s to construct
  `Unit`/`AttachedUnit` objects (deliberately deferred to its own future change — it cannot be
  designed sensibly until both producers exist, and is expected to differ per export format while
  this loader stays format-agnostic); wiring this loader into `ProbHammer.Web`/`/LivePlay` (still
  runs on fixtures); fetching catalogue files over HTTP from BSData's GitHub repository (this
  change reads a local clone during development to avoid GitHub rate limits — see `design.md`).
- **Forward-looking, not delivered here**: the BSData files change slowly and are a natural
  in-memory cache candidate once the app supports multiple concurrent users/sessions. This change
  should keep the loader/resolver behind a boundary that a later caching layer can sit behind
  without reshaping resolution logic — noted as a design constraint, not a feature this change
  implements.
