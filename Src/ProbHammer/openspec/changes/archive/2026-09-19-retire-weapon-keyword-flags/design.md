## Context

See `proposal.md` - Why for motivation. Relevant current-state facts gathered during exploration,
not restated there:

- The only live runtime consumer of `WeaponProfile`'s typed ability flags is
  `WeaponProfile.EqualityKey()` → `WeaponProfileEqualityKey`, read by
  `AttachedUnitAggregator.BuildWeapons` as a `Dictionary` key to group contributions into one
  aggregated entry. `EqualityKey()` already appends `string.Join("", KeywordsText)` to the key
  *in addition to* every named flag — so today, two profiles with any difference in keyword text
  already produce different keys regardless of whether that difference trips a recognized flag.
  This is confirmed by an existing test,
  `WeaponProfileTests.EqualityKey_distinguishes_weapons_that_differ_only_in_verbatim_keyword_text`,
  which asserts exactly this using an *unrecognized* token (`"Cleave"` vs no keyword) with no flag
  involved at all. The flags are already redundant for this purpose.
- `_UnitBlock.cshtml`'s `WeaponAbilityTags()` builds the rendered weapon-tag chip list purely from
  the typed flags (`if (w.Pistol) tags.Add("PISTOL"); ...`), never reading `KeywordsText`. This
  contradicts `datasheet-catalogue`'s existing "Weapon Profile Verbatim Keyword Text" requirement,
  which already specifies rendering must read the verbatim record. The requirement was speced
  correctly; the implementation never matched it. Two real, user-hit bugs this session were
  exactly this: BSData renaming `Pistol` to `Close-Quarters`/`CLOSE-QUARTERS` (confirmed via
  BSData's own `sharedRule` alias) and a `Cleave N` keyword with no flag at all — both vanished
  from `/LivePlay` with no unresolved/dimmed chip, because the flags were never set and rendering
  never looked past them.
- `RuleGlossary.TryResolve`'s `Normalize` pipeline (lowercase → strip trailing value/threshold
  suffix → collapse an `anti[-:\s]` prefix → strip remaining punctuation/whitespace) already
  tolerates arbitrary source casing, hyphenation, and appended values. It's confirmed
  case/casing-robust enough that raw `KeywordsText` tokens (`"Anti-infantry 4+"`,
  `"CLOSE-QUARTERS"`, `"Close-Quarters"`, `"CLEAVE 1"`) resolve identically well as today's
  hand-canonicalized `WeaponAbilityTags()` output (`"ANTI-INFANTRY 4+"`, `"PISTOL"`) — in fact
  better, since `RuleGlossary`'s own doc comment already names `"Cleave"` and `"Close-quarters"`
  as real BSData entries with no declared `Alias`, self-resolving via their bare `Name`.
- No consumer of these flags exists outside `ProbHammer.Core`, `ProbHammer.Web`, and their test
  projects — confirmed by a full-repo search. There is no simulation engine (the eventual reason
  these flags were introduced) and none is being built now.
- Non-runtime consumers that DO read individual flags today, found by exploration, all in test
  code: `WeaponKeywordParserTests.cs` (parser-focused, being replaced), one assertion each in
  `AttachedUnitAggregatorTests.cs` (`.Profile.LethalHits`, used only to distinguish which of two
  otherwise-identical grouped copies is which) and `BsdataDatasheetMapperTests.cs`
  (`.DevastatingWounds`), and the `bsdata-corpus-scan` capability's `WeaponKeywordScanTests.cs` /
  `WeaponKeywordAllowlist.cs`, built specifically around "token unrecognized by the flag parser."

## Goals / Non-Goals

**Goals:**
- Make `KeywordsText` the sole representation of a weapon's ability keywords - no parallel typed
  flags to keep in sync or to silently omit from rendering when a new BSData keyword appears.
