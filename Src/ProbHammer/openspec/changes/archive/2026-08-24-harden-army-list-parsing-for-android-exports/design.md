## Context

`ArmyListParser` (`src/ProbHammer.Core/Domain/Import/ArmyListParser.cs`) was designed and verified
against iOS-captured GW-app exports only (`data/gw-app-export*.txt`). Two real Android exports were
captured this session — `data/gw-android-export-custodes.txt` (app v2.4.0/139) and
`data/gw-android-export-deathguard.txt` (app v2.3.1/138), different players, both received over
WhatsApp — and both fail to import. Investigated line-by-column against the parser's actual
matching logic (not just visually): the failures reduce to two independent, mechanical causes; see
proposal.md for the summary and below for the detail behind each.

Both files were relayed via WhatsApp, so we cannot yet rule out WhatsApp's own text
handling as a contributing cause rather than (or alongside) the Android app itself — see Open
Questions.

## Goals / Non-Goals

**Goals:**
- Make both captured Android exports import successfully wherever the game data itself is
  unambiguous.
- Keep the fix mechanical and depth-independent, not a special case bolted on for one file.
- Preserve the existing "fail loudly, never guess a partition" behavior for the one case that
  remains genuinely ambiguous from text alone.

**Non-Goals:**
- Resolving the Custodian Guard partition ambiguity (see Decisions below) — deferred, not fixed
  here.
- Any change to `ArmyRosterEnricher` or BSData resolution — this change is confined to the parsing
  stage.
- Building a NewRecruit/BattleScribe JSON import path — tracked as a separate, independent change.

## Decisions

### Case-insensitive matching, scoped narrowly

Only three tokens get case-insensitive matching: the `Points` cost suffix (`NamePointsRegex`),
`ATTACHED UNITS`/`Attached Unit N`. The four top-level section headers (`CHARACTERS`, `BATTLELINE`,
`DEDICATED TRANSPORTS`, `OTHER DATASHEETS`) are left exactly as they are — both Android samples
render these identically, all-caps, to the iOS exports, so widening their match would be an
unverified guess rather than a confirmed fix. `Detachment Points` is likewise left as-is; both
Android samples already render it with the same casing as iOS.

### Bullet-continuation rule instead of a third bullet glyph

