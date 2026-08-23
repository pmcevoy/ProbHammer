## Context

See `proposal.md` for motivation. Relevant current state, confirmed by direct investigation
against the bundled `src/ProbHammer.Web/BsData/` snapshot and a real pasted export
(`data/gw-app-export-templars.txt`):

- `BsdataDatasheetMapper.ProcessProfile`'s `"Abilities"` case (`src/ProbHammer.Core/Domain/
  Catalogue/Bsdata/BsdataDatasheetMapper.cs`) adds every `Abilities`-typeName profile it encounters
  to `WalkContext.Abilities`, deduped by name only — with no regard for where in the tree the
  profile was found. `BuildDatasheet` copies that list straight into `Datasheet.Abilities`
  (`src/ProbHammer.Core/Domain/Catalogue/Datasheet.cs`), a plain `IReadOnlyList<Ability>` with no
  on-demand-only protection — unlike `WeaponProfile`, which the same class already resolves only by
  name (`ResolveWeaponProfile`/`TryResolveWeaponProfile`).
- Confirmed by directly reading the raw JSON: an intrinsic ability (e.g. Crusade Ancient's
  "Vengeful Exhortation") sits directly in `entry.Profiles`, where `entry` is the same
  `BsSelectionEntry` that owns the resolved `Unit` statline profile, and that entry's own
  `Type` is `"model"`. Every leaking ability confirmed this session — "Thirst for Glory" (Crusade
  Ancient, via the shared "Enhancements" → "Legends of Saga and Song Enhancements" entryLink
  chain), "Fervent Exemplars" and "Inheritors of Sigismund" (Sword Brethren Squad, via its own
  *locally-nested* "Enhancements" selectionEntryGroup) — instead sits in `entry.Profiles` where
  `entry.Type == "upgrade"`: BSData's own structural marker for a player-selectable choice, the
  same shape a weapon option uses (`Inheritors of Sigismund`'s own constraint is literally `max 3
  selections, scope: force` — the real Enhancement army-wide cap).
- `Datasheet.Abilities` is not the only ability consumer. `ArmyRosterEnricher.ResolveWargearItem`
  (`src/ProbHammer.Core/Domain/Roster/ArmyRosterEnricher.cs:125`) already falls back to a linear
  scan of `datasheet.Abilities` when a wargear name isn't a `WeaponProfile` — this is how
  Impulsor's "Shield Dome" (also `type: "upgrade"`, structurally identical to the three leaking
  abilities above) resolves today. Any fix that simply stops collecting `type: "upgrade"` abilities
  would silently break Shield Dome resolution; the abilities have to stay resolvable, just not
  unconditionally surfaced.
- `Unit.Enhancements` (`src/ProbHammer.Core/Domain/Roster/Unit.cs`) is `IReadOnlyList<string>`,
  populated straight from `ParsedUnit.Enhancements` in `ArmyRosterEnricher.BuildUnit` with no
  cross-reference against the catalogue at all. Confirmed unused anywhere else in `src/` (no
  renderer or aggregator reads it) — changing its element type has no other call site to update.
- `Ability` (`src/ProbHammer.Core/Domain/Catalogue/Ability.cs`) is `{ Name, Text, Choices, Scope }`
  — no field distinguishes an intrinsic ability from an optional one, or an Enhancement from any
  other optional ability grant.

## Goals / Non-Goals

**Goals:**
- Stop any optional, player-selectable ability (Enhancement or otherwise) from appearing on a unit
  unless the parsed export actually selected it.
- Keep every optional ability resolvable by name, so the existing Shield-Dome-shaped wargear
  fallback keeps working unchanged.
- Add a real Enhancement-name-to-ability resolution step, so a selected Enhancement (e.g.
  Helbrecht's "Ferocity") carries its actual rules text instead of being an inert string.
- Give `Ability` enough provenance (an `Origin`) that a future `/LivePlay` change can render an
  Enhancement distinctly from other abilities, without re-deriving that distinction from BSData
  shape a second time.

**Non-Goals:**
- The `/LivePlay` visual treatment for Enhancements (a distinct badge/color/section) is explicitly
  out of scope for this change — only the domain-model groundwork (`Ability.Origin`) is added here.
- Validating wargear/Enhancement *eligibility* (e.g. that Sword Brethren Squad shouldn't be able to
  take an Enhancement at all under the real game rules) is out of scope, consistent with this
  project's existing "the exporting app is trusted to have already produced a valid list" stance
  (`.claude/domain-model-11e.md`'s Deliberate Omissions). This change only stops an Enhancement
  from being attached *when the export never selected it* — it does not add composition validation.
- Detachment/primary-catalogue-aware filtering (the angle first suspected while investigating
  "Thirst for Glory") is not pursued: it doesn't generalize (the Sword Brethren Squad case has
  nothing to do with catalogue identity — both leaking abilities are genuine Black Templars
  content, on the correct catalogue, simply unselected) and the fix below makes it unnecessary —
  an Enhancement no one selected never surfaces regardless of which catalogue defines it.

## Decisions

### Split `Datasheet`'s ability collection into "intrinsic" (public, materialized) and "optional" (on-demand only), mirroring `WeaponProfile` exactly
`Datasheet` gains a second private index, `_optionalAbilities: IReadOnlyDictionary<string,
Ability>`, built from a new constructor parameter, with a `TryResolveAbility(string name, out
Ability ability)` accessor — same shape as `_weaponProfiles`/`TryResolveWeaponProfile`. A new
`OptionalAbilityNames: IReadOnlyList<string>` mirrors `WeaponNames` (diagnostic enumeration for a
"did you mean" suggestion; never a full options menu). The existing public `Abilities` property is
unchanged in shape — only which abilities land there changes.

Considered keeping one flat `Abilities` list and filtering at every read site (`ArmyRosterEnricher`,
`AttachedUnitAggregator`) instead. Rejected: that pushes the same filtering logic to multiple call
sites and re-introduces exactly the kind of "full options menu" leak this change exists to close —
`WeaponProfile` already proved the on-demand-at-the-source pattern works and every consumer already
expects it for weapons.

### Classification signal: the *nearest entry's own `Type`* in the existing ancestry chain
`WalkEntry`/`WalkGroup`/`WalkInfoLink` already thread an `IReadOnlyList<BsSelectionEntry> ancestry`
(built for InSv ability resolution, see `structured-invulnerable-save`'s design.md) ending in
whichever entry most directly makes a profile reachable — for `entry.Profiles`, that's `entry`
itself (the chain always ends with the entry currently being walked); for a `"profile"`-type
`infoLink`, it's whichever entry declared that link. `ProcessProfile`'s `"Abilities"` case reads
`ancestry[^1].Type`: `"upgrade"` means optional (on-demand only); anything else (`"model"`,
`"unit"`, or unset) means intrinsic (public `Abilities` list). This needs no new ancestry-tracking
mechanism — the existing chain already univocally identifies "which entry made this profile
reachable" for every code path that calls `ProcessProfile`.

Considered instead threading a separate `bool isOptional` flag down through the recursion. Rejected
as redundant: `entry.Type` is already carried on every element of the existing ancestry list, and
reading `ancestry[^1].Type` at the one call site that needs it is simpler than adding and
propagating a new parameter through every `WalkEntry`/`WalkGroup`/`WalkLink`/`WalkInfoLink` call.

### `Ability.Origin`: `Intrinsic | Enhancement | OptionalGrant`, classified by nearest enclosing group name containing "Enhancements"
A new `AbilityOrigin` enum (`Domain/Catalogue/AbilityOrigin.cs`), added as a required field on
`Ability`. The walk threads one additional signal alongside `ancestry`: the name of the nearest
enclosing `BsSelectionEntryGroup` (set when `WalkGroup` is entered from its own `group.Name`, reset
to `null` on an `entryLink` hop — the same "a shared/reusable target isn't structurally part of
whatever linked to it" reset the ancestry list itself already applies). When an ability is
classified as optional (per the `Type == "upgrade"` rule above), it is further classified as
`Enhancement` when that nearest group name contains `"Enhancements"` (case-insensitive substring,
not exact match), else `OptionalGrant`.

Confirmed necessary as a *substring* match, not exact equality, against two real shapes: Sword
Brethren Squad's own locally-nested group is named exactly `"Enhancements"`, but Crusade Ancient
reaches its leaking ability through a group named `"Legends of Saga and Song Enhancements"` — the
*directly* enclosing group for "Thirst for Glory," not `"Enhancements"` itself (that name belongs
to an outer group one level up the chain, reached earlier via a *different* entryLink). An
exact-match rule would misclassify every such nested Enhancement sub-pool as a plain
`OptionalGrant`. This mirrors this project's existing "resolve/detect by name, not a hardcoded id"
convention (`BsdataFactionResolver`, `IsGameModeGated`'s force-entry name resolution) rather than
inventing a new one.

Considered also matching on the entry's own `constraints` shape (`max: 3, scope: "force"` — the
real Enhancement army-wide cap seen on both confirmed Enhancement entries). Rejected: this
project's `Deliberate Omissions` already commit to never interpreting `constraints` as rules data;
adding one narrow reader for classification purposes would break that boundary for a second-order
concern (a UI hint) that the group-name signal already covers.

### `ArmyRosterEnricher`'s wargear fallback repoints onto `TryResolveAbility`
`ResolveWargearItem`'s existing ability-fallback branch (`datasheet.Abilities.FirstOrDefault(...)`)
becomes `datasheet.TryResolveAbility(itemName, out var ability)`. Purely mechanical — the resolved
`Ability` (Shield Dome) is identical either way; only where it's looked up changes. The "did you
mean" suggestion source (`datasheet.WeaponNames`) is left unchanged for a wargear-name miss — a
wargear item that resolves as neither a weapon nor an optional ability is overwhelmingly likely to
be a *weapon* name typo, matching this method's existing precedent (`Absolvor` vs. `Absolver`).

### New `ArmyRosterEnricher.ResolveEnhancements` step
For each `ParsedUnit`, every name in `parsedUnit.Enhancements` (already normalized via
`BsdataNameNormalization`, matching every other name resolution in this pipeline) is resolved
against the built `Unit`'s `Datasheet.TryResolveAbility`. A miss throws
`BsdataNameResolutionException` with a `BsdataNameSuggestion.FindClosest` suggestion sourced from
`datasheet.OptionalAbilityNames` — the same "resolve by name, fail loud with a diagnostic" contract
as every other resolution step in this file. `Unit.Enhancements` becomes the resolved
`IReadOnlyList<Ability>` (**BREAKING** — see proposal.md's Impact).

Considered validating that a resolved Enhancement's `Origin` is actually `Enhancement` (rejecting a
name that resolves to some other optional ability). Rejected as unnecessary composition validation
per this change's Non-Goals — if the export names something in the Enhancements slot, the on-demand
index either has it or it doesn't; further judging *what kind* of optional ability it is would be
new eligibility-checking behavior this project has consistently declined to add elsewhere.

### `AttachedUnitAggregator` needs no change
`AggregateAbilityEntry`'s existing `Unit.Datasheet.Abilities`/`ModelLine.Abilities` walk
(`Domain/Roster/AttachedUnitAggregator.cs`) already only reads abilities already attached to a
specific component or model-line — it never re-resolves anything by name and never reads
`Unit.Enhancements` at all today (confirmed: no reference to it anywhere in `src/`). This change
does not wire `Unit.Enhancements` into the aggregate ability view or `/LivePlay` rendering; doing so
is exactly the deferred future step this change's groundwork (`Ability.Origin`) exists to make
easy, not something to build now.

## Risks / Trade-offs

- **[Risk]** The nearest-enclosing-group-name heuristic for `Enhancement` vs. `OptionalGrant`
  could misclassify a real Enhancement pool named something that doesn't contain "Enhancements" at
  all, or over-classify an unrelated optional ability sitting inside a coincidentally-named group.
  → **Mitigation**: misclassification only affects the deferred future rendering distinction, never
  whether an ability is attached — the bug this change fixes (unselected abilities leaking onto
  units) is closed regardless of which `Origin` value an optional ability gets. Real corpus
  examples can be added to a future scan if this proves too coarse in practice.
- **[Risk]** `Unit.Enhancements`'s element-type change is a breaking API change to `ProbHammer.Core`.
  → **Mitigation**: confirmed zero other call sites read it today (grepped `src/`); the only
  producer (`ArmyRosterEnricher.BuildUnit`) is updated in the same change.
- **[Trade-off]** A miss during `ResolveEnhancements` now fails the entire import where today it
  silently carried an unresolvable string through. This is the intended behavior (matching every
  other name-resolution step in this pipeline) but is a user-visible behavior change: an export
  naming an Enhancement this loader can't find (e.g. a genuine BSData spelling difference, per the
  precedent already documented for unit/weapon names) now blocks import instead of importing with
  an inert, unrendered string. Accepted as consistent with the rest of the pipeline's fail-loud
  convention rather than a silent, harder-to-notice gap.
