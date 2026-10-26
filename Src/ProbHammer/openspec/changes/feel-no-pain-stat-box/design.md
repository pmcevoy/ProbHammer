# Design

## Context

See proposal.md for motivation. Today:

- `Statline` holds M/T/Sv/W/Ld/Oc as `ScalarCharacteristicView` plus `InSv` as an
  `InvulnerableSaveCharacteristicView` parsed from a BSData profile column. FNP has no profile
  column; BSData expresses it as a "Feel No Pain" rule infoLink whose `append` name modifier adds the
  value, so a parsed ability is named "Feel No Pain 5+" (bundled corpus: 195 bare references, most
  with the modifier; 11 starred names across Custodes, Necrons and Astra Militarum; a few compound
  names such as "Feel No Pain 6+ 4+*").
- `AttachedUnitAggregator` applies classified Statline effects through `ApplyEffect` (scalar,
  InvulnerableSave), records not-applied ones on `AggregateStatlineEntry.NotAppliedEffects` (filtered
  to scalar/InvulnerableSave), and offers Apply toggles only for effects `Reaches` reports as reaching
  something. `FeelNoPainEffect(Value, Qualifier)` is deserialized but used by none of these.
- The catalogue holds 113 FNP effects: 61 unconditional, 52 conditional; 47 carry a qualifier. Of
  the 9 whose text never mentions mortal wounds or Psychic Attacks, only Living Fortress's qualifier
  is invented; the other 8 are real restrictions put in the qualifier field (CHARACTER models only,
  Vehicle models, "against that attack", within an objective).
- `/LivePlay` renders InSv via `RenderInvulnerableSaveTile` (grid column 3, under Sv) and its
  provenance via `ValueProvenanceBuilder.InvulnerableSave`; runs group by `Statline` record equality.

## Goals / Non-Goals

**Goals:**
- Mirror the InSv path for FNP at every layer, reusing existing view types and provenance kinds.
- Never show a qualified FNP as if it applied to every attack.

**Non-Goals:**
- Displaying qualifiers in the tile itself, or splitting a value by damage source.
- Showing a granted FNP as an ability-list entry (the `NamedAbilityGrant` vnext item).
- Resolving a compound name's footnote ("Feel No Pain 6+ 4+*"); it stays caveated.

## Decisions

**FNP is a `ScalarCharacteristicView` on `Statline`, absent as a symbolic "–".** Add
`Statline.Fnp` (init-only, like `InSv`), defaulting to a view whose original value is
`SymbolicCharacteristicValue("–")`. Reusing the scalar view gives caveat/contributing-ability/record
equality for free, and `ValueProvenance.Format` already prints a symbolic value as-is, so an absent
original reads "–" in the popover. A dedicated `FeelNoPainCharacteristicView` was considered; FNP is
one threshold with no per-side split, so InSv's extra shape buys nothing. Record equality then makes
runs split on differing FNP with no grouping change.

**The bearer's own FNP is derived at roster Build, not at parse time, and always has a source.** A model-line's own abilities only
exist at roster resolution, and the same name must work for both import pipelines. A new step after
`BuildStatlines` reads the present ability entries and parses names with a closed pattern:
`^Feel No Pain (\d)\+$` gives a resolved value; a trailing `*` or a second value gives
`ScalarCharacteristicView.Caveated(firstValue, thatAbility)`; a bare "Feel No Pain" gives nothing.
Bearer matching reuses `IsBearerOf`'s Self semantics (model-line row, or every row of the
component). Multiple matches keep the lowest threshold. The value is
`ScalarCharacteristicView.Resolved(absent, threshold, [thatAbility])`: FNP is always ability-granted
in 11e, so even a datasheet's own FNP is a highlighted, tappable tile naming its source, unlike the
plain M/T/Sv/W/Ld/OC values. Parse-time derivation in `Datasheet` was
rejected: it can't see wargear-granted model-line abilities.

**A footnoted "X+*" is dropped when a qualified X+ grant reaches the same row.** Every starred
FNP in the bundled corpus (6 Anathema Psykana datasheets via Daughters of the Abyss, Ciaphas Cain via
Psychic Blank, Nekrosor Ammentar via Nullstone Field Generator) points at a damage-source-limited FNP
of the same threshold, so the footnote means "only against those". The derivation skips such a
source, leaving the bearer's general FNP (Aleya keeps her 5+) or absent, and the qualified grant's
caveat line keeps the tile amber. A footnote with no matching qualified grant stays caveated.

**A grant compares thresholds, unlike InSv's first-source-wins guard.** `ApplyInvulnerableSaveEffect`
skips a field that already has a contributing ability; FNP can't, because its own value always has
one, which would block every grant. A `FeelNoPainEffectResolver.Merge(effect, source, current)`
beside `InvulnerableSaveEffectResolver` keeps the better threshold, replaces the contributing
ability only when it improves the value, and preserves the original (absent) value. `ApplyEffect` gains the case;
`ApplyStatlineFlagRules`' not-applied filter and `Reaches` gain `FeelNoPainEffect`, which is what
makes Living Fortress get its toggle (usage limit only, so condition key "").

**A qualified effect is filtered out of application, not dropped.** `ApplyEffect` skips a
`FeelNoPainEffect` with a non-null qualifier; the aggregator records it on the entry so the popover
can list it. It is recorded on `NotAppliedEffects` like a conditional effect, and the provenance
builder tells the two apart: a qualified effect whose state is Applied becomes a Caveat-kind line
(amber, notes: the qualifier verbatim, then "Not included in the value."), and a conditional one stays a
NotAdded line with the qualifier added to its notes. This keeps one recording channel instead of a
new list on `AggregateStatlineEntry`. Alternative rejected: treating qualified effects as ordinary
grants, which shows e.g. "5+ against mortal wounds" as a general 5+.

**The FNP tile sits under W.** Same row as InSv, `grid-column: 4`, label "FNP", value "N+", "–"
when absent but highlighted. W is the characteristic FNP protects; Sv is taken by InSv.

**The valueless entry is filtered in `LivePlayModel`.** Before statline blocks and component spans
consume `view.Abilities`, drop entries named exactly "Feel No Pain" (case-insensitive, trimmed)
whose component has a run that renders an FNP tile (FNP present or a non-null FNP provenance).
Filtering in the aggregator was rejected: the decision depends on whether a tile renders, which is
a view concern.

**Living Fortress is corrected at the source.** Set its qualifier to null in
`tools/AbilityPipeline/data/classifications.json` with a `ReviewerNote`, then `export`. The other 8
misplaced qualifiers stay: they are real restrictions, and the caveat line is the safe display.

## Risks / Trade-offs

- [A starred FNP whose qualified grant isn't matched (Nullstone Field Generator's Keyword-targeted
  aura) still shows a caveated value pointing at the generic FNP template] → Honest but unhelpful.
- [Real qualifiers sit in the same field as model-subset restrictions, so some caveat notes read
  oddly beside a damage-source one] → The note shows the qualifier verbatim with no lead-in word, so
  it reads correctly either way; prompt v3's FNP-qualifier item fixes the data.
- [An unconditional, unqualified grant on a unit whose own value is already better] → No change,
  no highlight, matching InSv's "worse grant leaves the value unflagged".
- [Every FNP tile is amber, so amber on FNP no longer means "changed from the datasheet"] →
  Accepted (user decision): FNP is always ability-granted, and the popover says which ability.
- [The extra tile adds height to every FNP unit's run] → Same row as InSv, so no new row when a run
  already has an InSv; a run with only FNP gains the row InSv would have used.
