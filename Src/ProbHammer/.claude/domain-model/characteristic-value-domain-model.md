# Characteristic Value Domain Model

Full requirements: `openspec/changes/introduce-characteristic-domain-model/` (shape, added
unconsumed) and `unify-invulnerable-save-characteristic-view/` (first real consumer — wires
`InvulnerableSaveCharacteristicView` into `Statline.InSv`, see "Invulnerable Save Resolution" and
"Statline-Flag Rules" in statline-flag-rules.md). `CharacteristicValue`/`ScalarCharacteristicView` remain unconsumed —
`WeaponProfile.S`/`Ap` and `Statline`'s other five fields are still plain `int`; wiring those in is
a deliberately separate, not-yet-scoped follow-up.

```
CharacteristicValue                   // Domain/Catalogue/CharacteristicValue.cs - abstract, sealed
                                       // NumericCharacteristicValue(int)/
                                       // DiceCharacteristicValue(DiceExpression)/
                                       // SymbolicCharacteristicValue(string) subtypes (a "-"/"*"/
                                       // "N/A" characteristic, Symbol deliberately unconstrained -
                                       // not validated against a closed set, since real BSData
                                       // source text isn't guaranteed to be limited to those three
                                       // examples) - mirrors WeaponProfile's own abstract-base/
                                       // sealed-subtype shape rather than a Kind-tagged single
                                       // record, so a value can never carry more than one kind at
                                       // once or none at all. Gains implicit int and DiceExpression
                                       // conversions on the abstract base (-> NumericCharacteristicValue/
                                       // DiceCharacteristicValue respectively), mirroring
                                       // DiceExpression's/InvulnerableSave's own implicit-int-
                                       // conversion convention, kept symmetric across both numeric-
                                       // ish kinds. Each subtype overrides ToString() (Value.ToString()/
                                       // Value.ToString()/Symbol) rather than the default record dump,
                                       // matching DiceExpression's own hand-tuned ToString().

CharacteristicView                    // Domain/Catalogue/CharacteristicView.cs - abstract, sealed
                                       // ScalarCharacteristicView/InvulnerableSaveCharacteristicView
                                       // subtypes, not a generic CharacteristicView<T> (considered
                                       // and rejected - nothing in this domain is actually generic
                                       // *over* the wrapped type today). Base carries
                                       // ContributingAbilities (IReadOnlyList<Ability>) as its one
                                       // constructor field; each subtype adds its own typed
                                       // OriginalValue/DerivedValue (CharacteristicValue vs.
                                       // InvulnerableSave's melee/ranged pair), since those
                                       // genuinely differ in shape per kind.
  IsCaveated                          // abstract property, overridden per subtype as
                                       // `DerivedValue is null` - not a constructor field (an
                                       // earlier draft stored it separately; dropped once its
                                       // redundancy with DerivedValue's own nullability was
                                       // noticed, so the two can never disagree).
  Value                                // per subtype, not on the abstract base (same reason
                                       // OriginalValue/DerivedValue aren't) - `IsCaveated ?
                                       // OriginalValue : DerivedValue!`, the value a caller should
                                       // actually use/display. Added post-implementation
                                       // (unify-invulnerable-save-characteristic-view) after this
                                       // selection was first written inline at its one call site -
                                       // not a reprise of the rejected ComputeDerivedValue (below):
                                       // needs no external classification input, computes nothing
                                       // new, same complexity class as IsCaveated itself.
```

**Pure data - computes nothing.** `DerivedValue` is always supplied directly at construction, by
whatever external code builds the view; `CharacteristicView` has no method that classifies
abilities or computes a mutated value. A first implementation pass added a shared
`ComputeDerivedValue<T>` helper plus per-subtype `Create` factories that echoed `OriginalValue`
back when a caller-supplied `Func<Ability, bool>` classifier deemed every contributor "understood" -
removed after review (2026-09-04): with no modification/legality engine, that helper could only
ever return `OriginalValue` unchanged or `null`, which isn't actually deriving anything, and its
`Func` classifier parameter pre-empted the real future design discussed with the user - a
`StatlineFlagRule`-style catalogued rule engine (`Matches(Ability)` + apply logic, held in a closed
list, applied by an aggregator step) external to this value type, mirroring how `Statline` stays
pure data and `StatlineFlagRuleCatalogue`/`AttachedUnitAggregator` own the mutation logic instead.
That future engine is also the natural place to finally address `StatlineFlagRule`'s own missing
ordering/stacking/cap logic (`.claude/vnext-ideas.md`) - see
`introduce-characteristic-domain-model/design.md`'s Decision 2 and "Resolved During Review" for the
full reasoning trail.

`InvulnerableSaveCharacteristicView` formalizes `InvulnerableSave`'s existing melee/ranged split as
the first confirmed instance of the "compound, per-kind value shape" pattern this hierarchy allows
(a `CharacteristicView` subtype need not wrap a bare scalar). A Movement/Fly compound subtype (a
value plus a minimum-move component) was raised during design but deliberately deferred — its exact
fields aren't designed yet; adding one is a natural, low-risk extension point (just another
`CharacteristicView` subtype) once that shape is worked out.
