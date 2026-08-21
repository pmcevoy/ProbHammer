## Context

See `proposal.md` for motivation. Relevant existing shapes this design builds on:

- `BsdataClosure` (`Domain/Catalogue/Bsdata/BsdataClosure.cs`) already holds every catalogue file
  in a faction's transitive import closure (`Files: IReadOnlyList<(string FileName, BsCatalogue
  Catalogue)>`) plus the one base rules file every closure implicitly depends on
  (`GameSystem: BsCatalogue?`), resolved once by `BsdataClosureResolver.Resolve`.
- `ResolvedBsdataCatalogue` (`ResolvedBsdataCatalogue.cs`) bundles a `BsdataClosure` with the
  id/group/profile indices built over it, and is the per-starting-file unit `BsdataCatalogueCache`
  (`ProbHammer.Web`) caches — built once per faction, reused across every request/session that
  resolves against it.
- Confirmed against the live BSData clone: the base game-system file (`Warhammer 40,000.json`,
  reachable as `closure.GameSystem`) has a top-level `sharedRules` array (46 entries today, each
  `{name, description, alias[], id}`) — this is where universal special-rule text (Lethal Hits,
  Devastating Wounds, etc.) lives. A faction/library catalogue file instead has a top-level `rules`
  array of the same per-entry shape when it defines its own rules (confirmed: `Library - Astartes
  Heresy Legends.json` defines "Templar Vows" and "Oath of Moment" this way) — a plain faction file
  with no rules of its own (confirmed: `Imperium - Black Templars.json`, `Imperium - Space
  Marines.json`) has neither key present at all, which `System.Text.Json` already deserializes to
  an empty/default collection under this codebase's existing pattern for optional BSData fields.
- `WeaponAbilityTags()` (`_UnitBlock.cshtml`) is a `@functions` block that inspects a
  `WeaponProfile`'s boolean/int ability flags (`Torrent`, `Pistol`, `SustainedHits`, etc.) and
  returns hardcoded ALL-CAPS literal strings (e.g. `"TORRENT"`, `"SUSTAINED HITS 1"`) — these
  literals already match real `sharedRules[].alias` values almost exactly.
- `Ability.Text` exists on the domain model today but is rendered in exactly one place
  (`insv-caveat-text` in `_UnitBlock.cshtml`), with no `white-space` CSS handling.

## Goals / Non-Goals

**Goals:**
- Resolve BSData's existing rule-text sources into a queryable glossary, reusing the existing
  closure-resolution machinery rather than adding new traversal logic.
- Let a player reach any ability's or weapon keyword's full rules text, and any cross-reference
  inside that text, without leaving `/LivePlay` or losing their place.

**Non-Goals:**
- No restructuring of `WeaponProfile`'s existing boolean ability-flag model into a richer keyword
  type. A keyword chip resolves by looking up the same literal tag string `WeaponAbilityTags()`
  already produces against the new glossary — it does not require `WeaponProfile` itself to know
  anything about rule text. `WeaponProfile.EqualityKey()`/weapon-aggregation semantics are
  extensively exercised elsewhere (`AttachedUnitAggregator`) and are deliberately left untouched.
- No ability-text interpretation or behavior derivation. This remains a permanent exclusion (see
  `datasheet-catalogue`'s Deliberate Omissions) — the glossary and its cross-reference resolution
  produce display text and links only, never a rule's mechanical effect.
- No client-side JSON API, fetch layer, or new HTTP endpoint. Every page in this app is
  server-rendered Razor with session-backed state and no REST surface; this change keeps that
  shape — glossary lookups happen server-side per request, and matched text is emitted as part of
  the existing Razor render.
- No corpus-wide or cross-faction glossary. Like every other BSData lookup in this codebase, the
  glossary is scoped to one closure (one starting file's transitive imports) — this is not a
  limitation in practice, since an ability or weapon's own text only ever appears on a datasheet
  already resolved from that same closure.

## Decisions

### New JSON model fields, not a new reader
Add `Rules` and `SharedRules` (`IReadOnlyList<BsRuleJson>`, each element `{Name, Description,
Alias}`) to the existing `BsCatalogue` JSON model (alongside `SharedSelectionEntries`,
`SharedProfiles`, etc. in `Domain/Catalogue/Bsdata/Json/`), rather than a separate reader or a
second pass over the raw JSON. `BsdataCatalogueReader.Read` already deserializes every field this
loader needs and silently drops everything else (`constraints`/`modifiers`/`conditionGroups` etc.)
— `rules`/`sharedRules` become two more fields it now keeps, following that same established
shape.

### `RuleGlossary` built from the existing `BsdataClosure`, not a new resolver
A new `RuleDefinition(Name, Aliases, Text)` value type and a `RuleGlossary` built by a single
static `RuleGlossary.Build(BsdataClosure closure)`: iterate `closure.Files` in their existing
resolution order (starting file nearest, imports farther) collecting each file's own `Rules`, then
`closure.GameSystem?.SharedRules` for the universal set — first occurrence per normalized key
(see "One normalized-key index, built from both Name and Alias" below) wins on a collision,
mirroring `BsdataNameResolver`'s existing "local beats imported" and `BsdataDatasheetMapper`'s
existing "first occurrence wins" dedup conventions rather than inventing a new precedence rule.
No real collision between a faction rule and a universal rule has been observed in the corpus
explored so far; this ordering is a consistent default, not a fix for a confirmed problem.

`RuleGlossary` exposes a non-throwing `TryResolve(string nameOrAlias) -> RuleDefinition?`, mirroring
`Datasheet.TryGetStatline`/`TryResolveWeaponProfile`'s existing non-throwing-lookup convention for
"this name might not exist and that's not exceptional" cases.

### Glossary is built alongside `ResolvedBsdataCatalogue`, cached at the same tier
`ResolvedBsdataCatalogue.Build` gains one more step — `RuleGlossary.Build(closure)` — and exposes
the result as a new `Glossary` property. No new cache: `RuleGlossary` is derived purely from the
`BsdataClosure` `ResolvedBsdataCatalogue.Build` already resolves, so it rides the existing
`BsdataCatalogueCache` (keyed by starting file name, built at most once per faction) for free.

### Bracket-token extraction is a separate, pure function from resolution
A `RuleTextTokenizer.ExtractBracketTokens(text) -> IReadOnlyList<string>` extracts every
`[...]`-delimited substring via a simple non-nested-bracket pattern (no real example of a nested
`[[...]]` has been observed). It has no dependency on `RuleGlossary` — resolving an extracted token
against a specific glossary is a separate step (`RuleGlossary.TryResolve`), so extraction stays
testable independent of any specific closure's content, and the "not a reference signal"
requirement for `*`/`**`/`^^` is enforced simply by this function never looking at those characters
at all, rather than needing to explicitly exclude them — necessary because a bracket token can sit
inside an open italic span carried over from an earlier `***` marker (see "Emphasis markup: a
three-way mapping, not a link signal" below) with no `*`/`**`/`^^` character adjacent to the
bracket itself.

Extraction additionally normalizes the captured substring before returning it: `*`/`^` characters
found *inside* the brackets are stripped (confirmed real shape: `[^^Lethal Hits^^]`, a rule
referenced by its display Name wrapped in small-caps markup inside the brackets themselves — the
reverse of markup wrapping a bracket from the outside, which the "not a reference signal"
requirement already covers), and known Unicode whitespace/hyphen variants (U+00A0 no-break space,
U+2011 non-breaking hyphen, U+2013 en dash) are replaced with their plain-ASCII equivalent
(confirmed real shape: a no-break space between "DEVASTATING" and "WOUNDS" in one Astra Militarum
Library ability; a non-breaking hyphen in several `ANTI‑VEHICLE`-style references). This stays a
pure, glossary-independent step — it corrects known encoding-level authoring noise, never
interprets or discards semantic content (a value or category token inside the brackets is left
untouched here; see "One normalized-key index, built from both Name and Alias" below for where
that's handled). Found only once the full-corpus scan (task 3.4) was actually run against the
live clone — see PROGRESS.md.

### One normalized-key index, built from both Name and Alias
The first real corpus-scan run found 1,964 unresolved bracket-token occurrences across 71 distinct
tokens — the overwhelming majority a single systematic shape: a generic mechanic's sharedRules
entry carries a bare `Name`/`Alias` (e.g. `Sustained Hits`/`SUSTAINED HITS`, `Anti`/`ANTI`), but
real ability/weapon text always references it with a value or target category appended
(`SUSTAINED HITS 1`, `ANTI-VEHICLE 3+`) — a shape BSData's own rule text documents explicitly
(Sustained Hits' own description: `"This ability always takes the form **[SUSTAINED HITS X]**"`).
An early design here (an exact-match stage plus a second, `Alias`-only fallback stage) treated a
rule with no `Alias` at all as a confirmed dead end and left `Cleave`/`Close-Quarters` allowlisted
— but both rules' own description text self-references itself using that exact same convention
(`Cleave`: `"...**[CLEAVE X]**..."`; `Close-quarters`: `"...**[CLOSE-QUARTERS]**..."`), which is
direct evidence the missing `alias` array is a BSData data-entry gap, not a sign the rule has no
bracket-reference convention. Restricting the fallback to `Alias` baked that wrong assumption in
structurally. Once flagged, the fix is a simplification, not an added special case: index every
`RuleDefinition` by its normalized `Name` *and* every normalized `Alias`, both feeding the same
one dictionary, and resolve with a single lookup — no separate exact/fallback stages at all.

`RuleGlossary.Normalize(string) -> string` is a small, ordered, four-step pipeline (an
`AllowlistCheck`-adjacent design: a short, explicit list of transformations rather than one dense
regex, so a future BSData shape needs one more list entry, not new branching logic):

1. Lowercase the whole string.
2. Strip a trailing value/threshold/dice/placeholder suffix (`x`, a digit run, a digit run with a
   trailing `+`, or `d` + digits with an optional `+`-suffix, e.g. `d3`) preceded by whitespace —
   must run first, while that whitespace separator is still present.
3. Collapse a string starting with `anti` followed immediately by a space, hyphen, or colon to the
   bare string `anti`, discarding everything after it (see "The Anti carve-out" below) — must run
   before step 4 destroys that same boundary character, since without it there'd be nothing left
   to check.
4. Strip every remaining character that isn't a lowercase letter or digit — this is also what
   makes a casing/hyphenation variant like `Twin-Linked` collapse to the same key as
   `TWIN-LINKED` with no separate casing rule needed.

Both sides of every comparison go through the identical pipeline: a `RuleDefinition`'s `Name` and
`Aliases` are normalized this way when the glossary is built, and a query string (a bracket token
or a `WeaponAbilityTags()` literal) is normalized the same way before lookup. There is no longer a
distinct case-sensitive "exact" stage — normalization already makes a literal ALL-CAPS
`WeaponAbilityTags()` chip and a title-case ability-text reference land on the same key, so nothing
is lost by not special-casing the already-correct-literal case.

**Why this only removes a recognized trailing value, never an arbitrary suffix.** A real one-off
anomaly this correctly does *not* paper over: Adeptus Custodes' "No Foe Shall Stand" ability text
has a bracket closed one word too late (`[IGNORES COVER abilities]` instead of
`[IGNORES COVER] abilities`) — `"abilities"` isn't a recognized value token, so step 2 leaves it
un-stripped and it correctly stays unresolved rather than incidentally "fixed" by a looser rule.
It remains an individually-documented corpus-scan allowlist entry, the same treatment as the
pre-existing Aeldari Archon anomaly.

**The Anti carve-out.** Unlike every other generic mechanic, Anti's own sharedRules entry documents
a *two-part* referenced form (`**[ANTI-X Y+]**` — a target category *and* a value, not just a
value), confirmed further by a negated-target variant (`[ANTI: non‑MONSTER/VEHICLE 5+]`, a shared
"Blood Boil" ability reached by every chapter that imports Space Marines). Trailing-suffix removal
alone cannot reduce `ANTI-VEHICLE 3+` to `anti` — it would leave `anti-vehicle`. The tempting
general fix, "cut the token at its first hyphen, not just its first digit," was rejected:
`Twin-Linked`'s hyphen is part of the rule's own name, not a category separator, so a blanket
hyphen-cut would truncate it to `twin` — which happens to still match today (the same cut applied
symmetrically to the alias `TWIN-LINKED` also degrades to `twin`) but is not a rule to build on,
since a future alias sharing that same truncated prefix would silently resolve to the wrong rule
instead of correctly failing to resolve at all. Anti gets one narrow, explicit exception instead —
step 3 above. The boundary check (a space/hyphen/colon must immediately follow `anti`) is not
optional: without it, `"Antimatter Drive"` would also collapse to `anti` and wrongly resolve
(caught by a unit test written against exactly this input, not observed in the real corpus but
cheap to guard against given how coarse `^anti` alone would otherwise be). No other mechanic in
the confirmed corpus needs this category-infix shape.

**Why Name is now included, and what that costs.** Indexing by `Name` in addition to `Alias`
means `Cleave`/`Close-Quarters` resolve correctly (via their own normalized `Name`, since neither
has an `Alias` to index) rather than staying permanently allowlisted. The tradeoff: the earlier
design's "canary" property — noticing via a stale allowlist entry if BSData ever adds the missing
alias — no longer applies to these two specifically, since they already resolve correctly by
another path. That's an acceptable trade: a working popover today beats a diagnostic signal for a
problem that's already fixed.

Confirmed by re-running the corpus scan against the live clone after implementation (task 3.5):
the unresolved-token count dropped from 1,964 to **2** — only `IGNORES COVER abilities` (confirmed
one-off bracket-placement typo) and `whichever applies` (confirmed non-reference bracketed prose —
see the next risk bullet) remain, both genuine BSData authoring anomalies with no general fix that
wouldn't risk masking a real future mismatch. See PROGRESS.md for the implementation summary.

**A known limitation, stated plainly**: the corpus scan (and this normalization generally) only
verifies that a token *resolves* to *some* `RuleDefinition`, not that it resolves to the *correct*
one. A normalized-key collision between two genuinely different rules would silently return the
wrong text rather than failing loudly. No such collision has been found in the real corpus
explored so far, and the corpus scan re-run after implementation continues to guard against one
being introduced by a future BSData update (a new colliding alias would still just resolve to
*some* definition, so this residual risk is accepted, not eliminated, consistent with this being a
personal/single-user reference tool rather than a certified rules engine).

### Emphasis markup: a three-way mapping, not a link signal
Confirmed against real BSData text (`sharedRules`' "Lethal Hits" entry) that source emphasis markup
is genuine, deliberately nested CommonMark-style emphasis, not a bespoke convention: single
`*italic*`, double `**bold**`, and triple `***bold+italic***` (which can close its bold and italic
spans at different points — e.g. `***Designer's Note:**` closes bold after two words while the
italic span it opened continues to a single closing `*` at the end of the paragraph) all appear and
nest exactly as standard Markdown would parse them. `^^small-caps^^` is a fourth, BSData-specific
marker layered on top of that — confirmed by checking how NewRecruit and Wahapedia actually render
`^^`-wrapped text — and can combine with `**`/`*` on the same span (confirmed real shape:
`^^**Fabius Bile**^^`, bold and small-caps together). Rendering this needs a real small emphasis
parser (tracking open/close pairs, including the triple-marker split-close case) rather than a
sequence of naive string replacements: replacing every `**` first and then every remaining `*`
would break a `***X**...*` span's italic portion once its bold half has already been replaced out
from under it. Output is inline `<b>`/`<i>`/a small-caps `<span>` (exact class/CSS left to
implementation), never an interactive element — mirrors `rules-glossary`'s "Non-Bracket Markup Is
Not A Reference Signal" requirement, which this rendering step must not violate regardless of how
deeply a `[BRACKET]` token sits inside nested emphasis.

