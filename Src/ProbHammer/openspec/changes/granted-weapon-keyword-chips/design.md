## Context

See proposal.md for why. Current state this design builds on:

- `WeaponKeywordGrantEffect(Selector, Keyword, ReplacesKeyword?)` is parsed from every catalogue
  record and consumed nowhere. 433 grants exist; keyword strings are inconsistent ("Lethal Hits",
  "LETHAL HITS: non-MONSTER/VEHICLE", "[ANTI-TITANIC 4+").
- `AttachedUnitAggregator.MatchedWeaponEffects` finds a contribution's weapon effects but filters
  targets to Self/AttachedUnit before `IsBearerOf` sees an ability's origin, so a Detachment-rule
  ability never reaches a weapon. `IsBearerOf` already treats Detachment-rule origin as reaching
  every component.
- `WeaponProfile.EqualityKey` compares keywords through `RuleGlossary.NormalizeToken`, which strips
  a keyword's value and collapses every Anti-X to "anti" so glossary lookup ignores them. As a
  result "Sustained Hits 1" and "Sustained Hits 2", or "Anti-Infantry 4+" and "Anti-Vehicle 2+",
  currently compare equal, contrary to `attached-unit-tracker`'s "any real difference in the
  keyword set still splits".
- Value provenance (`ValueProvenanceBuilder`, `RulePopoverRenderer.BuildProvenancePopover`) renders
  every highlighted value amber; `ProvenanceLine.Applied` is false for both a not-added effect and a
  caveat line.
- A weapon keyword chip is rendered from `Profile.KeywordsText`; a token resolving in the glossary
  is a `BuildRulePopover` trigger, an unresolved one a dimmed span.

## Goals / Non-Goals

**Goals:**
- One keyword parser decides identity and value for grants, weapon equality and chip rendering.
- Core carries every grant a weapon received or could receive; the page only renders chips.

**Non-Goals:**
- Activating a conditional grant (condition toggles).
- Changing glossary lookup: a chip still resolves its rule text the value-blind way it does today.
- Renaming `--amber` tokens.

## Decisions

### D1. A Core keyword parser, separate from glossary normalisation
New `WeaponKeyword.Parse(string)` in `Domain/Catalogue` returns the keyword's identity, its value and
a display form. Identity is the text with brackets, case and spacing folded and its trailing value
removed; an Anti keyword keeps its target ("anti-infantry"), and a qualifier such as
"non-MONSTER/VEHICLE" stays part of the identity. The value is the trailing number, dice expression
or `N+` threshold. `Compare` implements the spec's "better" rule: higher wins, Anti's threshold
lower wins, dice by expected value. The display form drops the brackets and keeps the source text's
own wording.

Alternative: extend `RuleGlossary.Normalize`. Rejected: glossary lookup must stay value-blind
("Sustained Hits 2" resolves to the Sustained Hits rule), equality must not.

### D2. Weapon equality uses the keyword parser
`WeaponProfile.EqualityKey` normalises each token to identity plus value instead of
`NormalizeToken`. This fixes the latent merge of different values today and is required for a
partial Sustained Hits 2 replacement to split its row. Glossary resolution is untouched.

### D3. Applied grants rewrite the contribution's keywords, and are recorded
In `ResolveContributionProfile`, after S/AP/D, every unconditional matched `WeaponKeywordGrantEffect`
is resolved against the contribution's current keywords by `WeaponKeywordGrantResolver.Apply`,
which returns the new keyword list and, when something changed, the added keyword plus any replaced
one. Changed grants are recorded as `KeywordGrant(Ability SourceAbility, string Keyword, string?
ReplacedKeyword)` on `WeaponContribution.KeywordGrants`, and the entry reports them distinct by
(source, keyword). Since the rewritten keywords feed `EqualityKey`, grouping splits or merges by
itself. `ReplacesKeyword` from the classification is not consulted: identity matching already
finds the replaced keyword, and both records that set it agree with it.

`MatchedWeaponEffects` widens from `WeaponCharacteristicEffect` to a generic effect type so S/AP/D,
Attacks and grants share one matching pass.

### D4. Not-added grants are recorded only when they would change something
`FindNotAppliedEffects`' grant counterpart records `NotAppliedKeywordGrant(Ability SourceAbility,
string Keyword, EffectCondition Condition)` on the contribution and, distinct by (source, keyword),
on the entry. It checks the grant against the contribution's resolved keywords with the same
resolver and skips it when native precedence would discard it, so a redundant conditional grant
never shows. It never changes keywords, so it never splits.

### D5. One Detachment-rule exception for every weapon effect
`MatchedWeaponEffects` admits a classification whose target is Self or AttachedUnit, or whose
ability's origin is Detachment Rule, exactly like `TryGetStatlineClassification`. `IsBearerOf`
then returns true for every component. This covers grants and S/AP/D/A together, removing the
"known asymmetry" note.

### D6. Chips are a view model built in `LivePlayModel`
`WeaponRowViewModel` gains `Chips`: one `KeywordChip(string Text, ChipKind Kind,
IReadOnlyList<ChipSource> Sources)` per rendered chip, `ChipKind` being Native, Granted or
NotAdded. Native and Granted come from `Profile.KeywordsText` in order, a token being Granted when
an entry-level `KeywordGrant` has its identity; NotAdded chips follow, one per distinct not-added
keyword. `ChipSource(Ability Source, string Note)` carries "replaces Sustained Hits 1" or the
condition summary plus "not added". `RulePopoverRenderer.BuildKeywordChipPopover` renders a granted
chip: the sources as a `provenance-table` (each ability a depth-1 nested trigger, as D5 of
value-provenance-popovers) above the glossary rule text when one resolves. A native chip keeps
today's markup exactly.

### D7. A conditional colour, chosen live
New tokens `--cond` (border, tick, chip border) and `--cond-tint` (fill), the latter derived with
`color-mix` like `--amber-tint`: `--cond: #3d6fa3` (muted blue), `--cond-tint: color-mix(in srgb,
var(--cond) 20%, white)`. Chosen by the user on 2026-10-04 from three candidates mocked at 667×315
on the Chaos Lord list (muted blue, violet `#7b5ea7`, dusty rose `#a6527a`): blue separates most
clearly from warm amber, reads as "pending" next to amber's "active", stays distinct from amber
under the common forms of colour blindness, and differs clearly from the green-teal `--bg3`. Dusty
rose was too close to amber's warmth. `ProvenanceLine.Applied` becomes `ProvenanceLineKind` (Applied, NotAdded,
Caveat, Fixed) so `ValueProvenance.IsConditionalOnly` (lines all NotAdded) can tell a not-added
line from a caveat line, which stays amber. The tile renders `provenance-tile provenance-cond`.

## Risks / Trade-offs

- [D2 changes grouping for weapons whose keywords differ only in value] → On captured lists this
  should only split rows that were wrongly merged; the real-corpus tests and a live check of every
  list in `data/` confirm no unexpected splits.
- [Chip clutter on heavily buffed units] → Accepted, as for value provenance: the highlight is the
  point. Conditional chips are visually lighter than applied ones.
- [Grants come from LLM classification and may be wrong] → Same mitigation as value provenance: the
  popover always names the granting ability, whose own text is one tap away.
- [Messy keyword strings fail to match a native token] → The parser folds brackets and case and is
  unit-tested against every distinct grant keyword string in the catalogue; a non-matching grant
  shows as an extra chip rather than silently replacing the wrong keyword.

## Migration Plan

Single deploy with no persisted state changes. Rollback is a revert.
