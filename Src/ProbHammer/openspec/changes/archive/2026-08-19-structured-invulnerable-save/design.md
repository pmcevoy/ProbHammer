## Context

See `proposal.md` for motivation. Relevant current state:

- `Statline.InSv: int` (`src/ProbHammer.Core/Domain/Catalogue/Statline.cs`), with `0` meaning
  "absent" — the convention every other `BsdataDatasheetMapper` threshold parser already follows.
- `BsdataDatasheetMapper.ParseThreshold` validates `InSv` text against
  `CharacteristicPattern.InvSv` (`^(\d+)[+*]?$`) and throws `AmbiguousCharacteristicException` for
  anything else. `MapStatline` currently takes only a bare `BsProfile` — it has no access to the
  owning `BsSelectionEntry` or the closure's id/profile indices (`WalkContext`), both of which live
  one level up in `WalkEntry`/`ProcessProfile`.
- `ProcessProfile`'s `"Abilities"` case already collects every ability profile it walks — local or
  reached through an `infoLink` — into `WalkContext.Abilities`, deduped **by name**
  (`SeenAbilityNames`, first-occurrence-wins).
- Investigation against the live BSData clone (this session) confirmed the real shapes and, more
  importantly, the resolution mechanism:
  - `"5+ (Ranged)"` (Warhound Titan) is fully self-contained — no ability involved at all.
  - `"5+*"` (Canis Rex) resolves through `entry.InfoLinks` (`Type == "profile"`, cached
    `Name == "Invulnerable Save (5+*)"`) into the base rules catalogue's `sharedProfiles`.
  - `"2+*"` (Makari, Orks) resolves through a **locally-nested** `Abilities` profile in the same
    entry's own `Profiles` — not an infoLink at all.
  - **Confirmed name collision**: the base catalogue contains two different profiles both named
    `"Invulnerable Save (4+*)"` — one `"...against ranged attacks"`, one `"...against melee
    attacks"` — with different ids. A name-only lookup cannot distinguish them; only the specific
    infoLink's `targetId` (or the specific locally-nested profile) can.
  - A full corpus-wide sweep (`grep` across every catalogue file) found exactly 7 distinct
    `"Invulnerable Save (...)"`-named ability texts (4+/5+, model/unit-scoped, ranged/melee) plus
    Makari's one confirmed outlier (an unrelated re-roll caveat, not an attack-type restriction) —
    no other digit values or phrasings exist anywhere in the clone.

## Goals / Non-Goals

**Goals:**
- Replace `Statline.InSv: int` with a value type that can represent a melee/ranged split and a
  "structurally recognized but not semantically resolved" state, without ever guessing.
- Keep `BsdataDatasheetMapper` a purely *structural* parser: it identifies shapes and, for
  footnoted shapes, resolves *which* ability is linked (by id) — it never reads or classifies what
  that ability's text means.
- Shrink `CharacteristicResolutionAllowlist`'s InSv entry to zero real occurrences, since every
  confirmed corpus shape now parses successfully instead of throwing.

**Non-Goals:**
- Classifying a caveated ability's Description text (the "against ranged/melee attacks" vs.
  Makari's re-roll caveat distinction) is explicitly out of scope — tracked as a new vNext idea
  (`.claude/vnext-ideas.md`, "Ability-text interpretation pass"), to be designed as its own future
  change once its shape is clearer, potentially generalizing beyond invulnerable saves (e.g. the
  user's stated longer-term interest in ability-driven weapon-attack modifiers).
- `WeaponProfile.S`'s dice-notation Strength (Ork Battlewagon `D6+6` etc.) is the same class of
  representation gap but is handled separately.
- Wiring `BsdataDatasheetMapper`'s output into `/LivePlay` at runtime (parsing a real army export)
  stays deferred, per `domain-model-11e.md`'s existing "Deliberately Out of Scope" note — this
  change only adds hand-authored `Examples/` fixtures so `/LivePlay`'s rendering can be built and
  verified against a realistic shape.

## Decisions

### `InvulnerableSave` shape: `{ MeleeInSv, RangedInSv, Caveated, CaveatAbility }`
A dedicated immutable value type (matching this project's existing pattern —
`DiceExpression`, `WeaponProfile` — rather than three loose fields on `Statline`), **non-nullable**
on `Statline` (preserving the existing "0 means absent" convention rather than introducing a
parallel nullable-absence representation). `CaveatAbility: Ability?` is null except when
`Caveated == true`, in which case it is always set — a caveated value with no captured ability
cannot occur (this is itself a spec requirement, since it's what lets `/LivePlay` point somewhere
useful).

Considered keeping `Statline.InSv: int` alongside a new richer field for backward compatibility.
Rejected: only one real consumer exists today (`_UnitBlock.cshtml`), this project's stated
conventions reject compatibility shims when the call site can simply be updated (see `CLAUDE.md`),
and the project already has a direct precedent for a full non-shimmed replace (`WeaponProfile.D`'s
`int` → `DiceExpression` widening).

### Resolution always defers footnoted/ability-linked values to `Caveated = true`
Originally explored classifying the ability's Description text inline in the mapper (matching it
against the 7 known templates via a "ranged attacks"/"melee attacks" substring or exact-sentence
match). Reversed after discussion: the parser's job is structural (recognize a shape, find the
right linked ability by id), not semantic (understand what that ability says). Folding
classification into the mapper would tie a narrow, single-purpose piece of text-matching logic
into `BsdataDatasheetMapper`, when the user's stated longer-term goal is a general ability-text
interpretation capability serving other effects too (e.g. attack-count modifiers feeding weapon
aggregation). Building the narrow case first and stranding it inside the mapper would mean
re-deriving this same split later anyway.