### Popover mechanism: native Popover API, CSS anchor positioning, `@supports` fallback
Settled in prior discussion, restated here as the binding decision: `popover="auto"` (not
`<dialog>`), because nested popovers need the Popover API's built-in ancestor-aware light-dismiss
stacking — reproducing that with `<dialog>` would mean hand-rolled backdrop-click handling and
manual stack bookkeeping. Positioning uses native CSS anchor positioning, wrapped in
`@supports(anchor-name: --x)` with a simple non-anchored fallback rule so a popover still opens and
is dismissable on a browser without support — per the new "Popover Remains Functional Without
Anchor-Positioning Support" requirement. This is a personal/single-user tool, so leaning on current
native CSS instead of a JS positioning library is an accepted trade-off; no polyfill is planned
unless real testing on the target device shows one is needed.

**Implementation detail settled by task 5.5's real testing**: each trigger/popover pair uses the
CSS `anchor-name`/`position-anchor` custom-property pair (a unique inline `--a{id}` value per
pair), not the newer HTML `anchor="..."` IDREF attribute this decision's original phrasing also
named as an option — the target browser tested (Firefox 154) supports the former's underlying
properties (confirmed via `CSS.supports()`) but not the HTML attribute itself (confirmed via
`'anchor' in document.createElement('div')` → `false`). See the Risks section for what "supports"
turned out to mean in practice.