- Preserve every existing, real grouping/rendering behavior (two structurally-identical weapons
  still merge; two weapons differing by a real keyword still don't) without hand-maintaining a
  flag per keyword.
- Keep the `bsdata-corpus-scan` regression check meaningful under the new model: it should track
  the thing that actually affects what a player sees (does this token resolve against
  `RuleGlossary`?), not a vocabulary that no longer exists.

**Non-Goals:**
- Building the simulation engine, or any typed extraction of keyword values (dice counts,
  thresholds) for damage math. If that's built later, it starts from `KeywordsText` at that time.
- Changing `RuleGlossary`, `RuleGlossary.Normalize`, or the popover/rendering mechanism itself -
  this change only changes what feeds into the existing `glossary.TryResolve(tag)` call already
  used by both weapon-tag rendering sites in `_UnitBlock.cshtml`.
- Deduplicating identical tokens within one weapon's own `KeywordsText` (not currently a real
  corpus shape; out of scope unless it turns up).

## Decisions

### `WeaponProfile` loses every typed ability flag and the `Anti` dictionary
`Torrent`, `Blast`, `Melta`, `RapidFire`, `SustainedHits`, `LethalHits`, `DevastatingWounds`,
`TwinLinked`, `IndirectFire`, `Pistol`, `IgnoresCover`, `Assault`, `Anti` are removed from
`WeaponProfile` and from `WeaponProfileEqualityKey`. `KeywordsText` (already present) is the only
representation left. **Alternative considered**: keep the flags for possible future simulation
use, just stop rendering from them. Rejected — a field nothing reads or writes accurately (since
the parser recognizing it would also be removed) is dead weight and a future footgun (a flag that
silently reads `false` forever looks like a real "no" answer, not "we stopped tracking this").

### `WeaponKeywordParser` shrinks to tokenization only
Its recognized-vocabulary `TryRecognize` switch/regex bank and the `Apply`/`UnrecognizedTokens`
split it exists to serve are removed. What's left — split `Keywords` on `,`, trim, drop empty
entries, treat `"-"`/blank as empty — is small enough that either a slimmed-down
`WeaponKeywordParser` (kept as its own type since two independent import pipelines,
`BsdataDatasheetMapper` and `BattleScribeRosterMapper`, both call it) or an inlined one-liner in
each caller would work. **Decision**: keep it as its own small static method — two call sites
already share it, and a named method documents "this is where `Keywords` text becomes a token
list" better than a duplicated one-liner in each mapper.