Consequence: **both** halves of a footnoted `"/"`-split (e.g. `"4+* / 5+"`) get the *same*
placeholder value on both `MeleeInSv`/`RangedInSv` — the unfootnoted side's digit, the one value
knowable without semantic interpretation — rather than attempting a partial resolution of the
split itself, since which side is which attack type is exactly the fact only the ability's text
carries.

### Entry-scoped, id-based ability resolution (never name-only) — ancestry chain, not one entry
Confirmed necessary by the name-collision finding above. `MapStatline` gains access to the *chain*
of `BsSelectionEntry` ancestors from the walk root down to (and including) whichever entry actually
owns the `Unit` profile, plus the closure's `WalkContext` (id/profile indices) — a real signature
change, not just a value-type widening. First implemented with a single nullable `owningEntry`
(just the immediate entry), which the corpus scan proved insufficient: **Howling Banshees**' squad
carries its `"Invulnerable Save (4+*)"` infoLink on the top-level squad entry, while the footnoted
`InSv` characteristic sits on a *nested child* model entry one level down inside a
`selectionEntryGroup` — the immediate owning entry alone never sees the link. Fixed by threading a
full ancestry list (built as `WalkEntry`/`WalkGroup` recurse through an entry's own
`SelectionEntries`/`SelectionEntryGroups`) and searching it nearest-first. An entry reached via an
`entryLink` (a shared, reusable target, e.g. a wargear option linked from many unrelated places)
starts a *fresh* ancestry rather than inheriting the linking entry's — it isn't structurally part
of whatever happened to link to it.