Nesting does **not** depend on the anchor-positioning mechanism at all, contrary to this decision's
original framing (which assumed the `anchor` HTML attribute would double as the ancestor
registration). The actual Popover-API ancestor relationship nested popovers need for their
parent-stays-open / tap-between-closes-only-the-child / tap-outside-both-closes-both behavior comes
from `popovertarget` itself — the element that declaratively invokes a popover is automatically
registered as its ancestor for light-dismiss purposes, independent of how (or whether) that
popover ends up positioned. This is good news for the "positioning doesn't fully work yet"
finding above: nested-popover *behavior* (section 6) isn't at risk just because placement is
still imperfect in this browser.

### Weapon-keyword chip rendering reuses `WeaponAbilityTags()`'s existing output
`WeaponAbilityTags()` keeps returning the same list of literal tag strings it already does; the
Razor template changes from joining them into one `[@string.Join(", ", tags)]` span to rendering
one chip per tag, each looked up against the page's `RuleGlossary` (via `TryResolve`) to decide
whether it becomes a popover trigger or a plain inert chip (per the new "A keyword chip with no
matching glossary entry is not interactive" requirement). Exact markup/id-naming for wiring a chip
or ability-name-line to its popover element is left to implementation (`tasks.md`), not pinned
here — it doesn't change any spec-level behavior.

### Newline handling scoped to new popover text only
The new popover-text container gets `white-space: pre-line` so real line breaks in `RuleDefinition
.Text`/`Ability.Text` render as visual line breaks. The existing `insv-caveat-text` box is left
unchanged — it has never shown a real multi-paragraph value in practice, and touching it isn't
needed to satisfy this change's requirements, consistent with not refactoring code the task at hand
doesn't require.

### A self-referencing rule cannot recurse into its own popover again
Not anticipated by any earlier artifact in this change — found only once task 6.1 was actually
implemented and tested against real weapons. Several real BSData generic-mechanic rules reference
their own name inside their own description text, the same self-documenting convention already
relied on elsewhere in this change (see "One normalized-key index, built from both Name and Alias"):
confirmed real in `Sustained Hits` (`"...**[SUSTAINED HITS X]**..."`), `Anti`
(`"...**[ANTI-X Y+]**..."`), and `Cleave` (`"...**[CLEAVE X]**..."`). An unguarded first
implementation of the nested-reference renderer recursed into a resolved rule's own text
unconditionally, so opening any of these rules' popovers (e.g. tapping a real `SUSTAINED HITS 1`
weapon-keyword chip) attempted to render its own text inside its own popover inside its own popover
— infinitely — and reliably stack-overflowed the process. Confirmed live during implementation, not
theoretical.

