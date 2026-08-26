## 1. Datasheet per-model keyword lookup

- [x] 1.1 In `Datasheet.cs`, add an optional trailing constructor parameter
  `modelKeywords: IEnumerable<(string Name, IReadOnlySet<string> Keywords)>? = null` (defaulting to
  empty, mirroring `optionalAbilities`'s `?? []` pattern), and build an internal
  `_modelKeywordsByName` dictionary from it once, the same way `_statlinesByName` is built.
- [x] 1.2 Add `Datasheet.GetModelKeywords(string name) -> IReadOnlySet<string>`, returning the
  matching set or an empty set for an unknown name — never throwing, since most named models have
  no per-model keyword data at all.
- [x] 1.3 Unit test: a name with per-model keyword data returns that set; an unknown/no-data name
  returns an empty set, not null and not an exception.

## 2. BSData catalogue extraction (`BsdataDatasheetMapper`)

- [x] 2.1 Add a small helper that maps a `categoryLinks` list into a `Keywords` set: each entry's
  `name`, with a literal `"Faction: "` prefix stripped when present, otherwise kept verbatim — no
  exclusions, including an entry matching the owning selection's own name.
- [x] 2.2 In `BuildDatasheet`, call this helper on the resolved entry's own `categoryLinks` and
  pass the result into `Datasheet`'s `keywords:` argument (replacing the current empty list).
- [x] 2.3 In the same recursive walk that already extracts each child model entry's own Statline
  (per `Statline Extraction`), also run this helper over that same entry's own `categoryLinks`,
  collecting one `(Name, Keywords)` pair per named model, and pass the collected list into
  `Datasheet`'s new `modelKeywords:` argument.
- [x] 2.4 Unit tests using a small hand-built or trimmed real fixture (mirroring existing
  `tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/` fixtures): a bare category becomes a Keyword
  unchanged; a `"Faction: X"` category becomes `"X"`; an entry matching the datasheet's own name is
  kept; a child model entry's own `categoryLinks` (e.g. a Garlon-Souleater-shaped fixture with a
  `Psyker` category on only one of several named models) populates only that model's own keywords,
  not the Datasheet's top-level Keywords or any other model's.

## 3. BattleScribe roster JSON extraction (`BattleScribeRosterMapper`)

- [x] 3.1 Add a small local helper mirroring 2.1's rule but reading `categories` (roster JSON's own
  field name), consistent with this pipeline's existing convention of small, functionally-identical
  local equivalents rather than reusing `BsdataDatasheetMapper`'s own private helpers.
- [x] 3.2 In `BuildUnit`, extract the top-level unit selection's own `categories` into the
  synthesized `Datasheet`'s `keywords:` argument (replacing the current empty list).
- [x] 3.3 In `BuildModelLines`, extract each nested `model`-typed selection's own `categories`
  directly into that specific `ModelLine`'s `keywords:` constructor argument (no Datasheet-level
  round-trip needed — the selection object is already in hand at this point).
- [x] 3.4 Add a trimmed fixture derived from `data/nr-app-export-masters-of-the-maelstrom.json`
  (mirroring the project's convention of trimming a real captured export down to just the needed
  entries) and unit tests: the unit-level Datasheet gets its own top-level Keywords from the top
  selection's `categories`; Garlon Souleater's `ModelLine` gets `Psyker`; a sibling model line
  (e.g. Garreon the Corpsemaster) does not.

## 4. Army roster enrichment wiring

- [x] 4.1 In `ArmyRosterEnricher`, when building a `ModelLine` from a resolved `Statline` name (the
  BSData pipeline path), call `datasheet.GetModelKeywords(name)` and pass the result into that
  `ModelLine`'s `keywords:` constructor argument.
- [x] 4.2 Unit test: a `Unit` built from a Datasheet with per-model keyword data on one statline
  name produces a `ModelLine` with that Keyword for that name, and an unrelated `ModelLine` (same
  Unit, different statline name) does not.

## 5. Verification

- [x] 5.1 Run `dotnet test` to confirm no regression.
- [x] 5.2 Manual smoke-test pass against the live BSData clone (per design.md's decision not to add
  a permanent corpus scan): spot-check that the `"Faction: "` prefix strips correctly and no
  unexpected shape appears across a handful of factions beyond the two (Black Templars, Death
  Guard) already checked during exploration. Ran a temporary scan (deleted after the run, per
  design.md) across Aeldari - Craftworlds, Necrons, Orks, T'au Empire, Adeptus Custodes, and
  Tyranids (4,828 categoryLinks entries): `"Faction: "` strips correctly everywhere (e.g. "Faction:
  Asuryani" -> "Asuryani", "Faction: Harlequins" -> "Harlequins"). Found one more real
  colon-containing category shape present on every faction file, `"Allies: Unaligned Forces"` -
  expected to pass through verbatim per design.md's "no other transformation, exclusion" rule, and
  confirmed the current implementation does exactly that. No code change needed.
- [x] 5.3 Start the dev server, import `data/gw-app-export-masters-of-the-maelstrom.txt` via
  `/Import`, and on `/LivePlay` confirm: the Masters of the Maelstrom unit's Keywords section shows
  a populated set including `Psyker`; marking Garlon Souleater a casualty removes `Psyker` from the
  rendered set with no other change; restoring him brings it back. Verified live via Firefox
  DevTools MCP against the running dev server: Keywords rendered as Battleline, Chaos, Chaos
  Undivided, Epic Hero, Grenades, Heretic Astartes ("Faction: " stripped), Infantry, Legionaries,
  Masters of the Maelstrom (own name kept), Psyker, Support; casualty/restore round-trip on Garlon
  Souleater added/removed exactly `Psyker` with nothing else changing. (Hit one unrelated, already-
  documented naming mismatch along the way - export's "Absolvor bolt pistol" vs. BSData's "Absolver
  bolt pistol" - worked around in the test paste, no code change.)
- [x] 5.4 Import `data/nr-app-export-masters-of-the-maelstrom.json` (the BattleScribe/NewRecruit
  pipeline) via `/Import` and confirm the same end-to-end result for that pipeline. Verified via
  `curl` (antiforgery token + session cookie round-trip) against the running dev server rather than
  a browser paste, to avoid loading the ~77KB single-line JSON export into context: import POST
  redirected to `/LivePlay`, and the rendered page's Keywords section for "Legionaries with Masters
  of the Maelstrom" showed the identical set as the text-pipeline run (5.3) - Battleline, Chaos,
  Chaos Undivided, Epic Hero, Grenades, Heretic Astartes ("Faction: " stripped), Infantry,
  Legionaries, Masters of the Maelstrom (own name kept), Psyker, Support.
