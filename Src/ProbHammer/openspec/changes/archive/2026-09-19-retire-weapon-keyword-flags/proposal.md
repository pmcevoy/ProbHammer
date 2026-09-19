## Why

`WeaponProfile` carries a fixed set of typed ability flags (`Torrent`, `Blast`, `Melta`,
`RapidFire`, `SustainedHits`, `LethalHits`, `DevastatingWounds`, `TwinLinked`, `IndirectFire`,
`Pistol`, `IgnoresCover`, `Assault`, `Anti`) alongside `KeywordsText`, the weapon's verbatim
source keyword list. `WeaponKeywordParser` recognizes only an exact, hand-maintained vocabulary of
tokens and sets the matching flag; anything it doesn't recognize sets no flag. `/LivePlay`'s weapon
keyword chips are rendered from those flags alone (`WeaponAbilityTags()`), not from `KeywordsText` —
so a token the parser doesn't recognize doesn't just fail to set a flag, it vanishes from the live
view entirely, with no unresolved/dimmed chip and no visible sign anything is wrong. This is a
direct contradiction of `datasheet-catalogue`'s own "Weapon Profile Verbatim Keyword Text"
requirement, which already states rendering must read the verbatim record specifically so an
unrecognized token is never silently dropped — the requirement was never actually implemented that
way.

GW/BSData renames and adds weapon keywords on an ongoing basis (confirmed twice in one session:
`Pistol` renamed to `Close-Quarters`/`CLOSE-QUARTERS` for Space Marines and Orks via BSData's own
declared alias, and a `Cleave N` keyword with no corresponding flag at all, both silently vanishing
from `/LivePlay`), and this project is not building a Monte Carlo simulation engine that would
need these values as typed numbers/booleans — the flags' only live runtime consumer today is
`WeaponProfile.EqualityKey()`, which already also folds in the verbatim `KeywordsText` string
alongside every flag, making the flags redundant there too (already covered by
`WeaponProfileTests.EqualityKey_distinguishes_weapons_that_differ_only_in_verbatim_keyword_text`).
Every future keyword rename or addition currently requires a source-code change (new flag +
parser recognition + explicit rendering wire-up) before it stops silently disappearing; this change
removes that requirement by making the verbatim keyword bag the only representation.

## What Changes

- **BREAKING** (internal API only, no persisted data): Remove every typed ability flag and the
  `Anti` dictionary from `WeaponProfile` (`Torrent`, `Blast`, `Melta`, `RapidFire`,
  `SustainedHits`, `LethalHits`, `DevastatingWounds`, `TwinLinked`, `IndirectFire`, `Pistol`,
  `IgnoresCover`, `Assault`, `Anti`) and from `WeaponProfileEqualityKey`. `KeywordsText` becomes
  the sole representation of a weapon's keywords.
- Replace `WeaponKeywordParser`'s recognized-vocabulary flag-mapping with plain tokenization:
  split the `Keywords` characteristic on `,`, trim, drop empty entries, treat `"-"` as no
  keywords — no per-token recognition step, no allowlist of known flags.
- `WeaponProfile.EqualityKey()` groups by a case-folded, sorted, trimmed set of `KeywordsText`
  tokens instead of by named flags, so token order and casing differences that don't change the
  actual keyword set don't split what should be one aggregated entry, while any real difference
  in keywords still does.
- `/LivePlay`'s weapon keyword chips render one chip per `KeywordsText` token, verbatim, resolved
  against `RuleGlossary` exactly as today: a match renders an interactive popover-triggering chip,
  no match renders the existing dimmed, non-interactive chip — nothing is ever silently omitted
  regardless of whether the token is a recognized mechanic.
- Repurpose the `bsdata-corpus-scan` "Full-Corpus Weapon-Keyword Token Scan" from tracking tokens
  unrecognized by the (now-removed) flag parser to tracking tokens that don't resolve against
  `RuleGlossary` — the check that's actually relevant to what a player sees on `/LivePlay` now
  that there's no flag vocabulary to be unrecognized against.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `catalogue-json-ingestion`: "Weapon Keyword Flag Parsing" requirement replaced by plain
  tokenization — no flag recognition, no alias/synonym scenarios.
- `datasheet-catalogue`: "Weapon Profile Verbatim Keyword Text" requirement no longer describes a
  relationship to typed ability flags (there are none); the verbatim list is the only
  representation.
- `attached-unit-tracker`: "Aggregate Weapon Count View" requirement's structural-equality
  definition changes from "every named ability/keyword flag" to "the weapon's normalized
  `KeywordsText` set."
- `live-play-view`: "Weapon Section Rendering" requirement's ability-tag scenario changes from
  "active ability flags" to "one chip per `KeywordsText` token."
- `bsdata-corpus-scan`: "Full-Corpus Weapon-Keyword Token Scan" requirement changes from
  "unrecognized by `WeaponProfile`'s flag vocabulary" to "unresolved against `RuleGlossary`."

## Impact

- **Code**: `WeaponProfile.cs` (remove flags, update `EqualityKey`/`WeaponProfileEqualityKey`),
  `WeaponKeywordParser.cs` (reduce to tokenization, or fold into its caller), `_UnitBlock.cshtml`
  (`WeaponAbilityTags()` replaced with a `KeywordsText`-driven render), `AttachedUnitAggregator.cs`
  (no direct flag reads found, but re-verify during implementation), `WeaponKeywordScanTests.cs` /
  `WeaponKeywordAllowlist.cs` (re-pointed at glossary resolution instead of flag recognition —
  every existing "no corresponding flag" entry becomes moot; a new pass is needed to see which
  tokens genuinely fail to resolve against the glossary).
- **Tests**: `WeaponKeywordParserTests.cs`, `WeaponProfileTests.cs`,
  `AttachedUnitAggregatorTests.cs` (one assertion reads `.Profile.LethalHits`),
  `BsdataDatasheetMapperTests.cs` (one assertion reads `.DevastatingWounds`).
- **No consumer outside `ProbHammer.Core`/`ProbHammer.Web`/tests** reads these flags today — no
  simulation engine or other project depends on them.
- **Out of scope**: building the simulation engine itself. If/when that happens, it will need its
  own typed extraction from `KeywordsText` at that time, informed by this change's shape.
