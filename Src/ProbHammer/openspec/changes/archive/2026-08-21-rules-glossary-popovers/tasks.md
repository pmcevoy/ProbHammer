## 1. BSData Rules JSON Ingestion

- [x] 1.1 Add `Rules`/`SharedRules` fields to the BSData JSON model (`Domain/Catalogue/Bsdata/Json/`)
      for the `rules`/`sharedRules` arrays, each entry `{Name, Description, Alias}`, defaulting to
      an empty collection when the key is absent from a given file.
- [x] 1.2 Add trimmed fixture excerpts under `tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/` for
      both shapes (a `sharedRules` entry like "Lethal Hits" with an `alias`; a `rules` entry like
      "Templar Vows"), generated the same way existing fixtures are — deserialize a real file
      through the reader, re-serialize just the needed entries.
- [x] 1.3 Unit tests confirming `BsdataCatalogueReader.Read` deserializes both fields correctly
      against those fixtures, and that a file with neither key present still deserializes cleanly.

## 2. RuleGlossary Domain Type

- [x] 2.1 Add `RuleDefinition(Name, Aliases, Text)` to `Domain/Catalogue/Bsdata/`.
- [x] 2.2 Add `RuleGlossary` with `Build(BsdataClosure closure)` — iterate `closure.Files` in
      existing resolution order collecting each file's own `Rules`, then `closure.GameSystem
      ?.SharedRules`, first occurrence wins by Name and separately by Alias — and a non-throwing
      `TryResolve(string nameOrAlias) -> RuleDefinition?`.
- [x] 2.3 Extend `ResolvedBsdataCatalogue.Build` to also build and expose a `Glossary` property.
- [x] 2.4 Unit tests: a universal rule resolves by its Name and separately by its Alias; a
      faction/library rule reached transitively through the closure resolves; an unknown name
      returns null without throwing; a synthetic local-vs-imported collision fixture resolves to
      the local definition, mirroring `BsdataNameResolver`'s existing precedence tests.

## 3. Bracket Token Extraction & Resolution

- [x] 3.1 Add `RuleTextTokenizer.ExtractBracketTokens(text) -> IReadOnlyList<string>` — simple
      non-nested `[...]` extraction, with no awareness of `**`/`^^` markup at all.
- [x] 3.1a Extend extraction to normalize each captured token before returning it: strip any `*`/`^`
      characters found inside the brackets (confirmed real shape: `[^^Lethal Hits^^]`), and replace
      known Unicode whitespace/hyphen variants (U+00A0 no-break space, U+2011 non-breaking hyphen,
      U+2013 en dash) with their plain-ASCII equivalent (confirmed real shapes: a no-break space in
      `DEVASTATING<NBSP>WOUNDS`; a non-breaking hyphen in several `ANTI‑VEHICLE`-style references).
      Stays a pure, glossary-independent step — see design.md's "Bracket-token extraction is a
      separate, pure function from resolution".
- [x] 3.2 Unit tests: a token embedded in running text alongside `**`/`^^` markup extracts cleanly
      (e.g. `"...has the **[PRECISION]** ability..."` extracts `"PRECISION"`); text containing
      `^^**Fabius Bile**^^`/`^^**Warlord**^^` produces no tokens; inconsistently nested
      `^^Fabius Bile^^**` doesn't affect extraction.
- [x] 3.2a Unit tests for the new extraction-time normalization: `[^^Lethal Hits^^]` extracts as
      `"Lethal Hits"`; a token containing a U+00A0 no-break space extracts with a plain space in its
      place; a token containing a U+2011 non-breaking hyphen extracts with a plain `-` in its place.
- [x] 3.3 Unit tests resolving extracted tokens against a `RuleGlossary`: an exact match resolves
      to the right `RuleDefinition`; an unmatched token resolves to null without throwing or
      affecting resolution of the rest of the text.
- [x] 3.3a Replace the separate exact/fallback dictionaries in `RuleGlossary` with a single
      normalized-key index, per design.md's "One normalized-key index, built from both Name and
      Alias": `Normalize(string)` lowercases, strips a trailing value/threshold/dice/placeholder
      suffix, collapses an `anti`-prefixed string (with a required space/hyphen/colon boundary) to
      bare `anti`, then strips remaining punctuation/whitespace — applied identically to every
      `RuleDefinition`'s `Name` and each `Alias` when building the index, and to the query string
      at lookup time. Unit tests: `"SUSTAINED HITS 1"` and `"Sustained Hits D3"` both resolve to
      the Sustained Hits rule; `"ANTI-VEHICLE 3+"` and `"ANTI: non‑MONSTER/VEHICLE 5+"` both
      resolve to the Anti rule via the named exception; `"Antimatter Drive"` does **not** resolve
      (boundary check); `"Twin-Linked"` resolves to the Twin-linked rule despite the casing
      difference; `"CLEAVE 1"`/`"CLEAVE X"` resolve to the Cleave rule via its own `Name` (no
      `Alias` exists); `"CLOSE-QUARTERS"` resolves to the Close-quarters rule via its own `Name`
      for the same reason.
- [x] 3.4 Add an explicit-opt-in corpus-scan test (mirroring the existing `WeaponKeywordScanTests`/
      `CharacteristicResolutionScanTests` pattern in `CorpusScan/`) that walks every closure's
      ability and rule text, extracts bracket tokens, and reports any that fail to resolve against
      that closure's own glossary — validates the exact-match assumption at real-corpus scale
      without running as part of the normal `dotnet test` suite.
- [x] 3.5 Re-run the corpus scan (task 3.4) against the live clone now that 3.1a/3.3a are
      implemented, and rewrite `BracketTokenResolutionAllowlist` down to whatever genuinely remains
      unresolved — confirm the actual final count and token set against design.md's "One
      normalized-key index, built from both Name and Alias" and update that section with the
      confirmed number.
      **Confirmed**: 2 occurrences remain (down from 1,964) — `IGNORES COVER abilities` (one-off
      bracket-placement typo) and `whichever applies` (non-reference bracketed prose). `Cleave`/
      `Close-quarters` now resolve correctly via the unified Name+Alias index.

## 4. Weapon-Keyword Chip Rendering

- [x] 4.1 Change `_UnitBlock.cshtml`'s weapon-tags rendering (both the Ranged and Melee weapon
      tables) from one joined `[@string.Join(...)]` span per row to one chip per tag returned by
      `WeaponAbilityTags()`.
- [x] 4.2 Look up each tag string against the page's `RuleGlossary` to decide whether that chip is
      an interactive trigger (matched) or a plain inert chip (unmatched).
      Threaded `RuleGlossary` from `ResolvedBsdataCatalogue.Glossary` through to the partial: new
      `ArmyRosterBuildResult(ArmyRoster Roster, RuleGlossary Glossary)` return type on
      `IArmyRosterProvider.Build` (was bare `ArmyRoster`); `LivePlayModel.Glossary` property set in
      `OnGet`; `UnitBlockRenderModel` gained a third `Glossary` parameter so both `/LivePlay`'s own
      render and the casualty-sync endpoint's fragment re-render (`LivePlayCasualtyService`, which
      renders `_UnitBlock.cshtml` directly, not through the page) supply it.
- [x] 4.3 Add chip styling to `site.css` — bordered pill, using existing `--border`/radius tokens
      per `.claude/design-tokens.md` — matching the GW-app/Wahapedia reference precedent.
      Unresolved chips get `opacity: 0.55` (the page's existing disabled-control convention)
      rather than a distinct color, per rules-glossary's "A keyword chip with no matching glossary
      entry is not interactive".
- [x] 4.4 Manual check in-browser: a weapon with multiple keywords (e.g. a pistol with Ignores
      Cover, Pistol, and Torrent) renders three separate chips, not one joined bracket.
      Verified live via `dotnet run` + `firefox-devtools-mcp` against a real imported army list
      (`data/gw-app-export.txt`, Black Templars): Pyre Pistol renders three separate `.weapon-tag`
      elements (TORRENT/PISTOL/IGNORES COVER), Ferocity renders two (DEVASTATING WOUNDS/
      ANTI-INFANTRY 4+) — confirmed via DOM inspection, not just a screenshot. All ~29 chips across
      the roster resolved (`weapon-tag-resolved` class), including value-suffixed ones
      (`SUSTAINED HITS 1`, `RAPID FIRE 1/2`, `MELTA 2`, `ANTI-INFANTRY 4+`, `ANTI-CHARACTER 5+`) —
      real end-to-end confirmation that the normalized-key resolution work from section 3 is wired
      correctly into the UI.

## 5. Ability & Rule Text Popover

- [x] 5.1 Make each ability-name-line entry (Model and Unit Abilities columns) a tap trigger wired
      to a `popover="auto"` element carrying that ability's `Text`.
      `.ability-name-line` changed from a plain `<div>` to a `<button popovertarget="p-{id}">`
      (matching `.weapon-name-toggle`'s own established button-reset pattern), with its paired
      `<div id="p-{id}" popover="auto">` rendered as its next *sibling*, never nested inside it -
      required since a `<button>` cannot validly contain flow content like a `<div>`. This also
      broke `.ability-name-line`'s existing `:nth-child(even)` zebra striping (the interleaved
      popover `<div>`s shift child-position parity) - fixed by switching to `:nth-of-type(even)`,
      which counts only same-tag (`<button>`) siblings.
- [x] 5.2 Wire each resolvable keyword chip (from Section 4) to the same popover mechanism, sourced
      from the matched `RuleDefinition.Text`.
      Found and fixed a real bug flagged during review: `.weapon-tags` was rendered *inside*
      `.weapon-name-toggle` (the button that expands a weapon's contribution breakdown), so tapping
      a chip nested inside it would bubble up and toggle the breakdown too - confirmed live before
      the fix (clicking a chip incorrectly changed `aria-expanded`/revealed breakdown rows). Fixed
      by moving `.weapon-tags` to render as `.weapon-name-toggle`'s sibling instead of its child
      (same invalid-nesting reasoning as 5.1's `<button>`-in-`<button>` case) - confirmed live after
      the fix that tapping a chip only opens its popover, leaving `aria-expanded`/breakdown-row
      visibility untouched.
- [x] 5.3 Add popover text styling: `white-space: pre-line` so real line breaks render, base panel
      styling consistent with `.claude/design-tokens.md`'s existing palette/spacing/radius scale.
- [x] 5.4 Add CSS anchor positioning (`anchor`/`position-anchor`/`anchor()`) so each popover opens
      adjacent to its trigger, wrapped in `@supports(anchor-name: --x)` with a simple non-anchored
      fallback rule for an unsupporting browser.
      Used the CSS `anchor-name`/`position-anchor` custom-property pair (each trigger/popover pair
      gets a unique inline `--a{id}` value), not the newer HTML `anchor="..."` IDREF attribute -
      see 5.5's finding for why.
- [x] 5.5 Manual check on the actual target device/browser: popover opens adjacent to its trigger
      and dismisses on an outside tap; note whether anchor positioning is actually supported there
      (confirms or refutes the Firefox Mobile risk noted in `design.md`).
      Tested live via `dotnet run` + `firefox-devtools-mcp` (Firefox 154) against a real imported
      army list. **Open/dismiss functionality confirmed fully working**: an ability-name-line
      popover and a weapon-tag-chip popover both open showing the correct text, and a tap outside
      closes the open popover (native `popover="auto"` light-dismiss, no JS needed). **Positioning
      confirmed NOT working in this browser build** - and found something more specific than
      design.md's original risk anticipated: this Firefox reports `CSS.supports()` true for both
      the HTML `anchor` attribute's underlying properties (`anchor-name`/`position-anchor`) *and*
      `position-area`, but only `anchor-name`/`position-anchor` actually apply (the HTML `anchor`
      attribute itself is unsupported - confirmed via `'anchor' in document.createElement('div')`
      → `false`); switching to that pair still didn't visibly position anything, though - dev-tools
      inspection showed the `position-area` rule correctly matched and applied in the CSSOM, but
      `getComputedStyle` still resolved to `top: 0; left: 0`, meaning this browser's
      anchor-positioning implementation is real but still incomplete/immature, not simply absent.
      Per design.md's explicit "no polyfill unless real testing shows one is needed" stance, this
      is left as-is: the popover still opens/shows/dismisses correctly either way, exactly the
      "only placement quality degrades, never functionality" behavior the requirement calls for -
      not something to chase further for one browser's early implementation.

## 6. Nested Reference Popovers

- [x] 6.1 At popover-render time, run `RuleTextTokenizer.ExtractBracketTokens` +
      `RuleGlossary.TryResolve` over the popover's own text and wrap each resolved token in its
      own nested `popover="auto"` + `anchor` trigger, per `design.md`'s ancestor-relationship
      approach.
      Built as `RuleTextEmphasisRenderer.Render(text, renderBracket)` (Core, pure, delegate-based -
      see 6.3) plus `_UnitBlock.cshtml`'s `BuildRulePopover`/`RenderNestedReference` local functions
      (Web, glossary-aware), which recurse into a resolved reference's own `RuleDefinition.Text` -
      confirmed live to 3 levels deep on real data (a "Tactical Precision" ability → its granted
      "Lethal Hits" rule → that rule's own referenced "Devastating Wounds" rule). **Found and fixed
      a real infinite-recursion bug during this work**: several real BSData rules self-reference
      their own name in their own description text (confirmed: "Sustained Hits" contains
      `**[SUSTAINED HITS X]**`, "Anti" contains `**[ANTI-X Y+]**`, "Cleave" contains
      `**[CLEAVE X]**`) - an unguarded first version stack-overflowed rendering a real weapon's
      Sustained Hits chip. Fixed with a `shownRuleNames` ancestor-chain parameter threaded through
      both functions: a bracket resolving back to a rule already being displayed anywhere in its
      own ancestor chain renders as plain text instead of recursing again (same graceful-fallback
      treatment as a genuinely unresolved token - see design.md for the full writeup, since this
      is a real spec-relevant finding beyond what the original tasks anticipated).
- [x] 6.2 Leave an unresolved bracket token as plain text — no trigger, no nested popover.
- [x] 6.3 Add an emphasis-markup renderer mapping `*italic*` → `<i>`, `**bold**` → `<b>`, and
      `^^small-caps^^` → a small-caps `<span>`, including the nested `***bold+italic***` split-close
      case (see `design.md`'s "Emphasis markup: a three-way mapping, not a link signal") — a real
      parser tracking open/close pairs, not sequential string replacement. Confirm none of the three
      ever produce an interactive trigger regardless of what text they wrap.
      `RuleTextEmphasisRenderer` (Core): a single left-to-right token scan (longest-alternation-first
      regex: `***`/`**`/`*`/`^^`/`[BRACKET]`) tracking three independent open/closed booleans
      (bold/italic/small-caps, `***` toggling both bold and italic together) - not a stack, since
      the confirmed real split-close case needs each to close independently later. Bracket handling
      is fully delegated (`Func<string, (string Inline, string Popovers)>`), keeping this class
      free of any `RuleGlossary`/popover-id concern and independently testable (6.3a). A nested
      popover's own `<div>` is threaded back separately from inline content at every recursion
      level, specifically so it's never nested inside an ancestor `<b>`/`<i>`/small-caps `<span>` -
      that would leak the wrapping styling into the nested popover's own text via ordinary CSS
      inheritance (top-layer promotion doesn't detach a popover from the DOM tree for inheritance
      purposes, only for stacking/paint order).
- [x] 6.3a Unit tests for the emphasis renderer: single-asterisk italic; double-asterisk bold;
      double-caret small caps; `^^**Fabius Bile**^^` renders both bold and small-caps; the "Lethal
      Hits" Designer's Note nested `***X**...*` case renders "Designer's Note:" as bold+italic and
      the remaining text up to the closing `*` as italic only.
- [x] 6.4 Manual check: open a popover whose text contains a resolvable bracket reference, tap it,
      confirm the parent stays open; tap a location inside the parent but outside the child closes
      only the child; tap fully outside both closes both.
      Verified live via `dotnet run` + `firefox-devtools-mcp` (Firefox 154) against real imported
      data, using genuine trusted clicks throughout (not synthetic/JS-dispatched events - confirmed
      separately that Firefox's native light-dismiss correctly ignores those, as expected/correct
      security behavior, ruling out a JS-dispatch shortcut for this check). Found 23 real nested
      references across the roster (`CLOSE-QUARTERS`, `LETHAL HITS`, `DEVASTATING WOUNDS`). Opened
      "Tactical Precision" (Lieutenant) → its "LETHAL HITS" nested reference: parent stayed open
      throughout, per "Opening a nested popover keeps the parent open". A trusted click on the
      parent's own text (outside the child) closed only the child, parent remained open - "Dismissing
      between the two closes only the nested popover". A trusted click fully outside both (the page
      `<h1>`) closed both - "Dismissing outside both closes the whole stack". All three scenarios
      confirmed exactly as specified.

## 7. Cleanup

- [x] 7.1 Remove `.claude/vnext-ideas.md`'s "Ability/weapon-keyword definitions popup" entry now
      that it's implemented (including its superseded "genuine centre-screen popup" framing).
- [x] 7.2 Update `.claude/domain-model-11e.md`'s note about `_UnitBlock.cshtml`'s weapon-tag
      rendering "reconstructing text from the typed flags" to reflect the new per-keyword-chip,
      glossary-lookup rendering shape (still sourced from the boolean-flag-derived tag strings, not
      from `KeywordsText` — that switch remains a separate, still-deferred future step).
      Also added a full new "Rules Glossary & Popovers" section documenting `RuleDefinition`/
      `RuleGlossary`/`RuleTextTokenizer`/`RuleTextEmphasisRenderer` and the `/LivePlay` wiring, plus
      the corpus-scan test — matching this file's existing per-feature documentation convention,
      since no prior section covered any of this new domain area.
- [x] 7.3 Add the new keyword-chip and popover visual details to `.claude/design-tokens.md`.
      Extended the `--accent` palette row, the invulnerable-save-box Typography note, the
      Border-radius/no-drop-shadow Spacing & Shape bullets, and Interaction States (a new
      resolved/unresolved chip bullet, plus a correction to the ability-list zebra-striping bullet
      — `:nth-child(even)` → `:nth-of-type(even)`, since it was already stale after section 5's
      work landed). Added a new "Popovers" section covering anchor-positioning placement (including
      the real-device-testing finding) and how nesting reuses the same mechanism recursively.