Fixed with an ancestor-chain guard: `_UnitBlock.cshtml`'s `BuildRulePopover`/`RenderNestedReference`
thread a `shownRuleNames` set of every `RuleDefinition.Name` already open somewhere in the current
popover's own ancestor chain (seeded with the rule's own name for a top-level weapon-tag chip,
empty for a top-level ability trigger, since an ability isn't itself a `RuleDefinition`). A bracket
token that resolves back to a name already in that set renders as plain text instead of recursing —
the same graceful degradation already established for a genuinely unresolved token, and for the
same underlying reason: no correct, useful, non-recursive interactive behavior exists for "this
text is already the popover you're reading," so falling back to inert text is strictly better than
either infinite recursion or a merely-redundant extra tap target that would reopen the same content.
This also generalizes correctly to a longer cycle (A references B, B references A), not just direct
self-reference, since the guard checks the *whole* ancestor chain, not just the immediate parent —
no such cycle has been confirmed in the real corpus, but nothing about the fix assumes otherwise.

## Risks / Trade-offs

- **CSS anchor positioning support is uncertain on the actual target device** (tablet/phone,
  possibly Firefox Mobile) → confirmed real by task 5.5's manual testing (Firefox 154, desktop):
  positioning does not visibly apply, and it's a more specific gap than originally guessed - not
  "the feature is absent" but "the implementation is present-but-incomplete." `CSS.supports()`
  reports `anchor-name`/`position-anchor`/`position-area` all as supported (the HTML `anchor`
  attribute alone is the part genuinely unsupported), the authored `position-area` rule is
  confirmed matched in the CSSOM, yet `getComputedStyle` still resolves to `top: 0; left: 0`. This
  is exactly why `anchor-name`/`position-anchor` was chosen over the HTML `anchor` attribute for
  the actual implementation (see "Popover mechanism" below) - it's the pairing this specific
  browser gets furthest with, even though neither fully works yet. Mitigated exactly as planned:
  the `@supports` block and native `popover="auto"` semantics mean a popover still opens, shows
  the right text, and dismisses on an outside tap regardless - confirmed live - so only placement
  quality degrades, never functionality. No polyfill pursued, per this decision's own stated
  stance ("no polyfill is planned unless real testing... shows one is needed" - real testing found
  a genuine gap, but not one a small JS shim could confidently paper over without knowing which of
  several still-shifting spec details this particular browser build gets wrong).