### `EqualityKey()` groups by a normalized `KeywordsText` set, not by named flags
Replace the flag list in `WeaponProfileEqualityKey` with one normalized representation of
`KeywordsText`: case-fold and trim each token, deduplicate, then **sort the resulting tokens into
one canonical order and only then join them into one comparable string** — the sort must happen
before the join, not after, otherwise the join is just as order-sensitive as today's
`string.Join("", KeywordsText)` and fixes nothing. This mirrors `NormaliseAnti`'s existing "sort a
copy, join into one comparable string" pattern already used for the `Anti` dictionary.
**Why normalize instead of reusing today's raw `string.Join("", KeywordsText)`**: today's join is positionally ordered and
case-sensitive, so two profiles carrying the same real keywords in a different order, or differing
only by a casing quirk in the source data, would incorrectly count as different profiles and fail
to merge — a real risk given BSData's own inconsistent casing (confirmed this session: Orks mixes
`"CLOSE-QUARTERS"` and `"Close-Quarters"` for what BSData itself declares an identical rule).
Normalizing removes that false-split risk while still splitting on any *real* keyword difference,
preserving every existing grouping test's intent
(`WeaponProfileTests.EqualityKey_distinguishes_weapons_that_differ_only_in_verbatim_keyword_text`/
`_treats_identical_verbatim_keyword_text_as_equal`, `AttachedUnitAggregatorTests`' "Weapons
differing only by a targeting/eligibility keyword are not combined").
**Alternative considered**: normalize by resolving each token through `RuleGlossary` first (so two
different spellings of the *same real rule* — e.g. `"Pistol"` and `"Close-Quarters"` — would merge
as equal). Rejected for this change: `EqualityKey()` is computed on `WeaponProfile` alone, which
has no reference to a `RuleGlossary`/`BsdataClosure`, and threading one through would be a much
larger structural change than this proposal's scope; plain per-weapon-profile text normalization
already fixes the concrete casing-split risk found, and glossary-aware grouping can be revisited
separately if a real corpus case ever needs it.

### `/LivePlay` renders one chip per `KeywordsText` token, resolved against `RuleGlossary` exactly as today
`_UnitBlock.cshtml`'s two weapon-tag render sites (Ranged and Melee tables) already do
`glossary.TryResolve(tag)` per tag and branch on resolved/unresolved styling
(`weapon-tag-resolved`/`weapon-tag-unresolved`) - that loop stays unchanged. What changes is only
where `tags` comes from: instead of `WeaponAbilityTags(weapon)` reconstructing canonical strings
from flags, it becomes `weapon.KeywordsText` directly (or a thin wrapper if any list-shape
adaptation is still needed - e.g. its current `List<string>` return type). This is the direct fix
for the "silently vanishes" bug class: an unresolved token now always renders (dimmed, per the
existing, already-correct "A keyword chip with no matching glossary entry is not interactive"
scenario), it just never disappears outright.

### `bsdata-corpus-scan`'s weapon-keyword scan repoints at `RuleGlossary` resolution
`WeaponKeywordScanTests.cs` currently walks the corpus calling
`WeaponKeywordParser.UnrecognizedTokens(keywordsText)` (a method being removed) and checks results
against `WeaponKeywordAllowlist.cs`. It's repointed to tokenize (via the slimmed parser) and check
each token against a `RuleGlossary` built from the same catalogue file's own closure (the same
`BsdataClosure`/`RuleGlossary.Build` machinery `WeaponKeywordScanTests` would need to construct
per file, analogous to how it already builds `WalkContext` per file today), recording a token with
no `TryResolve` match. **Every existing "no corresponding flag" allowlist entry becomes
meaningless** (there's no flag vocabulary left to be unrecognized against) and is deleted; a fresh
run against the real corpus is needed post-implementation to seed whatever tokens genuinely don't
resolve against the glossary — expected to be a materially different, likely much shorter list,
since `RuleGlossary` already recognizes many mechanics (`Hazardous`, `Precision`, `Cleave`,
`Psychic`, `Extra Attacks`, etc. all have real BSData rule definitions) that
`WeaponKeywordParser`'s hand-maintained flag vocabulary never modeled at all.

## Risks / Trade-offs

- **[Risk]** Removing the flags is a real breaking change to `WeaponProfile`'s public shape within
  `ProbHammer.Core` → **Mitigation**: confirmed by full-repo search that no consumer exists outside
  this repo's own two projects and their tests; every affected call site is enumerated in
  `proposal.md` - Impact and `tasks.md`.
- **[Risk]** A weapon whose keyword text differs only by trailing/leading whitespace or
  double-spacing (a source-data quality issue, not seen yet in the corpus) could still produce a
  spurious equality split even after normalization, if the normalization doesn't collapse internal
  whitespace the way `RuleGlossary.Normalize` does → **Mitigation**: normalize each token the same
  way `RuleGlossary.Normalize` does (or reuse it directly) rather than a narrower ad hoc
  trim/lowercase, so the two mechanisms can't drift into disagreeing about what counts as "the same
  keyword."
- **[Trade-off]** Corpus tokens that resolve against `RuleGlossary` but whose resolved rule text
  describes a mechanic this app still doesn't model behaviorally (e.g. `Cleave`'s actual damage
  effect) will now render as a normal, clickable, resolved chip - indistinguishable from a fully
  "supported" keyword like `Torrent` used to look. This is an intentional, accepted trade-off: the
  chip has never asserted "this mechanic affects any calculation," only "this is the weapon's own
  keyword text, click to read the rule" - true both before and after this change - and the
  previous illusion of a `Torrent`-shaped chip meaning "fully modeled" was never accurate anyway
  (this app performs no damage math today regardless).

## Migration Plan

No data migration - `WeaponProfile`/`WeaponProfileEqualityKey` are in-memory-only domain types with
no persisted or serialized form (confirmed by `datasheet-catalogue`: catalogue data is re-resolved
from BSData JSON on demand, never cached to disk in the app's own shape). Deploy as one ordinary
code change; no rollback concerns beyond reverting the commit.

## Open Questions

(none - see Decisions for the alternatives considered and rejected)
