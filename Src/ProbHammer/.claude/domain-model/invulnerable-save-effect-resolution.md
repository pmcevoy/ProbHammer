# Invulnerable-Save Effect Resolution

Full requirements: `openspec/changes/resolve-invulnerable-save-effects/`. The InSv-specific
counterpart to `characteristic-modification-kind`'s own scalar resolution — resolves a classified
`InvulnerableSaveCharacteristicEffect` (see "Rule Effect Classification (Text-Only)" in
rule-effect-classification.md) plus its
source `Ability` into a real, displayable `InvulnerableSaveCharacteristicView`, proven correct in
isolation via the exact same not-yet-consumed discipline that component established.

```
InvulnerableSaveEffectResolver.Resolve(effect, sourceAbility, current)
    -> InvulnerableSaveCharacteristicView
                                       // Domain/Catalogue/InvulnerableSaveEffectResolver.cs - see
                                       // class's and method's own doc comments
```

**Ground-truth verified, not just unit-tested**: resolving the Effect classified from Shield Dome's
own real Name+Text ("The bearer has a 5+ invulnerable save.") against Shield Dome's own `Ability`
reproduces the exact hand-computed result the now-retired `ShieldDomeStatlineFlagRule.Apply` used to
produce (`IsCaveated`/`OriginalValue`/`DerivedValue`/`ContributingAbilities` all compared
field-by-field, not via whole-record equality - mirrors every other test in this codebase touching
`InvulnerableSaveCharacteristicView`, which consistently avoids that in favor of explicit field
assertions) — proving the general resolver was at least as correct as the specific hand-authored
rule it went on to replace.

**Now the runtime consumer** (`apply-rule-effect-baseline`) — see the class's own doc comment; also
"Statline-Flag Rules" in statline-flag-rules.md. `StatlineFlagRule`/
`ShieldDomeStatlineFlagRule`/`StatlineFlagRuleCatalogue` are deleted; this resolver, run from the
checked-in baseline, is the only thing producing a real `InvulnerableSaveCharacteristicView` on a
live roster now. Confirmed byte-for-byte equivalent to the retired hand-authored rule both by the
ground-truth test above (now comparing against a hand-computed expected value rather than a call to
the deleted type) and by a real captured export's Impulsor/Shield Dome rendering identically
pre- and post-migration.
