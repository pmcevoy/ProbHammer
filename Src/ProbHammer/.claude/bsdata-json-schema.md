# BSData 11e JSON Catalogue Schema

Reference notes on the raw file format `Domain/Catalogue/Bsdata/` reads — the BattleScribe
`catalogueSchema` shape, JSON now instead of the XML it originally was. This describes the *data
format itself*; for the C# types and pipeline that read it, see `.claude/domain-model-11e.md`'s
"BSData JSON Ingestion" section.

Real files live in a local clone at `C:\Users\Pete\wh40k-11e` (this machine only, not part of the
repo) — one file per faction/sub-faction, e.g. `Imperium - Black Templars.json`,
`Chaos - Chaos Space Marines.json`. All examples below are trimmed excerpts of real files.

---

## The root

Every file is a single wrapper object:

```json
{ "catalogue": { ...everything... } }
```

---

## Cross-file: `catalogueLinks`

A catalogue can **import** another catalogue wholesale:

```json
"catalogueLinks": [
  { "name": "Imperium - Space Marines", "targetId": "e0af-...", "importRootEntries": true }
]
```

Black Templars' own file doesn't define "Assault Intercessor Squad" or "Scout Squad" anywhere —
those names only resolve because Black Templars imports Space Marines, and Space Marines defines
them. `importRootEntries: true` means "treat everything the target file offers as available here
too." This chains: Space Marines itself imports four more files (Agents of the Imperium, Titans,
etc.), which is why resolving a name means walking outward through every import, not just reading
one file.

**Gotcha:** a link's cached `name` can drift from its target file's *actual* current name — real
example, `"Chaos - Daemons Library"` whose target file is really
`"Chaos - Chaos Daemons Library.json"`. Only `targetId`, matched against the target file's own
top-level catalogue `id`, is reliable.

---

## Within one file: `sharedSelectionEntries` — the pickable things

The flat, name-searchable list of everything nameable in the file: units, individual models,
wargear items, enhancements. One entry, trimmed:

```json
{
  "id": "eecd-6976-6410-1e42",
  "name": "Darnath Lysander",
  "type": "model",
  "profiles": [ /* stat blocks — see below */ ],
  "selectionEntries": [ /* his weapon, nested one level down */ ]
}
```

---

## `profiles` — the actual stat blocks

A profile is a `name` + a `typeName` (the field that says *what kind* of stat block it is —
`"Unit"`, `"Ranged Weapons"`, `"Melee Weapons"`, `"Abilities"`) + a `characteristics` array.
Characteristics aren't fixed columns, they're a list searched by name:

```json
{
  "name": "Darnath Lysander",
  "typeName": "Unit",
  "characteristics": [
    { "name": "M",    "$text": "5\"" },
    { "name": "T",    "$text": "5" },
    { "name": "Sv",   "$text": "2+" },
    { "name": "InSv", "$text": "4+" }
  ]
}
```

That's the whole statline. A weapon profile is the same shape with `A`/`BS`/`S`/`AP`/`D`/`Keywords`
characteristics instead of `M`/`T`/`Sv`/etc.

---

## Single model vs. squad — where the Unit profile actually sits

**Single model** (Darnath Lysander): the `"Unit"` profile sits directly in his own entry's
`profiles` array, as above.

**Squad** (Crusader Squad): the top-level entry has *no* Unit profile at all. Instead it has nested
`selectionEntryGroups` — wargear-choice groupings, e.g. a "Crusaders" group containing an
"Initiates" sub-group and a "Neophytes" sub-group — and those contain the actual
`selectionEntries`, one per loadout option:

```
Crusader Squad (entry, type: "unit")
 └─ selectionEntryGroups: "Crusaders"
     └─ selectionEntryGroups: "Initiates"
         ├─ selectionEntries: "Initiate w/ boltgun"       → profiles: [Unit "Initiate", Ranged "Boltgun"]
         ├─ selectionEntries: "Initiate w/ chainsword"    → profiles: [Unit "Initiate", Melee "Chainsword"]
         ├─ selectionEntries: "Initiate w/ heavy weapon"  → profiles: [Unit "Initiate", ...]
         └─ selectionEntries: "Initiate w/ other weapon"  → profiles: [Unit "Initiate", ...]
```

Each loadout option carries its *own copy* of the same `"Initiate"` Unit profile — byte-identical —
alongside whichever weapon that option grants. That's why raw "Initiate" occurrences show up 4
times in the file: four wargear options, not four model variants. (The loader dedupes by profile
name for exactly this reason.)

---

## Cross-references inside an entry: `entryLinks` vs `infoLinks`

Two different pointer mechanisms, both "look elsewhere by id":

- **`entryLinks`** point at another *entry* or *group* — used for shared wargear. A squad's "Bolt
  pistol" choice is usually an `entryLink` to one canonical "Bolt pistol" `selectionEntry` defined
  once (maybe even in an imported file) and reused by every unit that can take one, rather than
  being redefined per-unit.
- **`infoLinks`** are lighter, mostly `type: "rule"` (pointing at glossary-style rules text). But
  occasionally `type: "profile"`, pointing straight at a *profile* sitting in a separate top-level
  `sharedProfiles` list rather than being nested in the tree at all. Real example: Chaos Space
  Marines' "Legionaries" squad — its troop model's Unit profile lives only in `sharedProfiles`,
  reached by an `infoLink`, not nested under the squad anywhere a plain tree-walk would find it.

Both link kinds' cached `name` can go stale relative to their target — only `targetId` is reliable.

---

## What's deliberately ignored

Every entry above also carries `constraints`, `modifiers`, `conditionGroups`, `associations`, and
`costs` — BattleScribe's own list-building/validation layer (min/max picks, points values, "who can
lead whom"), sitting right alongside the stat data in the same JSON. It's real and present in every
real file; the loader's C# types simply don't model it, so it's silently dropped on parse rather
than needing to be explicitly stripped out.
