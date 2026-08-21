## Why

`/LivePlay` renders ability names and weapon-keyword tags with no way to see what they actually
do — `Ability.Text` exists on the domain model today but is rendered in exactly one place
(the invulnerable-save caveat box), and weapon keywords render as boolean-flag-derived tags with
no text at all. This is a reference tool used mid-game on a tablet/phone; a player who forgets
what `[SUSTAINED HITS 1]` or their Chapter's own detachment rule does currently has to break away
to a rulebook or another app. BSData already carries this text — universal rule text in the base
game system's `sharedRules`, faction/detachment rule text in library/faction catalogues' `rules`
arrays, both reachable through the closure-resolution machinery `catalogue-json-ingestion` already
built — it is simply discarded today.

## What Changes

- New `RuleDefinition`/`RuleGlossary` domain type populated from BSData's `sharedRules` (game-system
  level) and `rules` (faction/library level) arrays, resolved via the existing catalogue-closure
  import walk. Indexed by a normalized key built from both `Name` and `Alias` (BSData's `alias`
  values already closely match the literal ALL-CAPS tag strings `WeaponAbilityTags()` hardcodes,
  e.g. `"LETHAL HITS"`, but real ability/weapon text almost always appends a value or category to a
  generic mechanic's bare name, e.g. `"SUSTAINED HITS 1"`, `"ANTI-VEHICLE 3+"` — an ordered,
  bounded text-normalization pipeline, not fuzzy matching, resolves these against the same bare
  entry).
- New mechanical extraction of literal `[BRACKET]` tokens from ability/rule `Text`, normalized
  (markup characters and known Unicode whitespace/hyphen variants found inside the brackets are
  cleaned up) and then resolved against the glossary through that same normalized-key lookup. This
  is lookup, not interpretation — no behavioral effect is ever derived from the matched text,
  consistent with the existing "ability text is never auto-parsed into behavior" stance.
  `*italic*`/`**bold**`/`^^small-caps^^` markup is explicitly **not** treated as a link signal (not
  a documented format — checked BSData's own
  `catalogue-development` wiki, which documents no markup grammar for description text — but
  confirmed real, consistent usage across the corpus, including nested `***bold+italic***`-style
  text) and renders as styling only, mapped 1:1 to italic/bold/small-caps respectively.
- `/LivePlay`'s weapon-keyword rendering changes from one joined `[TAG, TAG, TAG]` span per weapon
  row to one bordered chip per keyword, matching both the official GW app's and Wahapedia's
  rendering — and letting each keyword be independently identified as a tap target, which a single
  joined span cannot.
- Tapping an ability name or a weapon-keyword chip opens a popover, anchored locally to the tapped
  element (native CSS anchor positioning, not centered), showing that ability's or rule's full
  text with real newlines preserved as line breaks (currently silently collapsed — the one existing
  `Ability.Text` rendering site has no `white-space` handling).
- A `[BRACKET]` token inside a popover's own text is itself tappable, opening a second popover
  anchored to that token and stacked on top of the first (native Popover API nested/ancestor-aware
  light-dismiss — tapping outside the child but inside the parent closes only the child; tapping
  fully outside both closes the whole stack).
- `.claude/vnext-ideas.md`'s "Ability/weapon-keyword definitions popup" entry — including its
  now-superseded "genuine centre-screen popup" framing — is removed once this ships.

## Capabilities

### New Capabilities
- `rules-glossary`: resolves BSData's `sharedRules`/`rules` text into a queryable glossary
  (by Name and Alias) over a catalogue closure, and mechanically extracts `[BRACKET]`
  cross-reference tokens from a piece of ability/rule text for lookup against it.

### Modified Capabilities
- `live-play-view`: weapon-keyword rendering changes from one joined tag span to one chip per
  keyword; ability names and weapon-keyword chips become tap targets that open a locally-anchored,
  dismissable popover showing full rule text, including nested popovers for cross-references
  within that text.

## Impact

- New code under `ProbHammer.Core/Domain/Catalogue/Bsdata/` (glossary type + builder + bracket-
  token extraction), consumed via the existing `ResolvedBsdataCatalogue`/`BsdataCatalogueCache`
  caching tier — no new caching mechanism.
- `ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` — `WeaponAbilityTags()`/weapon-tags rendering,
  ability-name-line rendering in both Model and Unit ability columns.
- `ProbHammer.Web/wwwroot/css/site.css` — new keyword-chip and popover styles, `white-space`
  handling for multi-paragraph rule text.
- No new server API/endpoint, no client-side fetch, no new third-party dependency — resolution
  stays server-side and text is emitted as part of the existing per-request Razor render, matching
  every other page in this app.
