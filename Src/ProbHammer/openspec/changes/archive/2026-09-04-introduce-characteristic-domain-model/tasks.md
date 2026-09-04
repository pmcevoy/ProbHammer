## 1. `CharacteristicValue` hierarchy

- [x] 1.1 Add abstract `CharacteristicValue` record to `ProbHammer.Core/Domain/Catalogue/` (file
      location: `CharacteristicValue.cs`), with sealed subtypes `NumericCharacteristicValue(int
      Value)`, `DiceCharacteristicValue(DiceExpression Value)`, `SymbolicCharacteristicValue(string
      Symbol)`.
- [x] 1.2 Add an implicit `int` → `CharacteristicValue` conversion operator on the abstract base,
      producing a `NumericCharacteristicValue` — mirrors `DiceExpression`'s/`InvulnerableSave`'s
      existing implicit-int-conversion convention.
- [x] 1.2a (added post-review) Add a symmetric implicit `DiceExpression` → `CharacteristicValue`
      conversion operator, producing a `DiceCharacteristicValue` — the int conversion alone was
      asymmetric with `DiceCharacteristicValue`'s own existence.
- [x] 1.2b (added post-review) Override `ToString()` on all three subtypes (`Value.ToString()`,
      `Value.ToString()`, `Symbol` respectively), matching `DiceExpression`'s own hand-tuned
      `ToString()` convention rather than the default record dump.
- [x] 1.3 Unit tests: each subtype constructs and round-trips its wrapped value; the implicit `int`
      and `DiceExpression` conversions each produce the expected subtype/value; the three subtypes
      are structurally distinct (a `DiceCharacteristicValue` is never equal to a
      `NumericCharacteristicValue` even with an equivalent expected value, etc.); each subtype's
      `ToString()` renders its plain value/symbol.

## 2. `CharacteristicView` hierarchy

- [x] 2.1 Add abstract `CharacteristicView` record to `ProbHammer.Core/Domain/Catalogue/` (file
      location: `CharacteristicView.cs`), carrying `ContributingAbilities`
      (`IReadOnlyList<Ability>`) as its one constructor field, plus an `abstract bool IsCaveated { get; }`
      property (no constructor field for it).
- [x] 2.2 Add sealed `ScalarCharacteristicView` subtype with `OriginalValue`/`DerivedValue` typed as
      `CharacteristicValue`/`CharacteristicValue?`, overriding `IsCaveated => DerivedValue is null`.
- [x] 2.3 Add sealed `InvulnerableSaveCharacteristicView` subtype with `OriginalValue`/`DerivedValue`
      typed as `InvulnerableSave`/`InvulnerableSave?`, overriding `IsCaveated => DerivedValue is null`.
- [x] ~~2.4 Add a shared private/internal static helper that computes the derived value...~~
      **Reverted post-implementation** (2026-09-04, discussed with the user): a first pass added
      `ComputeDerivedValue<T>` plus per-subtype `Create` factories; removed after review because (a)
      with no modification/legality engine, the helper could only ever echo `OriginalValue` back or
      return `null` — not actually derive anything — and (b) its `Func<Ability, bool>` classifier
      parameter pre-empted the real future design (a `StatlineFlagRule`-style catalogued engine
      external to this value type). `CharacteristicView` is pure data: `DerivedValue` is always
      supplied directly at construction by whatever external code builds the view. See design.md's
      Decision 2 ("`CharacteristicView` is pure data") and "Resolved During Review" for the full
      reasoning.
- [x] 2.5 Unit tests per subtype, covering: `OriginalValue`/`DerivedValue`/`ContributingAbilities`
      round-trip through the constructor; a non-null `DerivedValue` → `IsCaveated` false; a null
      `DerivedValue` → `IsCaveated` true.

## 3. Documentation

- [x] 3.1 Add a "Characteristic Value Domain Model" section to `.claude/domain-model-11e.md`
      documenting `CharacteristicValue`/`CharacteristicView` and their subtypes, cross-referencing
      this change and noting they are unconsumed (no existing type wired to them yet).
