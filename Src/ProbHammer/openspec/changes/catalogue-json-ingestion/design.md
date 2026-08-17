## Context

A locally-cloned copy of the BSData 11th Edition repository (`C:\Users\Pete\wh40k-11e`, not part
of this repo) provides one JSON file per faction/sub-faction — the BattleScribe `catalogueSchema`
shape, structurally the same generic selection-entry/profile/characteristic tree the archived 10e
pipeline already parsed from XML (see `legacy/10e-pipeline/.claude/bsdata-parsing.md`), just
JSON instead of XML and `catalogueLinks`/`importRootEntries` instead of `infoLink`/`profileLink`
for cross-file references. Direct inspection of three real captured exports
(`data/gw-app-export*.txt`) against the actual files (not just the schema in the abstract)
established:

- A name referenced by a real export (e.g. `Assault Intercessor Squad`, fielded by a Black
  Templars army) can be entirely absent from that faction's own file and resolvable only through
  an imported catalogue.
- That import chain is transitive, confirmed at least three levels deep
  (`Black Templars → Space Marines → Agents of the Imperium`/`Library - Titans`/`Imperial Knights
  - Library`), and applies even to a faction with a single, unsplit faction line
  (`Chaos - Chaos Space Marines` itself imports three further catalogues).
- A name can also be fully, locally redefined in a sub-faction's own file (`Crusader Squad` in
  `Imperium - Black Templars.json`) even though the parent catalogue it imports could plausibly
  carry a same-named generic version — local definitions must win.
- `InSv` is exposed as a first-class `Unit`-typeName characteristic in 11e JSON, unlike 10e where
  it required text/infoLink extraction.

See `proposal.md` for motivation and full scope. This document covers the resolution/mapping
approach only.

## Goals / Non-Goals

**Goals:**
- Resolve a name to a `Datasheet` correctly across a faction's real transitive import graph.
- Map BSData's `characteristics`-by-name shape onto the existing `Domain.Catalogue` types without
  requiring any change to those types' public shape (with the single open exception of the weapon
  ability-flag vocabulary — see Decisions).

**Non-Goals:**
- Building the `Unit`/`AttachedUnit` graph (the enrichment step) — deferred to a separate change,
  per `proposal.md`'s Impact section.
- Feel No Pain extraction. Unlike `InSv`, FNP has no field anywhere on `Statline` or `Datasheet`
  today, and nothing downstream consumes it. Adding a field for it is a `datasheet-catalogue`
  shape change this proposal doesn't make; extracting FNP without anywhere to put it isn't
  meaningful work. Revisit once something needs it.
- Any caching or persistence layer. See Risks/Trade-offs for why the design still shapes itself
  around this being added later.
- Fetching catalogue files over HTTP from the official BSData GitHub repository. The archived 10e
  pipeline already solved this (`bsdata-parsing.md`'s "Fetching Strategy" — download on first run,
  cache to disk) and this change's runtime file-loading boundary (see Decisions) is deliberately
  shaped so that strategy can be dropped in later without touching resolution/mapping logic. Not
  built now specifically to avoid hitting GitHub API rate limits while iterating on the parser
  itself — development and tests read from a local clone instead (see Decisions).
- Rewriting `_UnitBlock.cshtml`'s `WeaponAbilityTags` to read the new verbatim keyword record
  instead of reconstructing text from flags. This change gives `WeaponProfile` the field that
  rewrite would need, but doesn't perform it — matches the existing "not wiring this loader into
  `/LivePlay`" boundary above; the fixtures `/LivePlay` currently renders from could adopt the new
  field independently of this change or the loader ever being wired up.
- Recognizing context-dependent alternate spellings of an existing flag (`Cleave` for melee
  `Blast`, `Close Combat` for `Pistol`) as that flag. See Decisions and Risks/Trade-offs.
- Validating that a resolved roster is a legal build (constraints/modifiers/associations)  —
  matches the project's standing "trust the exporting app" stance (see
  `.claude/domain-model-11e.md`'s Deliberate Omissions).

## Decisions