Candidate search per ancestor: its own locally-nested `Abilities` profiles (Makari's shape) before
its own `InfoLinks` where `Type == "profile"` (Canis Rex's shape) — never against the flattened,
name-deduped `WalkContext.Abilities`, which the name-collision finding shows can silently lose the
wrong candidate.

**A second real naming convention, also only found by running the corpus scan**: alongside the
digit-parameterized `"Invulnerable Save ({digit}+*)"`, a generic, unparameterized `"*Invulnerable
Save"` name is also real — confirmed locally-nested on Space Marines' Judiciar and Astraeus, and
infoLink-based on Chaos Knights' War Dog Executioner (and the whole War Dog/Knight family). Both
names are tried at every ancestor.

*Related, discovered but explicitly not fixed here*: `WalkContext.Abilities`/`SeenAbilityNames`'s
existing first-occurrence-wins dedup-by-name has the same latent collision risk for the *general*
ability list (not just InSv resolution) if a single datasheet ever needed two differently-meaning
abilities sharing one name. No real corpus datasheet currently exercises this. Left alone —
out of scope for this change, flagged here so it isn't lost.

### Game-system file is a closure member for `SharedProfiles` only, never for entries/groups
Running the corpus scan (task 4.2) surfaced that the base rules file (`Warhammer 40,000.json`) —
where the `"Invulnerable Save (X+*)"`/`"*Invulnerable Save"` shared profiles actually live — was
**never reachable at all**: `BsdataClosureResolver` only follows `catalogueLinks`, but every real
catalogue references its game system via a separate `GameSystemId` field, never a `catalogueLink`.
`BsdataCatalogueReader.Read` also unconditionally threw for this file (it's shaped
`{ "gameSystem": {...} }`, not `{ "catalogue": {...} }`). Fixed generally, per explicit user
direction ("the correct fix... means we won't need another workaround on other references"): one
`BsCatalogue` type now models both root shapes (`BsCatalogueFile` gained a second nullable
`GameSystem` property alongside `Catalogue`; `Read` returns whichever is populated), and
`BsdataClosureResolver.Resolve` now also resolves the current catalogue's `GameSystemId` against
every available file's own `Id` (no cached name to guess from, so — like
`ResolveImportFileName`'s own existing fallback — this scans every file once per closure).

**First attempt was too broad and caused a real regression, caught by the same corpus scan**:
folding the game-system file into `BsdataClosure.Files` (the same list `catalogueLinks` imports
populate) made its own `SharedSelectionEntries`/`SharedSelectionEntryGroups` reachable too —
previously-dangling `entryLinks` named `"Warlord"`/`"Enhancements"`/`"Crusade"` (silently
unresolved before, by design, per `WalkLink`'s own "target not found... skipped" comment) suddenly
resolved into generic, cross-faction template wargear this loader was never designed to map as
real weapons — T'au's `"Ethereal"` newly threw a `FormatException` reaching a weapon profile whose
`A`/`S`/`AP`/`D` characteristic was a literal `"*"` placeholder. Narrowed: `BsdataClosure` now
carries the resolved game-system `BsCatalogue` as its own separate `GameSystem` property, entirely
outside `Files`. Only `BsdataNameResolver.BuildProfileIdIndex` reads it (merging in its
`SharedProfiles`); `BuildIdIndex`/`BuildGroupIdIndex`/`Resolve` never do, so ordinary entryLink/link
resolution can't reach the game system's own entries/groups — restoring the original "unresolvable
target silently skipped" behavior for links that were always meant to dangle, while still exposing
exactly the one thing real catalogue entries actually need from it.

### One genuine data anomaly remains allowlisted, not fixed
Aeldari's Archon/Ynnari Archon carry a footnoted `InSv` (`"2+*"`) with no matching ability anywhere
in the entry — neither name convention, local or linked, and none of the entry's own abilities
(Leader/Overlord/Devious Mastermind/Hatred Eternal) mention an invulnerable save either. Confirmed
by direct inspection, not assumed: this is a real BSData authoring gap (the footnote may reference
a physical-card note with no structured JSON equivalent), not a bug in this resolution logic —
correctly throws via the "no candidate ability found" path and is allowlisted narrowly (matched on
both `Characteristic: "InSv"` and the specific `Text: "2+*"`, not a broad catch-all), so a
different future InSv failure would still surface rather than being silently absorbed.

### Two failure modes stay exceptions; no case is silently downgraded further
- Raw text matching no recognized shape at all (unchanged from today's behavior).
- A footnote marker present but **no** candidate ability reachable from the owning entry (new —
  previously this was folded into the same generic "ambiguous" throw as every other InSv failure;
  now it's the *only* InSv shape left that throws, since it means the naming convention itself
  didn't hold, not that this parser doesn't yet understand a known shape).

Both are currently unobserved in the full corpus (confirmed by the sweep), so
`CharacteristicResolutionAllowlist`'s InSv entry is removed rather than re-scoped.

### `/LivePlay` rendering and fixtures
New hand-authored `Examples/` fixture(s) needed since the current example army has no
caveated/differing-value invulnerable save to render against — confirmed with the user that their
own army fixture doesn't exercise this shape. Rendering approach (uniform case unchanged;
differing/caveated case gets distinct visual treatment) to be iterated live in-browser via
`firefox-devtools-mcp`, per this project's established `/LivePlay` design workflow (see
`PROGRESS.md`'s history of icon/layout iterations) — exact visual treatment is a tasks-level/
implementation-time decision, not fixed here.

## Risks / Trade-offs

- **[Risk]** A caveated value's `MeleeInSv`/`RangedInSv` placeholder (same digit on both sides) can
  be wrong in the specific direction it implies, until the future interpretation pass resolves it
  — e.g. it doesn't distinguish "this could be worse in melee" from "this could be worse at range."
  → **Mitigation**: `Caveated = true` is exactly the signal that these two fields are not fully
  trustworthy yet; `/LivePlay`'s rendering must visually distinguish a caveated value from a
  resolved one, never present the placeholder as equally confident.
- **[Risk]** `MapStatline`'s new dependency on entry + closure context is a real signature change
  touching call sites inside `BsdataDatasheetMapper` itself (not external callers, since
  `BuildDatasheet` is the only public entry point). → **Mitigation**: contained entirely within one
  file; no `ProbHammer.Core` public API changes as a result.
- **[Risk]** The generic `"*Invulnerable Save"` name carries no digit, unlike
  `"Invulnerable Save ({digit}+*)"` — if two different ancestors in the same chain each carried a
  same-named generic ability with different meanings, the nearest one wins by construction (same
  precedence convention as everywhere else in this loader), which could theoretically pick the
  wrong one for a specific nested child. Unconfirmed in the real corpus (the full scan is clean);
  flagged rather than chased further since there's no concrete case to design against yet.
- **[Trade-off]** Deferring ability-text classification means this change alone does not make
  `/LivePlay` show a fully "correct" invulnerable save for the ~40 footnoted real datasheets (e.g.
  Aeldari Rangers still renders as caveated, not as a clean ranged-5+/melee-0 split) — only the
  self-contained shapes (`"(Ranged)"`/`"(Melee)"`) get a fully resolved value from this change.
  Accepted deliberately: the alternative (building the classifier now) was explicitly rejected in
  favor of scoping it as its own future change (see Non-Goals).