Initial hypothesis (see exploration transcript) was that Android uses a third tier of nesting with
its own (missing) bullet glyph. Checked against exact column positions in both files: an unbulleted
line's *text* always starts at the same column as the *text* of the bulleted line immediately above
it in the same list (bullet + space = 2 columns; the continuation line's leading-space count is
exactly 2 more than the bulleted line's). This holds for every one of the eight examples across both
files, at every depth (a unit's own direct weapons, and a model group's nested weapons alike),
including lists with more than one continuation line (Defiler: 4 unbulleted lines in a row, all at
the same indent, not progressively deeper).

This means the fix is not "support 3 levels of nesting" but "within `CollectBulletBlocks`, an
unbulleted line that isn't itself a new unit header extends the current list instead of ending it."
One rule, applied uniformly, rather than a depth-specific special case — confirmed necessary by
checking Blade Champion (a single-model unit whose top-level bullets — role, weapon, Enhancement —
are *all* individually bulleted, no continuation at that tier) alongside Defiler/Daemon Prince
(single-model units whose direct-weapon lists *do* use continuation lines): the continuation
behavior is specific to weapon-count lists within one list, not a blanket "every second bullet is
dropped" rule.

**Alternative considered and rejected**: matching on absolute indentation depth (0/2/4/6 spaces) as
a stand-in for tier. Rejected because `Tokenize()` already discards leading whitespace via `.Trim()`
before any classification runs, and because indentation depth is not by itself semantically stable
across the corpus (see next decision) — the *relative* signal (continuation aligns with the
preceding bulleted item's text column) is what's actually reliable, not the absolute depth.

### ForceDisposition is optional

A third, independent finding, surfaced only once tasks 1-2 were implemented and the real
`gw-android-export-deathguard.txt` was run through the parser end-to-end: its preamble goes
straight from the Detachment line (`"Virulent Vectorium (3 Detachment Points)"`) to the BattleSize
line (`"Strike Force (2000 points)"`) with no `ForceDisposition` line between them at all - every
other captured export (iOS and Android alike, including the Custodes Android file, whose
`ForceDisposition` is `"Priority Assets"`) has one. `ForceDisposition` is free text with no fixed
shape of its own, so its absence can't be detected from its own content - only from what follows:
the parser now peeks the next line and, if it already matches the `<Name> (<N> Points)` BattleSize
pattern, treats `ForceDisposition` as absent (`""`) rather than consuming that line as disposition
text. Confirmed with the user before implementing, since it's a third mechanical cause outside
this change's originally-scoped two (see Why), and could in principle be a WhatsApp-transit
artifact rather than a genuine Android-app shape - the same open question already tracked for the
bullet-continuation rule (see Risks below) applies here too, and is left open pending the same
directly-saved Android export.

### The Custodian Guard partition ambiguity is not resolved by this change

Once the continuation-line rule flattens `Custodian Guard`'s three weapon-count lines into one list
— `1x Guardian spear`, `3x Praesidium Shield`, `3x Sentinel blade`, group total 4 — the existing
partition rule (remaining, non-common weapon counts must sum exactly to the total) still fails:
1+3+3=7≠4. The only sensible reading (1 model with a spear, 3 models with a shield+blade *pair*)
requires treating `Praesidium Shield` and `Sentinel blade` as one merged 3-model sub-group, not two
independent 3-model alternatives. That is structurally identical to a case the parser's own
partition rule was deliberately designed *not* to do: Company Veteran's `"1x Master-crafted bolt
rifle"` / `"1x Master-crafted heavy bolter"` are two genuinely independent single-model
alternatives that happen to share a count, and merging same-count remaining weapons by default
would silently mis-partition that case. Text structure alone does not distinguish "coincidentally
equal count, independent alternatives" from "equal count because it's one paired loadout" — the
Death Guard file, independently, showed *no* recurrence of this ambiguity at all (every unit there
is either a single-model flat list or a model group where every weapon's count already equals the
total), so it isn't clear yet how common this shape is.

Rather than guess a merge rule from one example, this change leaves the existing "fail loudly,
name the raw wargear text" behavior in place for this case (already-existing scenario, unmodified).
A future change can revisit this once either a second real example clarifies the pattern, or the
NewRecruit JSON pipeline makes the whole class of ambiguity moot for whoever hits it.

## Risks / Trade-offs

- **[Risk]** The bullet-continuation rule was derived from two files, both relayed via WhatsApp.
  If WhatsApp itself is reformatting the text (not the Android app), a *direct* Android export
  (no WhatsApp hop) could look different, and this fix might target a shape that doesn't actually
  occur in the wild. → **Mitigation**: the user is obtaining a directly-saved Android export
  (~2026-08-27) to check against; this fix is verified against real captured files either way, and
  is a pure widening of what's accepted (nothing that parsed before stops parsing), so it carries no
  regression risk to the iOS path even if the Android hypothesis turns out incomplete.
- **[Risk]** A continuation-line's "no bullet, aligned indent" shape is, in principle, positionally
  identical to a malformed unit header that happens to be indented similarly. → **Mitigation**: the
  rule only applies *inside* an already-open bullet block (i.e. only once `CollectBulletBlocks` has
  seen at least one bulleted line for the current unit); at the top of a unit block, an unbulleted
  line still falls through to unit-header parsing exactly as today, so a genuine parse error still
  surfaces with today's diagnostic in that position.

## Migration Plan

No migration — this is a pure widening of what `ArmyListParser.Parse` accepts. Every export that
parses successfully today continues to parse identically (existing `ArmyListParserTests` and the
real iOS-captured fixtures act as the regression guard). No data or session-shape changes.

## Open Questions

- Whether the bullet-continuation pattern is native to the Android GW app or introduced by
  WhatsApp's own text handling remains open pending a directly-saved (non-WhatsApp) Android export.
  Doesn't change this change's approach (see Risks above) — purely a question of how much weight to
  put on "Android exports will keep needing this" versus "this was one transport-specific artifact."
- Same question applies to the missing `ForceDisposition` line found in
  `gw-android-export-deathguard.txt` (see "ForceDisposition is optional" above): whether a real
  Android list with no disposition selected renders no line at all, or whether this one line was
  dropped somewhere in the WhatsApp relay, isn't yet known.