**Resolve per-faction closures, not one global corpus-wide map.**
Alternative considered: load all ~40 files into one flat `Dictionary<name, entry>` (mirrors the
archived 10e pipeline's `_globalProfiles`, and was the initial instinct going into this design).
Rejected: 10e's global map was scoped to shared *rule-text* `infoLink` targets specifically, a
narrower problem than resolving full unit names. Generic wargear names (e.g. a basic weapon
carried by dozens of unrelated chapters) very plausibly get independently-defined entries with
different ids across files that have no import relationship at all — a flat name-keyed map spanning
the whole corpus would let whichever file happened to load last silently win, which is correct by
luck at best. Resolving only the closure reachable from the army's own starting file avoids this
by construction: two unrelated codexes' same-named entries are never both in scope for the same
resolution.

**Local-file entries always win over imported ones; among sibling imports with no local override,
declared `catalogueLinks` order wins.**
The first half is directly confirmed behavior (`Crusader Squad`). The second half — precedence
between two *sibling* imported catalogues that both define the same name — has no confirmed
real-world trigger in the sampled data; declared order is a decidable, arbitrary default chosen so
the rule is total rather than left ambiguous. Revisit if a real conflict surfaces.

**Match characteristics by their `name` field, never by `typeId`.**
`typeId` values are GUIDs into a separate characteristic-type registry this design never loads.
Every characteristic relevant to the existing `Domain.Catalogue` shape (`M`, `T`, `Sv`, `W`, `LD`,
`OC`, `InSv`, `Range`, `A`, `BS`/`WS`, `S`, `AP`, `D`, `Keywords`, `Description`) already has a
stable, human-readable `name` — the same shorthand `Statline`/`WeaponProfile`'s own fields already
use, which is itself a signal those field names were chosen with this data source in mind (see
`.claude/domain-model-11e.md`). Resolving `typeId` would add a whole second lookup graph for no
behavioral benefit.

**Squad vs. single-model detection mirrors the 10e pipeline's already-solved approach:** a
`Unit`-typeName profile on the entry itself means single-model; a `Unit`-typeName profile nested
on distinct child entries means squad, one `Statline` per distinct child. Confirmed structurally
present in 11e JSON (`Crusader Squad`'s `Sword Brother`/`Initiate`/`Neophyte` children), not just
assumed from 10e precedent.

**`WeaponProfile` gains a verbatim, always-populated record of its source `Keywords` text,
decoupled entirely from the existing typed ability flags. Rendering reads only that verbatim
record, never the flags. Recognizing a new token — whether a wholly new mechanic or an alternate
spelling of an already-modeled flag — is explicitly deferred, not attempted in this change.**

Two different kinds of gap turned up in the sampled data, and they carry different risk if
mishandled:

- Tokens with **no existing behavioral home** (`Hazardous`, `Precision`, `Heavy`). Worst case for
  these: a tag goes unrendered. Each is mechanically a poor fit for a flat `WeaponProfile` bool
  even if one were added — `Hazardous` is a self-damage side effect with no `Simulation/*`
  consumer (paused, unwired from this domain model per `domain-model-11e.md`'s Deliberate
  Omissions); `Heavy` is conditional on movement state `Domain.Roster` doesn't track at all;
  `Precision` is a target-allocation mechanic, not a per-attack modifier, and doesn't belong on
  `WeaponProfile` as an ability flag at all. None of the three are one decision — extending the
  domain model for them ahead of a concrete consumer (the simulator) risks guessing wrong about a
  shape that differs per keyword anyway.
- Tokens that are **context-dependent alternate spellings of an already-modeled flag**
  (`Cleave` — melee's equivalent of ranged `Blast`; `Close Combat` — a generalization of
  `Pistol`). These are the more dangerous case if mishandled, in two concrete ways: (1)
  `AggregateWeaponEntry` groups by `WeaponProfile.EqualityKey()` — two rules-identical weapons
  that happen to use different spellings for the same rule would silently fail to group if one
  spelling's flag is set and the other's isn't; (2) `_UnitBlock.cshtml`'s `WeaponAbilityTags`
  reconstructs display text *from* the flags (`if (w.Blast) tags.Add("BLAST")`) — if `Cleave` were
  mapped onto `Blast` without also preserving the source spelling, a player whose own datasheet
  says `Cleave` would see `/LivePlay` render the wrong word, `"BLAST"`. That's worse than an
  unrendered tag: confidently wrong is worse than silently missing.

Given both risks trace back to the same root cause — rendering derived from flags instead of from
source text — the fix addresses the root cause rather than patching each keyword individually:
`WeaponProfile` retains the exact source keyword text, in source order, for every token,
unconditionally, regardless of whether that token also sets a typed flag. Rendering is rebuilt to
read only that verbatim record, which retires flag-derived text reconstruction entirely — a
`Cleave`-tagged weapon renders `"CLEAVE"` whether or not anything ever recognizes it as `Blast`.

What this change deliberately does **not** do: build an alias table recognizing `Cleave`/`Close
Combat` (or any other not-yet-seen alternate spelling) as an existing flag. The typed flags
continue to be set only for tokens whose mapping is already established and exact. This is an
accepted, explicit trade-off, not an oversight — see Risks/Trade-offs.

**Catalogue file access goes through a narrow "get JSON content for this filename" boundary,
backed by local-disk reads against a configuration-supplied root directory — not a hardcoded
path.** This change reads from a local BSData clone (development machine only; not part of this
repo) to avoid GitHub API rate limits while the parser itself is under active iteration (see
Non-Goals). The root directory is supplied via configuration (e.g. `appsettings.json` /
environment variable), never hardcoded into source, so the path doesn't leak a specific
developer's machine into checked-in code. The boundary itself is what makes the later HTTP-fetch
work (mirroring the 10e pipeline) additive rather than a rewrite: swapping the local-disk
implementation for an HTTP-fetching one behind the same "content for this filename" shape touches
nothing in closure resolution or mapping. The same boundary is also what the future caching layer
(see Non-Goals, Risks/Trade-offs) sits behind.

**Namespace: `ProbHammer.Core.Domain.Catalogue.Bsdata`.** Matches the existing `Domain.Catalogue`
namespace this change populates (see `.claude/domain-model-11e.md`) rather than introducing a new
top-level area; resolves `proposal.md`'s "exact namespace TBD in design.md" forward-reference.

**JSON deserialization: `System.Text.Json`, no third-party dependency.** No JSON parsing exists
anywhere in this project yet to set precedent, but the project already has an explicit stance
against third-party libraries for the equivalent XML case (`CLAUDE.md`: "No third-party XML
library — use `System.Xml.Linq`"). Applying the same reasoning: `System.Text.Json` is .NET 8's
built-in default and sufficient for the deserialization shapes in `tasks.md` §1.

**Automated tests use trimmed real excerpts copied into
`tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/`, not the external local clone directly.**
Matches the project's existing fixture convention (`tests/ProbHammer.Tests/Domain/Fixtures/`,
referenced in `.claude/domain-model-11e.md`). A test that reads
`C:\Users\Pete\wh40k-11e` directly only passes on this machine with that clone present — it can't
run in CI or for another contributor. The many "verify against real data" items in `tasks.md` are
manual/exploratory verification steps during implementation (run once against the live local
clone to confirm the design's claims); whichever of them should become permanent regression
coverage need their real-data excerpt copied into the fixtures directory first.

## Risks / Trade-offs

- **[Risk]** Because alias recognition is deferred, two weapons that are rules-identical but
  spelled differently in source data (e.g. a `Blast` ranged weapon and a `Cleave` melee weapon
  that turn out to be the same underlying rule) will not be recognized as equivalent by anything
  that reads the typed flags — `EqualityKey`-based aggregation groups them separately, and any
  future simulation would treat them as unrelated. → **Mitigation**: accepted deliberately for
  this change (see Decisions) — verbatim keyword text guarantees nothing is *lost*, only that some
  semantic recognition is *not yet built*; adding an alias later requires no storage shape change,
  since the verbatim record already retains everything needed to reparse.
- **[Risk]** Whether a repeated same-named child entry (e.g. `Crusader Squad`'s four `Initiate`
  occurrences) represents genuinely distinct model variants or duplicate/linked references to one
  shared definition is not yet traced. → **Mitigation**: verify by inspection before implementing
  the child-model-entry walk; the behavior contract in `specs/catalogue-json-ingestion/spec.md`
  is agnostic to which turns out to be true, so this doesn't block design, only implementation
  sequencing (belongs in `tasks.md`).
- **[Risk]** Large per-file JSON deserialization cost (`Imperium - Space Marines.json` is ~7 MB)
  repeated on every resolution within a closure that includes it, and — once HTTP fetching lands —
  repeated network cost too. → **Mitigation**: not addressed by this change (see Non-Goals), but
  the file-access boundary described in Decisions exists specifically so both a future in-memory
  cache and the future HTTP-fetching source can sit behind it without either one reshaping closure
  resolution or mapping logic.
- **[Risk]** An `entryLink`'s `targetId` gives no indication on its own whether it resolves inside
  the same file or into an imported one — confirmed the hard way during design exploration, where
  an initial claim that `Crusader Squad` was import-only turned out to be wrong on inspection.
  → **Mitigation**: implementation must always attempt local resolution before falling through to
  imports, never infer resolution location from an `entryLink`'s shape alone.

## Migration Plan

None — this change is purely additive. No existing code references the types or namespace this
introduces yet.
