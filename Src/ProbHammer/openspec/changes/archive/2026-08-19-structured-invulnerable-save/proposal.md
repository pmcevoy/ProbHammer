## Why

Real BSData datasheets give some models a conditional or multi-valued invulnerable save (a
different value depending on whether the attack is ranged or melee), which `Statline.InSv: int`
cannot represent. `BsdataDatasheetMapper.ParseThreshold` currently throws
`AmbiguousCharacteristicException` for every such case — correct behavior, but tracked only as an
allowlisted "known limitation" (`bsdata-corpus-scan-tests`) rather than resolved. That blocks
`/LivePlay` from ever rendering a real invulnerable save for ~50 real datasheets once the BSData
loader is eventually wired up, and there is no fixture today exercising the shape at all, so the
rendering side has never been designed against a realistic example.

## What Changes

- New `InvulnerableSave` value type (`MeleeInSv`, `RangedInSv`, `Caveated`, `CaveatAbility`)
  replaces `Statline.InSv`'s type, from `int` to `InvulnerableSave` (the property keeps its
  existing name — `Statline.InSv`). **BREAKING**: the field's shape changes; the one existing
  consumer (`_UnitBlock.cshtml`) is updated in this change.
- Self-contained raw-text shapes — a plain digit, a `"(Ranged)"`/`"(Melee)"` parenthetical, and the
  un-footnoted half of a `"/"`-split — resolve directly at parse time with `Caveated = false`, no
  ability lookup involved.
- Footnoted/ability-linked shapes — a bare trailing `"*"`, or a `"/"`-split where one side carries
  `"*"` — always resolve to `Caveated = true` with a captured, entry-scoped, id-resolved reference
  to the specific linked `Ability` (`MeleeInSv`/`RangedInSv` populated with the one value known
  with confidence — the plain/un-footnoted digit where one exists, otherwise the bare parsed digit
  — as a placeholder pending later interpretation). Resolution is deliberately never a name-only
  lookup: the same ability name (e.g. `"Invulnerable Save (4+*)"`) can denote two different
  abilities with opposite meanings in the real corpus, so the mapper always resolves the specific
  candidate reachable from the owning entry (a local nested profile or a `"profile"`-type
  infoLink), by id.
- No interpretation of the linked ability's text happens at this layer — classifying what a
  `Caveated = true` ability actually means is explicitly deferred (tracked as a new vNext idea,
  `.claude/vnext-ideas.md`, to be picked up as its own future change).
- `BsdataDatasheetMapper.MapStatline` gains access to its owning entry and the closure's
  resolution context (id/profile indices) to perform this entry-scoped lookup — a real
  signature/plumbing change to how statlines are mapped, not just a value-type widening.
- `AmbiguousCharacteristicException` for `InSv` narrows to only two cases, both currently
  unobserved in the full corpus: a genuinely new raw-text shape, or a footnoted value whose owning
  entry has no matching candidate ability at all. Every confirmed real shape (plain, parenthetical,
  `"/"`-split, bare footnote) now parses successfully instead of throwing, so the corpus-scan
  allowlist's `InSv` entry is removed rather than merely re-described.
- New hand-authored `Examples/` fixture(s) covering a multi-value/caveated invulnerable save —
  today's example army has none, so `/LivePlay`'s rendering has nothing real to be designed
  against.
- `/LivePlay` renders the resolved value: the common uniform case (`MeleeInSv == RangedInSv`,
  `Caveated == false`) is unchanged ("4++"); a differing melee/ranged value or a `Caveated == true`
  value gets a distinct visual treatment, including a way to reach the associated ability when one
  is present.

Out of scope, deliberately: `WeaponProfile.S`'s dice-notation Strength (Ork Battlewagon `D6+6`
etc.) is the same class of representation gap but is being handled as its own separate change.

## Capabilities

### New Capabilities
- `invulnerable-save`: the `Statline.InSv` value type's shape and the parsing rules
  that populate it from BSData's raw `InSv` characteristic text — self-contained resolution,
  entry-scoped ability-reference capture for footnoted forms, and the narrowed exception
  conditions.

### Modified Capabilities
- `catalogue-json-ingestion`: `Statline Extraction`'s `InSv` handling changes from a direct-read
  scenario to producing the new `InvulnerableSave` value, including the entry-scoped ability
  resolution described above.
- `bsdata-corpus-scan`: the "known ambiguous-InSv pattern is recorded" scenario no longer applies —
  the confirmed real InSv shapes stop raising an exception at all, so the corpus scan's InSv
  allowlist entry is removed.
- `live-play-view`: `Statline Section Rendering`'s invulnerable-save scenarios are extended to
  cover a differing melee/ranged value and a caveated value, alongside the existing
  present/absent scenarios.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/Statline.cs` — new `InvulnerableSave` type;
  `Statline.InSv` retyped from `int` to `InvulnerableSave`.
- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/BsdataDatasheetMapper.cs` — `MapStatline`
  signature/logic change, entry-scoped ability resolution.
- `tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/CharacteristicResolutionAllowlist.cs`
  — InSv allowlist entry removed.
- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` — invulnerable-save rendering.
- `src/ProbHammer.Core/Examples/` — new fixture(s) exercising a multi-value/caveated save.
- `.claude/domain-model-11e.md`, `.claude/design-tokens.md` — doc updates for the new shape and any
  new rendering token.
- `.claude/vnext-ideas.md` — already updated during exploration with the deferred ability-text
  interpretation pass idea this change's `Caveated = true` cases feed into.
