# Characteristic Value Domain Model

Full requirements: `openspec/changes/introduce-characteristic-domain-model/` (shape, added
unconsumed) and `unify-invulnerable-save-characteristic-view/` (first real consumer — wires
`InvulnerableSaveCharacteristicView` into `Statline.InSv`, see "Invulnerable Save Resolution" and
"Statline-Flag Rules" in statline-flag-rules.md). `CharacteristicValue`/`ScalarCharacteristicView` are
no longer unconsumed — `Statline.M`/`T`/`Sv`/`W`/`Ld`/`Oc` and `WeaponProfile.S`/`Ap`/`Bs`/`Ws` are
all `ScalarCharacteristicView` now (see "Remaining Scalar Characteristics Retyped" in
rules-glossary-and-popovers.md); every real BSData/BattleScribe mapping call site still just wraps
its parsed base value as a plain, non-caveated view, so nothing populates a real caveat on any of
these fields yet.

```
CharacteristicValue                   // Domain/Catalogue/CharacteristicValue.cs - see the class's
                                       // own doc comment (abstract/sealed NumericCharacteristicValue(int)/
                                       // DiceCharacteristicValue(DiceExpression)/
                                       // SymbolicCharacteristicValue(string) subtypes; the implicit
                                       // int/DiceExpression conversions; SymbolicCharacteristicValue's
                                       // own doc comment for why Symbol stays unconstrained). Each
                                       // subtype overrides ToString() (Value.ToString()/
                                       // Value.ToString()/Symbol) rather than the default record dump,
                                       // matching DiceExpression's own hand-tuned ToString().

CharacteristicView                    // Domain/Catalogue/CharacteristicView.cs - see the class's own
                                       // doc comment (abstract/sealed ScalarCharacteristicView/
                                       // InvulnerableSaveCharacteristicView subtypes; ContributingAbilities
                                       // as the one base constructor field). Generic CharacteristicView<T>
                                       // was considered and rejected - nothing in this domain is
                                       // actually generic *over* the wrapped type today.
  IsCaveated                          // see property's own doc comment (`DerivedValue is null`); not
                                       // a constructor field - an earlier draft stored it separately,
                                       // dropped once its redundancy with DerivedValue's own
                                       // nullability was noticed, so the two can never disagree.
  Value                                // per subtype - see SelectValue's own doc comment for the
                                       // OriginalValue/DerivedValue selection logic. Added
                                       // post-implementation (unify-invulnerable-save-characteristic-view)
                                       // after this selection was first written inline at its one call
                                       // site - not a reprise of the rejected ComputeDerivedValue
                                       // (below): needs no external classification input, computes
                                       // nothing new, same complexity class as IsCaveated itself.
```

**Pure data - computes nothing.** `DerivedValue` is always supplied directly at construction. A
first implementation pass added a `ComputeDerivedValue<T>`/per-subtype `Create`-factory mechanism,
removed after review (2026-09-04) as dishonest about what it computed and as pre-empting the real
future rule-engine design — see `introduce-characteristic-domain-model/design.md`'s Decision 2 and
"Resolved During Review" for the full reasoning trail.

`InvulnerableSaveCharacteristicView` formalizes `InvulnerableSave`'s existing melee/ranged split as
the first confirmed instance of the "compound, per-kind value shape" pattern this hierarchy allows
(a `CharacteristicView` subtype need not wrap a bare scalar). A Movement/Fly compound subtype was
raised during design but deliberately deferred — see the same design doc's "Resolved During Review"
for why; adding one later is a natural, low-risk extension point (just another `CharacteristicView`
subtype) once that shape is worked out.
