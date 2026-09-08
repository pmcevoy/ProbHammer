## Context

See `proposal.md` - Why for motivation. Relevant current state:

- `tools/RuleEffectClassificationReport/Program.cs` walks the live BSData clone, groups every
  distinct rule/ability text by `RuleEffectClassifier.Normalize`d Text, classifies each with
  `RuleEffectClassifier.Classify`, and prints three flat sections (Effect / Target-only /
  default-only) - no memory of a previous run.
- `RuleClassification` is `(RuleTarget Target, IReadOnlyList<CharacteristicEffect> Effects)` - a
  small, closed record. Not wired to anything yet.
- `src/ProbHammer.Web/BsData/*.json` is the precedent for bundling checked-in data into the Docker
  image as its own layer: `<Content Remove="BsData/**" />` in `ProbHammer.Web.csproj`, a dedicated
  `COPY --link` line in `Dockerfile`, and a config key (`Bsdata:RootDirectory`) resolved against
  `IWebHostEnvironment.ContentRootPath`. `LocalDiskBsdataCatalogueSource.ListFileNames()` does a
  flat `Directory.GetFiles(rootDirectory, "*.json")` over that directory - every file it returns is
  treated as a real catalogue file by every consumer (`BsdataClosureResolver`, every CorpusScan
  test), so nothing unrelated can share that directory.
- `tests/.../ArmyListParserTests.ReadDataFile` resolves a checked-in fixture path via
  `[CallerFilePath]` relative to the test source file itself, rather than a hardcoded absolute path
  - portable across machines/checkouts. This differs from
  `RuleEffectClassificationReport.DefaultClonePath`, which is deliberately a hardcoded
  machine-specific path because the BSData clone lives *outside* the repo.

## Goals / Non-Goals

**Goals:**
- A checked-in baseline that lets `RuleEffectClassificationReport` skip reprinting anything already
  verified and unchanged, while never silently swallowing an actual change to a verified result.
- A location and packaging story for the baseline consistent with the assumption that `/LivePlay`
  will eventually read it too (see proposal.md - Why), without wiring that consumption now.
- A snapshot-write workflow cheap enough to run after every review pass.

**Non-Goals:**
- Wiring `RuleClassification`/the baseline into `BsdataDatasheetMapper`, `AttachedUnitAggregator`,
  or `/LivePlay`. This change only makes the baseline exist and makes the report tool respect it.
- Any change to `RuleEffectClassifier`'s own classification logic (the possessive-phrasing fix, the
  "+1 OC" shorthand fix, and caveat detection are separate, follow-on changes that build on this
  one).
- A UI or CLI editor for the baseline beyond `--write-baseline`. Notes (see below) are edited by
  hand in the checked-in JSON, the same way an `AllowlistEntry<T>.Description` is hand-authored
  today.

## Decisions

### Baseline file shape: JSON, keyed by normalized Text, one record per entry

```json
{
  "entries": [
    {
      "text": "<RuleEffectClassifier.Normalize'd text>",
      "target": { "kind": "AttachedUnit" },
      "effects": [ { "characteristic": "Oc", "verb": "Improve", "amount": 1 } ],
      "note": null
    }
  ]
}
```

Keyed by Text, not `(Name, Text)` - mirrors the report tool's own existing grouping rationale
(`RuleEffectClassifier.Classify` never reads `name`, so two different Names sharing identical Text
are the same classification result; keying on Name would let one real verified result silently
split into several baseline rows). `target`/`effects` mirror `RuleClassification`'s own shape
directly (`System.Text.Json` polymorphic serialization for `RuleTarget`, matching how the domain
type is already structured) rather than inventing a parallel representation - keeps the drift
comparison a straightforward structural diff against a freshly-computed `RuleClassification`, no
translation layer to keep in sync as the domain type grows fields.

`note` is optional (`null` when absent) and free text, present only when a human has recorded that
this entry's classification is verified correct but known incomplete (see proposal.md - What
Changes; the 5 caveated seed entries). Not a structured field (e.g. not an enum of "gap kinds") -
mirrors `AllowlistEntry<T>.Description`'s own free-text convention elsewhere in this codebase for
"here's a human explanation of a known exception," and there's no shared vocabulary yet for
*kinds* of missing content to structure it against.

**Alternative considered:** a hand-authored C# file (`VerifiedRuleClassifications.cs`), matching
the `CharacteristicResolutionAllowlist.cs`/`WeaponKeywordAllowlist.cs`/
`BracketTokenResolutionAllowlist.cs` convention every other CorpusScan allowlist uses. Rejected:
those files are exception lists a person writes and rarely touches; this baseline is written
*programmatically* by `--write-baseline` after every review pass, and a machine-generated C# file
diffed via `git diff` is worse to read than machine-generated JSON. The `note` field is the one
genuinely hand-authored part, and free-text JSON is no harder to hand-edit than a C# collection
initializer would be.

