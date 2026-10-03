# Invulnerable-Save Effect Resolution

Full requirements: `openspec/changes/resolve-invulnerable-save-effects/`. The InSv-specific
counterpart to `characteristic-modification-kind`'s own scalar resolution — resolves a classified
`InvulnerableSaveCharacteristicEffect` (see ability-classification-catalogue.md) plus its
source `Ability` into a real, displayable `InvulnerableSaveCharacteristicView`, proven correct in
isolation via the exact same not-yet-consumed discipline that component established.

```
InvulnerableSaveEffectResolver.Resolve(effect, sourceAbility, current)
    -> InvulnerableSaveCharacteristicView
                                       // Domain/Catalogue/InvulnerableSaveEffectResolver.cs - see
                                       // class's and method's own doc comments
  .ResolveCaveat(effect, sourceAbility, current)   // a footnoted InSv, from its linked ability
  .Merge(effect, sourceAbility, current)           // a present ability granting an InSv
```

**Two callers, two semantics** (`adopt-llm-ability-classifications` 7.2.1). A present ability's
grant is **merged**: better (lower) save per side, `0` never overrides, and unchanged (not credited)
when it improves neither side, so a one-sided grant like Kustom Force Field's ranged 4+ can't wipe an
existing melee save. A caveat footnote instead **defines** the save: the side it names takes its
value; the side it leaves `0` is "no save" for a bare footnote (`4+*`, Judiciar, stored as 4/4) but
keeps the plain value for a split one (`4+* / 5+`, Howling Banshees/Wyches, stored as 5/5).
Real-corpus tests cover both footnote shapes.

**Ground-truth verified, not just unit-tested**: resolving the Effect classified from Shield Dome's
own real text ("The bearer has a 5+ invulnerable save.") against Shield Dome's own `Ability`
reproduces the exact hand-computed result the now-retired `ShieldDomeStatlineFlagRule.Apply` used to
produce (`IsCaveated`/`OriginalValue`/`DerivedValue`/`ContributingAbilities` all compared
field-by-field, not via whole-record equality - mirrors every other test in this codebase touching
`InvulnerableSaveCharacteristicView`, which consistently avoids that in favor of explicit field
assertions) — proving the general resolver was at least as correct as the specific hand-authored
rule it went on to replace.

**Now the runtime consumer** (`apply-rule-effect-baseline`) — see the class's own doc comment; also
"Statline-Flag Rules" in statline-flag-rules.md. `StatlineFlagRule`/
`ShieldDomeStatlineFlagRule`/`StatlineFlagRuleCatalogue` are deleted; this resolver, run from the
ability-classification catalogue, is the only thing producing a real `InvulnerableSaveCharacteristicView` on a
live roster now. Confirmed byte-for-byte equivalent to the retired hand-authored rule both by the
ground-truth test above (now comparing against a hand-computed expected value rather than a call to
the deleted type) and by a real captured export's Impulsor/Shield Dome rendering identically
pre- and post-migration.