- **Regex-based bracket extraction could mis-fire on a stray literal `[` that isn't a real
  cross-reference** — confirmed real: Blood Angels' "Visions of Heresy" ability text ends
  `"...re-roll the Charge roll made for this unit [whichever applies]"`, ordinary bracketed prose,
  not a reference at all → mitigated exactly as this risk anticipated before it was confirmed: an
  unresolved token renders as plain, non-interactive text, so this specific false-positive
  extraction degrades to "this text isn't clickable," never a broken render.
- **A normalized-key collision in `RuleGlossary` (see "One normalized-key index, built from both
  Name and Alias") could silently resolve a token to the wrong `RuleDefinition`** rather than
  failing to resolve at all — no such collision has been found in the real corpus, and the corpus
  scan re-run after implementation continues to guard against introducing one, but it only
  verifies that a token resolves to *some* definition, not that it's the *correct* one → accepted
  as a residual risk given this is a personal/single-user reference tool, not a certified rules
  engine.
- **`sharedRules`/`rules` are optional/inconsistently-present fields across the corpus** (confirmed:
  present with content on the game-system file and on at least one library file, absent entirely on
  at least two faction files checked) → mitigated by the same default-to-empty deserialization this
  loader already relies on for every other optional BSData field.
- **The Popover API's nested-ancestor behavior is new to this codebase** (no prior use of
  `popover`/`<dialog>` anywhere in `/LivePlay`, which today uses only native `<details>` for
  disclosure) → mitigated by keeping the mechanism native/spec-defined rather than hand-rolled, so
  behavior is validated by manual testing on the actual target browser rather than by inventing and
  maintaining custom stacking logic.

## Open Questions

- Exact popover element markup/id-naming scheme (e.g. one `<div popover>` per referenced rule per
  unit block vs. some deduplicated/shared approach) — an implementation detail for `tasks.md`,
  doesn't change any spec-level requirement above.
- Whether an ability should itself become a cross-reference *target* (i.e. another ability's text
  linking to it by name, not just to a universal/faction rule) — no real example of this has been
  observed in the data explored so far; left for a future change if real data shows it's needed,
  rather than building for a case not yet confirmed to exist.
