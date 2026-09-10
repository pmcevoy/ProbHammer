## Context

`AttachedUnitAggregator.BuildWeapons` groups weapons across all present model-lines by
`WeaponProfile.EqualityKey()`, which deliberately excludes Name/Range/Attacks. `AggregateWeaponEntry`
retains `Profile` (the first-inserted contributor's own `WeaponProfile`) purely for its other,
genuinely-shared identity fields (Type/Skill/S/Ap/D/keyword flags) — its own doc comment already
says `Profile.A` "is whichever contributor happened to be inserted first and is not authoritative."
`Profile.Name` has exactly the same problem today, just never previously named as such, because
nothing has needed a group's display Name to be *correct* rather than merely present.

`WeaponContribution` (the per-model-line record retained inside an `AggregateWeaponEntry` for
provenance and rendered as breakdown rows) does not carry the contributing weapon's own Name at all
— only `ComponentName`/`StatlineName`/`Count`/`PerModelAttacks`/`LoadoutIndex`.

Neither `AggregateWeaponEntry` nor `WeaponContribution` is ever persisted or serialized — both are
rebuilt fresh from the session's stored parsed army list on every request (see root `CLAUDE.md`'s
"no server-held roster state between requests"), so there is no migration or compatibility concern
for either record's shape changing.

**Correction to a claim made during this change's own exploration session**: the exploration
transcript cited `Domain.Roster.DetachmentNameResolver` as the existing joining convention to mirror.
That's wrong and should not be followed — `DetachmentNameResolver` runs in the opposite direction
(it *parses* one blob of text into separate known Detachment names via a greedy longest-match
"chomp"; it contains no name-joining/formatting logic at all). The actual existing precedent for
joining a list of names into one readable string is `AttachedUnit.Name`'s own inline switch
expression (`src/ProbHammer.Core/Domain/Roster/AttachedUnit.cs`), which already produces
"Bodyguard.Name with X", "... with X and Y", and "... with X, Y, and Z" for 1/2/3+ attached
components. That is the pattern this change actually follows.

## Goals / Non-Goals

**Goals:**
- `WeaponContribution` carries its own contributing weapon's Name.
- An aggregated weapon entry's rendered name reflects every distinct Name actually merged into it,
  not an arbitrary single contributor's Name.

**Non-Goals:**
- No change to grouping/equality behavior — which contributions merge into one entry is unchanged
  (Name already plays no role in `EqualityKey`, and continues not to).
- No named-weapon rule-effect targeting, `WeaponCharacteristicEffect`/`WeaponSelector`, or any other
  later phase from `.claude/vnext-ideas.md`'s WeaponProfile-targeting-rule-effects plan — this
  change only adds the Name tracking those phases will need, and the display fix it directly
  enables on its own.
- No corpus-frequency audit of how often a differently-named merge actually occurs in real BSData
  data today — the composite-name behavior is strictly more correct than today's arbitrary
  single-name behavior regardless of frequency, so this isn't required to justify the change (per
  the proposal's own Why).
- No change to `ShowsBreakdownTrigger`, contribution-row selection/filtering, or any caveat/marker
  rendering — untouched by this change.

## Decisions

**1. `WeaponContribution` gains a `Name` field, populated where `PerModelAttacks` already is.**
`BuildWeapons` already resolves each model-line's weapon to a real `WeaponProfile` (`profile =
unit.Datasheet.ResolveWeaponProfile(weaponName)`) before constructing that model-line's
`WeaponContribution`. Adding `Name: profile.Name` alongside the existing `PerModelAttacks:
profile.A` is a one-line addition at an already-correct point — no new resolution step.

**2. The composite display Name is computed once, in the domain layer (`BuildWeapons`), as a new
field directly on `AggregateWeaponEntry` — not recomputed at the page-rendering layer.**
This is a deliberate departure from the pattern `LivePlay.cshtml.cs`'s own comments describe for
the *contribution-breakdown* grouping ("the domain retains ungrouped, per-ModelLine provenance;
only the display grouping is page-layer work"). That precedent applies to a genuinely
display-only convenience — merging same-`(ComponentName, StatlineName)` rows so the UI isn't
overwhelmed, itself further conditional on client-side selection state. A group's own composite
Name is different in kind: it's an unconditional, structural fact about which distinct weapons the
group's `Contributions` actually represents, true regardless of any UI or selection state, directly
analogous to `TotalAttacks` (also computed once, in the domain layer, from the same `Contributions`
list). Computing it there also means every consumer (today, `_UnitBlock.cshtml`; potentially a
future named-weapon-effect resolver) reads one authoritative value instead of each recomputing the
same distinct-and-join logic independently.

Alternative considered: compute it in `LivePlay.cshtml.cs` alongside `WeaponRowViewModel`, matching
where `BuildContributionBreakdown`'s own grouping lives. Rejected for the reason above — it would
duplicate the same computation if a future weapon-effect resolver (a later phase) also needs a
group's set of distinct Names outside the rendering path.

**3. `AggregateWeaponEntry` gets a new `Name` field alongside `Profile`; `Profile` itself is
untouched.** `Profile.Name` stays exactly as it is today (an arbitrary, non-authoritative
first-inserted value) — deleting or repurposing it isn't necessary, since nothing will read it for
display once `_UnitBlock.cshtml` switches to the new field. `AggregateWeaponEntry`'s existing doc
comment (which already flags `Profile.A` as non-authoritative) is extended to flag `Profile.Name`
the same way, so a future reader doesn't rediscover this the hard way.

**4. Join algorithm**: `Contributions.Select(c => c.Name).Distinct()` (LINQ `Distinct()` preserves
first-occurrence order, which is what "first-encountered order" in the spec means — no separate
ordering step needed), then: 1 name → itself; 2 → `"{a} and {b}"`; 3+ → `string.Join(", ",
names[..^1]) + ", and " + names[^1]`. This is written as its own small helper local to
`AttachedUnitAggregator`, not extracted into a shared formatter alongside `AttachedUnit.Name`'s own
near-identical switch expression. `AttachedUnit.Name`'s version is baked into its own
"`{Bodyguard.Name} with ...`" template and used exactly once; duplicating a ~4-line join pattern for
a second, structurally different call site is smaller and clearer than extracting a shared utility
for two call sites. Revisit only if a third genuine use case appears.

## Risks / Trade-offs

- [Risk] An existing hand-built test fixture might already (even unintentionally) combine two
  differently-named, structurally-identical weapons and assert today's arbitrary single-name
  behavior. → Mitigation: `tasks.md` includes a search of existing weapon-aggregation tests for this
  shape before implementation, so any such fixture's expected name is updated deliberately, not
  discovered as an unexplained test failure.
- [Trade-off] `AggregateWeaponEntry` now carries two "name-shaped" values (`Name` and
  `Profile.Name`) where only one is meaningful for display — the same trade-off already accepted for
  `TotalAttacks`/`Profile.A` today, so this is consistent with existing precedent rather than a new
  inconsistency this change introduces.
