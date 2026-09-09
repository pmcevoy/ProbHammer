# BattleScribe/NewRecruit JSON Import Pipeline

Full requirements: `openspec/changes/import-battlescribe-json-rosters/`. A second, independent
import pipeline: `BattleScribe roster JSON text → BattleScribeRosterFormat.TryParse → BsRoster
(session-stored as a BattleScribeArmyImport) → BattleScribeRosterMapper.Map → ArmyRoster`, with no
`Domain.Catalogue.Bsdata`/BSData catalogue involvement anywhere — every
`Datasheet`/`Statline`/`WeaponProfile`/`Ability` is synthesized directly from the roster JSON's
own already-resolved `profiles`/`rules`, since (unlike a GW-app text export) BattleScribe/NewRecruit
JSON already carries every characteristic, weapon profile, and rule text fully resolved inline.
Serves NewRecruit-only users (whose per-army-book content in the official GW app is paywalled)
directly, and gives Android GW-app users a reliable alternative when the native text export hits
one of that pipeline's own still-open ambiguities: export from GW app, clean up in NewRecruit,
export as JSON. Analyzed against exactly one real captured sample
(`data/gw-app-export-templars.json`) — several shapes below remain UNVERIFIED pending a second,
independently-sourced sample.

Namespace: `ProbHammer.Core.Domain.Import.BattleScribe`, JSON DTOs in its own `Json`
sub-namespace.

```
Json/BsRosterFile.cs                  // System.Text.Json-backed types for the subset of the
                                       // BattleScribe rosterSchema this pipeline reads - BsRoster
                                       // (Xmlns, Name, Costs, CostLimits, Forces), BsRosterForce
                                       // (CatalogueName, Rules, Selections), BsRosterSelection (Id,
                                       // Name, Type, Number, Profiles, Rules, Costs, Selections,
                                       // Associations - the same shape reused at every nesting
                                       // level), BsRosterProfile (+ CharacteristicText(name)),
                                       // BsRosterCharacteristic ($text via [JsonPropertyName]),
                                       // BsRosterCost (Name, Value - shared shape for roster totals
                                       // and a selection's own cost tags, e.g. "Enhancements",
                                       // "Detachment Points"), BsRosterAssociation (Type, To, Name).
                                       // Only what this pipeline needs is modeled; everything else
                                       // silently ignored on deserialize.

BattleScribeRosterFormat.TryParse(text, out BsRoster?) -> bool
                                       // see class's and method's own doc comments. Uses a
                                       // CamelCase/case-insensitive JsonSerializerOptions, matching
                                       // BsdataCatalogueReader's own convention.

BattleScribeRosterParseException(message)
                                       // see class's own doc comment
```

**`BattleScribeRosterMapper.Map(BsRoster) -> ArmyRoster`**: army metadata comes from
`roster.costs`/`costLimits`' own "pts" entry (PointsSpent/PointsLimit) and from splitting
`force.catalogueName` on " - " (e.g. "Imperium - Adeptus Astartes - Black Templars" -> 3 segments)
for Faction — not required to match `BsdataFactionResolver`'s own suffix-matching convention, since
that resolver is never invoked here. Detachments/ForceDisposition/BattleSize come from the
"Detachment"/"Force Disposition"/"Battle Size" top-level selections' own nested selection name(s);
BattleSize additionally strips a trailing parenthetical (e.g. "Incursion (1000 Point limit)" ->
"Incursion", matching the text pipeline's convention despite different parenthetical wording).

Attachment resolution (`BuildUnits`): every top-level "unit"/"model" selection's own outgoing
`associations[].name` in `("Leading","Supporting")` targets another top-level selection by id — the
target becomes a Bodyguard, every selection targeting it becomes Attached, and a selection with
neither association is standalone. Confirmed against the sample's three real associations (two
2-member groups, one Leader-only group) — a 3-member group remains UNVERIFIED pending a second
sample.

Unit assembly (`BuildUnit`): see the method's own doc comment for the exact wiring (Intrinsic vs.
CoreRule/ArmyRule origin, `FindEnhancements`' whole-tree walk). Origin uses the same
`ArmyRuleNameLookup.Resolve(Faction).Contains(rule.Name)` check the BSData pipeline uses, so
`AttachedUnitAggregator.PromoteArmyRuleAbilities`'s cross-component dedup (e.g. "Templar Vows")
applies here too. Deliberately NO chapter/mode gating — a roster JSON only ever contains rules
already applicable to the exported army, and structurally cannot leak an unselected Enhancement,
since it only contains actual selections.

ModelLine assembly (`BuildModelLines`): see the method's own doc comment for the exact wiring and
real-data examples (Sword Brethren's shared-statline fallback, Helbrecht/Impulsor's solo-model
case). No partition inference is ever needed — each already-split loadout node carries its own
count directly (this pipeline structurally cannot reproduce `harden-army-list-parsing-for-android-
exports`'s still-open Custodian Guard case).

Weapon/ability collection (`CollectWeaponsAndAbilities`): see the method's own doc comment for the
exact wiring and the Storm-Bolters-style wrapper example — dividing a weapon-carrying child's own
`number` by the loadout's model count recovers the per-model quantity; an "Abilities"-typeName
child becomes an OptionalGrant-origin Ability, mirroring the BSData pipeline's equivalent
classification.

Characteristic-text parsing reuses `DiceExpression.Parse` (A/D) and `WeaponKeywordParser.Apply`
(Keywords) directly — this format's own text shapes are byte-for-byte the same convention BSData
uses. Plain-int/threshold/measurement parsing carries small, functionally-identical local
equivalents rather than reusing `BsdataDatasheetMapper`'s own private (inaccessible) helpers.
Invulnerable-save caveat resolution is this format's own simpler resolution (no BSData-style
entryLink ancestry chain to walk) — UNVERIFIED, since the one real sample analyzed has no caveated
InSv at all. Since unify-characteristic-effect-resolution, it never attempts to interpret the
linked ability's own Text either (mirroring `BsdataDatasheetMapper.ResolveInvulnerableSave`'s
identical simplification) — a footnoted/split value always resolves to `Caveated(fallbackValue,
ability)`, deferring real resolution to `AttachedUnitAggregator`'s own Build-time baseline lookup,
exactly mirroring the BSData pipeline's own wiring (invulnerable-save's "This resolution behavior
SHALL be identical regardless of which import pipeline produced the Ability being matched").

This pipeline's own ability extraction (Statline/Weapon/Ability Extraction above and Core Rule
Extraction in bsdata-json-ingestion.md) is subject to the same "Leader"/"Support"/"Attached Unit" exclusion as the BSData
pipeline, via the same central `Datasheet` constructor filter (`resolve-known-ability-effects`) —
`BuildUnit`'s `intrinsicAbilities`/`coreRuleAbilities` are concatenated and passed straight into
`new Datasheet(...)`, the same single injection point the BSData pipeline funnels through, so no
separate filter is needed here.

```
BattleScribeRuleGlossaryBuilder.Build(BsRoster) -> RuleGlossary
                                       // see class's own doc comment

RuleGlossary.BuildFrom(IEnumerable<RuleDefinition>) -> RuleGlossary
                                       // see method's own doc comment
```

`StoredArmyImport`/`TextArmyImport`/`BattleScribeArmyImport` live in `Domain.Import` itself — see
"Session-Backed Import" in army-list-import-pipeline.md.
