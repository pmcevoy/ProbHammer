## 1. Web project data folder and Docker packaging

- [x] 1.1 Create `src/ProbHammer.Web/Data/` as a sibling of `src/ProbHammer.Web/BsData/`.
- [x] 1.2 Add `<Content Remove="Data/**" />` to `ProbHammer.Web.csproj`, mirroring the existing
      `<Content Remove="BsData/**" />` line.
- [x] 1.3 Add a `COPY --link src/ProbHammer.Web/Data/ /app/Data/` layer to `Dockerfile`, mirroring
      the existing `BsData/` copy line.

## 2. Baseline file shape and serialization

- [x] 2.1 Define the baseline JSON shape (`entries: [{ text, target, effects, note }]`) per
      design.md, serializing `target`/`effects` directly from `RuleTarget`/`CharacteristicEffect`
      via `System.Text.Json` polymorphic serialization.
- [x] 2.2 Write a loader that reads `src/ProbHammer.Web/Data/RuleEffectClassifications.json` into
      an in-memory lookup keyed by normalized Text.

## 3. Per-field diff

- [x] 3.1 Implement the structural diff between a baseline entry's stored `target`/`effects` JSON
      and a freshly-computed `RuleClassification`'s own serialized form: unequal-but-present-in-both
      → drift; present in current shape but absent from baseline → schema growth.
- [x] 3.2 Define each `RuleClassification` field's "boring default" for schema-growth backfill
      (today: `Target = SelfRuleTarget`, `Effects = []` — the same default the report's own
      default-only bucket already uses).
- [x] 3.3 Implement drift/backfill/unchanged classification per the diff, per
      specs/rule-effect-classification-baseline's three corresponding requirements.

## 4. Report tool integration

- [x] 4.1 In `RuleEffectClassificationReport/Program.cs`, resolve the baseline file's default path
      via `[CallerFilePath]` relative to the tool's own source file (mirroring
      `ArmyListParserTests.ReadDataFile`), overridable by a command-line argument.
- [x] 4.2 For each corpus text with a baseline entry: unchanged → count only, no listing; drift or
      schema-growth-with-non-default → print under a new "Changed since verified" section, showing
      which field(s) changed and their old/new values.
- [x] 4.3 Leave every text without a baseline entry on the existing Effect/Target-only/default-only
      listing path, unchanged from today's behavior.
- [x] 4.4 Add a `--write-baseline` mode: snapshot the current full classification for every corpus
      text already in or newly reviewed into the baseline, preserving any existing `note` field
      untouched.

## 5. Seed data

- [x] 5.1 Populate `RuleEffectClassifications.json` with the 20 currently-verified Effect results
      from the live corpus run (see `.claude/vnext-ideas.md`'s "Rule Effect Classification
      (Text-Only)" section and the current report output for the exact Target/Effects values).
- [x] 5.2 Record the known-incomplete `note` on the 5 entries with real unmodeled content: Blastajet
      Force Field, Leader-beast, Lesk's Heroes, Redoubtable Machine Spirit, Scattershield.

## 6. Verification

- [x] 6.1 Run the report tool against the live BSData clone and confirm all 15 fully-verified
      entries and 5 known-incomplete entries collapse to the unchanged count, with zero drift
      reported.
- [x] 6.2 Temporarily hand-edit one baseline entry's stored `Effects` to a wrong value, re-run, and
      confirm it's surfaced as drift — then revert the edit.
- [x] 6.3 Confirm the 618 Target-only and 3192 default-only results still print exactly as before
      this change.
- [x] 6.4 Run `--write-baseline` and confirm the resulting `git diff` against the seed data from
      task 5.1 is empty (the tool reproduces the hand-seeded values byte-for-byte).
- [x] 6.5 Update `.claude/domain-model-11e.md`'s "Rule Effect Classification (Text-Only)" section
      and `.claude/vnext-ideas.md` to record this change, per CLAUDE.md's Documentation Maintenance
      rule.
