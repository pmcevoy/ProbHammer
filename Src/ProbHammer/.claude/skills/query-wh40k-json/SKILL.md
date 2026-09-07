---
name: query-wh40k-json
description: Query BSData catalogue JSON, roster export JSON (NewRecruit/BattleScribe), and test fixture JSON in this repo using jq instead of writing throwaway Python/.NET scripts. Use whenever you need to inspect a datasheet's profiles/weapons/abilities, search across the BSData corpus for a name/rule/infoLink shape, or look at a captured export/fixture file.
---

# Querying wh40k JSON with jq

`jq` is on PATH. Use it instead of a throwaway Python or C# script whenever the task is "look
something up in a JSON file" — it's faster to write, and the recipes below already account for
this repo's specific JSON shapes (BattleScribe `catalogueSchema` / `rosterSchema`).

Always double-quote file paths on the command line — most BSData file names contain spaces
(`"Imperium - Adeptus Custodes.json"`). On Windows use the Bash tool (Git Bash), not PowerShell —
these recipes are POSIX `jq`/shell.

## Where the JSON lives

| Location | What it is | Shape |
|---|---|---|
| `src/ProbHammer.Web/BsData/*.json` (46 files) | The bundled BSData snapshot the running app actually reads | `{ "catalogue": {...} }` |
| `C:\Users\Pete\wh40k-11e\*.json` | External live BSData clone — same shape, ahead of the bundled snapshot, this machine only. Never read by automated tests; useful for manual corpus checks (see `.claude/domain-model-11e.md`'s "Full-Corpus Scan Tests") | `{ "catalogue": {...} }`, plus one root game-system file `"Warhammer 40,000.json"` shaped `{ "gameSystem": {...} }` |
| `tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/*.json` | Trimmed excerpts used by unit tests | Same catalogue shape, or small hand-built closure fixtures |
| `data/gw-app-export-*.json`, `data/gw-android-export-*.json`, `data/nr-*.json`, `data/verification-*.json` | Captured real exports (GW app text pipeline has `.txt` twins; NewRecruit/BattleScribe roster JSON) | `{ "roster": {...} }` (rosterSchema, see below) |

Prefer the in-repo `src/ProbHammer.Web/BsData/` copy unless you're specifically doing a
corpus-wide sanity check the way the `[Fact(Explicit = true)]` CorpusScan tests do — those
intentionally read the external clone (see `LiveClone.RequireSource()` in
`tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/`).

## Catalogue file shape (`{"catalogue": {...}}`)

Top-level `.catalogue` fields: `id`, `name`, `gameSystemId`, `catalogueLinks` (imports),
`categoryEntries`, `sharedSelectionEntries` (datasheets), `sharedSelectionEntryGroups`,
`sharedInfoGroups`, `sharedProfiles`, `sharedRules`, `entryLinks`.

A datasheet (`sharedSelectionEntries[]` entry) nests `profiles[]` (each a `typeName` of
`"Unit"` | `"Ranged Weapons"` | `"Melee Weapons"` | `"Abilities"` | `"Transport"`, each with
`characteristics[]` of `{name, $text, typeId}`), plus `selectionEntries[]`/
`selectionEntryGroups[]` for nested wargear/model-count options, `infoLinks[]` (references to
`sharedRules`/other entries — `type: "rule"` is a Core/Army rule reference, `type: "profile"` a
weapon/ability reference), and `modifiers[]`/`modifierGroups[]` (conditional hide/set logic).

```bash
F="src/ProbHammer.Web/BsData/Imperium - Adeptus Custodes.json"

# List every datasheet name in a file
jq -r '.catalogue.sharedSelectionEntries[].name' "$F"

# Dump one datasheet's full JSON
jq '.catalogue.sharedSelectionEntries[] | select(.name=="Custodian Wardens")' "$F"

# List a datasheet's profiles (name + typeName) — quick overview before diving in
jq '.catalogue.sharedSelectionEntries[] | select(.name=="Custodian Wardens")
    | .profiles[]? | {name, typeName}' "$F"

# Just the Unit statline characteristics
jq '.catalogue.sharedSelectionEntries[] | select(.name=="Custodian Wardens")
    | .profiles[]? | select(.typeName=="Unit") | .characteristics
    | map({(.name): .["$text"]}) | add' "$F"

# Just the ranged/melee weapon profiles
jq '.catalogue.sharedSelectionEntries[] | select(.name=="Custodian Wardens")
    | .profiles[]? | select(.typeName=="Ranged Weapons")' "$F"

# Every ability's name + text
jq '.catalogue.sharedSelectionEntries[] | select(.name=="Custodian Wardens")
    | .. | objects | select(.typeName? == "Abilities") | {name, id}' "$F"

# Nested selection groups (model-count / wargear options) — names only, one level
jq '.catalogue.sharedSelectionEntries[] | select(.name=="Custodian Wardens")
    | [.selectionEntries[]?.name, .selectionEntryGroups[]?.name]' "$F"

# infoLinks of type "rule" (Core/Army rule references) anywhere under a datasheet
jq '.catalogue.sharedSelectionEntries[] | select(.name=="Custodian Wardens")
    | .. | objects | select(.type? == "rule")' "$F"

# Resolve a sharedRules entry by name (glossary lookup)
jq '.catalogue.sharedRules[] | select(.name=="Martial Ka'"'"'tah")' "$F"

# Every distinct typeName found anywhere in the file (sanity-check for an unmapped shape)
jq -r '[.. | objects | select(has("typeName")) | .typeName] | unique[]' "$F"

# Every distinct infoLink "type" value in the file (mirrors InfoLinkTypeAllowlist's own scan)
jq -r '[.. | objects | select(has("targetId") and has("type")) | .type] | unique[]' "$F"

# catalogueLinks — what this faction file imports
jq '.catalogue.catalogueLinks' "$F"

# A Detachment-choice group by name (case-insensitive)
jq '.catalogue.sharedSelectionEntryGroups[]? | select(.name | test("Detachment"; "i"))
    | {name, id}' "$F"
```

### Searching across the whole corpus

`jq` has no built-in multi-file "grep for this value and tell me which file" — loop in the shell
and use `jq -e` (or `-c`) per file, this is the idiomatic replacement for a Python walk-the-corpus
script:

```bash
# Which files define a datasheet named "X"?
for f in src/ProbHammer.Web/BsData/*.json; do
  jq -e --arg n "X" '.catalogue.sharedSelectionEntries[]? | select(.name==$n)' "$f" >/dev/null \
    && echo "$f"
done

# Every distinct typeName across the entire bundled corpus (one-liner, no shell loop needed —
# jq -s slurps every file into one array first)
jq -s -r '[.[] | .catalogue | .. | objects | select(has("typeName")) | .typeName] | unique[]' \
  src/ProbHammer.Web/BsData/*.json

# Count occurrences of an ability name across the corpus
jq -s '[.[] | .catalogue | .. | objects | select(.name? == "Oath of Moment")] | length' \
  src/ProbHammer.Web/BsData/*.json

# Which files contain an infoLink of type "infoGroup" (the confirmed, allowlisted gap —
# see .claude/domain-model-11e.md's "Core Rule Ability Extraction")
for f in src/ProbHammer.Web/BsData/*.json; do
  n=$(jq '[.. | objects | select(.type? == "infoGroup")] | length' "$f")
  [ "$n" != "0" ] && echo "$f: $n"
done
```

Use `-s` (slurp) when you want one aggregate answer across every file; use a shell loop when you
need to know *which* file matched.

## Roster export JSON (`{"roster": {...}}`)

BattleScribe/NewRecruit `rosterSchema` — already fully resolved, no BSData lookup needed to read
one of these. `.roster.forces[]` holds one force per detachment/army; each `selections[]` entry
recurses into its own `selections[]` for nested wargear/model children (same shape at every
nesting level: `id`, `name`, `type`, `number`, `profiles`, `rules`, `costs`, `selections`,
`associations`).

```bash
R="data/nr-export-chaos-lord-terminator-armour.json"

# Top-level roster metadata
jq '.roster | {name, gameSystemName, costs}' "$R"

# Every top-level selection name + type (a unit/model is typed "model", not "unit" —
# "upgrade" covers Detachment/Battle Size/Force Disposition and other non-model choices)
jq -r '.roster.forces[].selections[] | "\(.type)\t\(.name)"' "$R"

# Just the model (unit) selections
jq -r '.roster.forces[].selections[] | select(.type=="model") | .name' "$R"

# One selection's full tree
jq '.roster.forces[].selections[] | select(.name=="Chaos Lord in Terminator Armour")' "$R"

# Every profile characteristic on a selection (already-resolved statline/weapon text)
jq '.. | objects | select(.profiles? != null) | .profiles[] | {name, typeName,
    characteristics: (.characteristics | map({(.name): .["$text"]}) | add)}' "$R" | head -80

# Leader/Support attachment associations (Leading/Supporting)
jq '.. | objects | select(.associations? != null) | .associations' "$R"

# Every rule name attached anywhere (for building/checking a RuleGlossary)
jq -r '[.. | objects | select(.rules? != null) | .rules[].name] | unique[]' "$R"
```

## Test fixture JSON (`tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/*.json`)

Same catalogue shape as the bundled BSData files (trimmed excerpts) or small hand-built closure
fixtures — the recipes above apply directly. Use these to check what a test fixture actually
contains before editing the test that references it:

```bash
jq '.catalogue.sharedSelectionEntries[].name' \
  tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/crusader-squad-enrichment.json
```

## Tips

- `jq -C` for colored output when eyeballing in the terminal; drop it when piping.
- `jq -e '<filter>' file >/dev/null` — exit code only, good for shell-loop `if`/`&&` checks (used
  above for the "which file" search).
- `.. | objects | select(...)` walks every nested object regardless of depth — the right default
  for this corpus since datasheet nesting depth varies (a single-model entry vs. a squad with
  wargear sub-options aren't the same shape).
- `$text` must be quoted as `.["$text"]` in jq filters — `$` starts a jq variable reference
  otherwise.
- A literal apostrophe inside a `jq` program on the shell command line needs the
  `'"'"'` escape (see the `Martial Ka'tah` example above) — or pass the value via `--arg`
  instead, which is cleaner for anything scripted (see the corpus-search examples).