### Per-field diff: structural JSON diff between two serialized `RuleClassification` snapshots

For a baselined text, deserialize the baseline entry's `target`/`effects` alongside the freshly
computed `RuleClassification`'s own JSON serialization, then compare:

- A property present in both and unequal → **drift** (surface).
- A property present in the baseline but producing an equal value on the fresh run → contributes to
  **unchanged** (don't surface).
- A property that exists in the current `RuleClassification` shape but has no corresponding key in
  the (older) baseline JSON → **schema growth**: compute it fresh, compare against that property's
  defined default (`SelfRuleTarget`+`[]` today; any future field's own defined default), backfill
  silently if it matches, else surface once as new information.

This reuses ordinary JSON structural comparison rather than hand-writing a field-by-field
`RuleClassification`-typed comparator, so a future field added to `RuleTarget`/`CharacteristicEffect`
needs no matching change to the diff logic itself - only that field's own "what's the boring
default" definition, which the schema-growth rule already requires regardless.

An entry is only ever fully "unchanged" (collapsed to the summary count) when every property
diffs clean. A single drifted or newly-surfaced property is enough to print the whole entry, with
the specific field(s) that changed called out - not a partial silent update of only the unchanged
half.

### File location: `src/ProbHammer.Web/Data/RuleEffectClassifications.json`

A sibling of `BsData/`, never nested inside it (see Context - `ListFileNames()` risk). Same
packaging treatment as `BsData/`: `<Content Remove="Data/**" />`, its own `COPY --link` Dockerfile
layer, a `RuleEffectClassifications:FilePath` config key (default resolved against
`ContentRootPath`, mirroring `Bsdata:RootDirectory`'s own convention) - even though nothing reads
this config in `Program.cs` yet in this change, since no consumer is wired. Registering the config
key now, unused, is deliberately *not* attempted here - Non-Goals already excludes wiring, and an
unused config registration with no consumer would be exactly the kind of premature scaffolding
[[feedback_avoid_premature_behavior_on_unconsumed_domain_types]] warns against. The path is instead
just a documented convention the eventual consuming change will register.

### Report tool's own path resolution: `[CallerFilePath]`, not a CLI-overridable machine path

Unlike `DefaultClonePath` (the external BSData clone, genuinely outside the repo and
machine-specific), the baseline file lives *inside* this repo. `RuleEffectClassificationReport`
resolves its default path the same way `ArmyListParserTests.ReadDataFile` resolves checked-in
fixtures - `[CallerFilePath]`-relative, walking up from the tool's own source file to
`src/ProbHammer.Web/Data/RuleEffectClassifications.json` - portable across machines and checkouts
with no config needed. Still overridable via a command-line argument, mirroring the clone-path
argument's own precedent, in case a future test wants to point the tool at a scratch baseline.

## Risks / Trade-offs

- **[Risk]** `RuleEffectClassifier.Normalize` changing in the future (as it already has once, per
  its own doc comment) shifts a baseline entry's key, making it look like a brand-new, unverified
  text even though nothing about the underlying rule/ability changed. → **Mitigation**: none
  attempted here; this mirrors a limitation the corpus report tool already has today (a
  `Normalize` change already shifted bucket counts once, per `RuleGlossary.Build`'s own history) and
  is out of scope to solve generally. A `Normalize` change is rare and already requires a deliberate
  code change to `RuleEffectClassifier` itself, so the resulting one-time re-review is bounded and
  visible (a burst of "new" entries with familiar text), not silent.
- **[Risk]** JSON structural diffing conflates "field added to the schema" with "field renamed" -
  a rename would look like schema growth (new key) for every existing entry, silently backfilling a
  default rather than correctly recognizing the renamed field's already-known value. →
  **Mitigation**: none attempted here; a rename to `RuleTarget`/`CharacteristicEffect` should be
  paired, in whatever future change makes it, with a one-time baseline migration script - not
  something this change's diff mechanism needs to anticipate generically.
- **[Trade-off]** The `note` field being free text (not structured) means the report tool can't do
  anything mechanical with it beyond displaying it - it's purely for a human reader's benefit,
  consistent with `AllowlistEntry<T>.Description`'s own role elsewhere.

## Migration Plan

Additive only - a new file, a new tool mode, a new report-listing branch for texts already in the
baseline. No existing behavior changes for any text not yet in the baseline (618 Target-only + 3192
default-only + any future unreviewed Effect result keep printing exactly as today). No rollback
concerns beyond deleting the new file and reverting the tool/csproj/Dockerfile changes.
