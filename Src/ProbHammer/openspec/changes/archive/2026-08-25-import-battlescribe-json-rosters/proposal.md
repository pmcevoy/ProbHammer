## Why

Some players don't use the official GW app at all — its per-army-book content is paywalled, while
NewRecruit (a BattleScribe-compatible army builder) is free — so they have no GW-app text export to
paste into `/Import` in the first place. Separately, investigating real Android GW-app export
failures (see the sibling change `harden-army-list-parsing-for-android-exports`) surfaced that
NewRecruit can export a roster as JSON in the standard BattleScribe `rosterSchema` shape
(`xmlns: "http://www.battlescribe.net/schema/rosterSchema"`), and that this JSON is **already fully
resolved** — statlines, weapon profiles, ability/rule text, and the Leader/Support/Bodyguard
attachment relationship are all present inline, needing no BSData catalogue lookup at all. A real
captured sample (`data/gw-app-export-templars.json`, the existing Templars fixture round-tripped
through NewRecruit) was analyzed in detail and confirmed this: it resolves attachment via a
structural `associations` field, already splits multi-model squads into per-loadout sub-selections
(eliminating the partition-ambiguity class of bug the text pipeline has), and tags Enhancements with
a distinct `costs` entry — three of the hardest problems in the existing text pipeline simply don't
exist in this format. Building a second, independent import pipeline for it both serves NewRecruit-
only users directly and gives Android users (whose native export text has real, so-far-unresolved
ambiguities) a reliable path to `/LivePlay`: export from GW app, clean up in NewRecruit, export as
JSON.

## What Changes

- A new import pipeline: BattleScribe roster JSON → `ArmyRoster`, bypassing
  `Domain.Catalogue.Bsdata` entirely for this path. Every `Datasheet`/`Statline`/`WeaponProfile`/
  `Ability` needed is synthesized directly from the JSON's own already-resolved `profiles`, not
  looked up against a BSData catalogue closure.
- `/Import` gains format detection: a submitted payload that parses as JSON (with the expected
  `roster`/`xmlns` shape) is routed to the new pipeline; anything else continues through the
  existing `army-list-parsing` → `army-roster-enrichment` pipeline unchanged.
- **BREAKING** (internal only, no persisted/external data): `ISessionArmyListStore` and
  `IArmyRosterProvider`'s signatures change from `ParsedArmyList`-specific to a small format-
  discriminated wrapper, so `/LivePlay` and casualty-sync stay format-agnostic. Session state is
  in-memory and per-process (`AddDistributedMemoryCache`), so there is nothing to migrate — a
  restart already clears it today.
- Attachment (Leader/Support → Bodyguard) is resolved from the roster's own `associations` field
  (`"Leading"`/`"Supporting"` → target selection id), not from parsed text.
- Enhancement selections are recognized via a `costs` entry named `"Enhancements"` on the selection
  itself — a more reliable signal than the existing text pipeline's group-name substring match, and
  one that structurally cannot leak an unselected Enhancement (the roster only ever contains actual
  selections).
- Ability/rule text already carries the same `**bold**`/`^^small-caps^^`/`[BRACKET]` markup the
  existing `RuleTextEmphasisRenderer`/`RuleTextTokenizer` already parse; a roster-scoped
  `RuleGlossary` is built by collecting every distinct `rules[]` entry found anywhere in the roster
  (by id), reusing the existing `RuleGlossary`/`RuleDefinition` types rather than inventing new ones.

## Capabilities

### New Capabilities
- `battlescribe-roster-import`: parses and maps a BattleScribe `rosterSchema` JSON export directly
  into `Domain.Roster` shapes (`Unit`/`AttachedUnit`/`ModelLine`/`ArmyRoster`), including attachment
  resolution, Enhancement recognition, and statline/weapon/ability extraction — with no BSData
  catalogue dependency.

### Modified Capabilities
- `army-list-import`: `/Import`'s submission handling recognizes either a GW-app text export or a
  BattleScribe JSON export and routes to the corresponding pipeline; per-session storage becomes
  format-agnostic (still storing only the source-level intermediate, never a built `ArmyRoster`,
  per the existing "session stores the intermediate, not the graph" principle).

## Impact

- New code under `ProbHammer.Core.Domain.Import` (or a sibling namespace — see design.md) for the
  BattleScribe roster JSON model and its mapping into `Domain.Roster` shapes.
- `src/ProbHammer.Web/Services/SessionArmyListStore.cs`, `ArmyRosterProvider.cs`: signature changes
  to a format-discriminated stored-import type.
- `src/ProbHammer.Web/Pages/Import.cshtml.cs`: format detection on submission.
- No change to `army-list-parsing`, `army-roster-enrichment`, `catalogue-json-ingestion`, or
  `Domain.Catalogue.Bsdata` — the existing GW-app text pipeline is untouched and continues to work
  exactly as it does today.
- Verified so far against exactly one real captured sample (`data/gw-app-export-templars.json`, one
  faction, one detachment) — see design.md's Risks for what still needs broader verification before
  this is considered fully robust.
