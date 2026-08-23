using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Examples;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;
using ProbHammer.Web.Pages;

namespace ProbHammer.Tests.Web;

public class LivePlayModelTests
{
    [Fact]
    public void OnGet_OrdersAttachedUnitsBeforePlainUnits_LargestFirstWithinEachGroup_TiesByName()
    {
        var units = LivePlayModel.BuildUnitBlocks(View.Roster());

        // Attached-sourced units (Crusader Squad x2 @ 12 models, Sword Bretheren @ 5) precede
        // plain-Unit-sourced ones (Assault Intercessor / Howling Banshees / Scout @ 5 models each,
        // Canis Rex / Impulsor / Twin-Ward Sentinel @ 1 each - the latter two added by
        // `redesign-invulnerable-save-display`). Within the tied 12-model attached pair, "High
        // Marshal Helbrecht..." sorts before "Marshal..." ascending; within the tied 5-model plain
        // trio, "Assault Intercessor" < "Howling Banshees" < "Scout" ascending; within the tied
        // 1-model plain trio, "Canis Rex" < "Impulsor" < "Twin-Ward Sentinel" ascending.
        units.Select(u => u.Name).Should().Equal(
            "Crusader Squad with High Marshal Helbrecht and Crusade Ancient",
            "Crusader Squad with Marshal and Lieutenant",
            "Sword Bretheren Squad with Marshal",
            "Assault Intercessor Squad",
            "Howling Banshees",
            "Scout Squad",
            "Canis Rex",
            "Impulsor",
            "Twin-Ward Sentinel");
    }

    [Fact]
    public void BuildUnitBlock_GroupsAdjacentSameComponentEqualValuedStatlines_SeparatesOnValueOrComponentChange()
    {
        var statlineA = new Statline(6, 4, 3, 2, 6, 2);
        var statlineB = new Statline(6, 4, 4, 2, 6, 2); // differs from A by Sv

        var view = new AttachedUnitAggregateView(
            Name: "Test Unit",
            IsAttachedUnit: true,
            Statlines:
            [
                new AggregateStatlineEntry("Squad A", "Sword Brother", statlineA, 1, 1, []),
                new AggregateStatlineEntry("Squad A", "Initiate", statlineA, 5, 5, []),
                new AggregateStatlineEntry("Squad A", "Neophyte", statlineB, 4, 4, []),
                new AggregateStatlineEntry("Squad B", "Guardian", statlineB, 1, 1, [])
            ],
            Weapons: [],
            Abilities: [],
            Keywords: new HashSet<string>());

        var block = LivePlayModel.BuildUnitBlock(view);

        // Sword Brother + Initiate share Squad A and statlineA -> one block. Neophyte shares Squad A
        // but has statlineB -> a value change starts a new block. Guardian shares statlineB with
        // Neophyte but belongs to Squad B -> a component change starts a new block even though the
        // value matches the immediately preceding entry.
        block.Statlines.Should().HaveCount(3);
        block.Statlines[0].Entries.Select(e => e.StatlineName).Should().Equal("Sword Brother", "Initiate");
        block.Statlines[1].Entries.Select(e => e.StatlineName).Should().Equal("Neophyte");
        block.Statlines[2].Entries.Select(e => e.StatlineName).Should().Equal("Guardian");
    }

    [Fact]
    public void BuildUnitBlock_RowBindsModelLineAbilities_AndSpansComponentWideAbilitiesAcrossTheirRuns()
    {
        var statlineA = new Statline(6, 4, 3, 2, 6, 2);
        var statlineB = new Statline(6, 4, 4, 2, 6, 2); // differs from A by Sv, so Neophyte gets its own run
        var rowBoundModelAbility = new Ability { Name = "Iron Halo", Text = "...", Scope = AbilityScope.Model, Origin = AbilityOrigin.Intrinsic };
        var componentWideUnitAbilityA = new Ability { Name = "Righteous Zeal", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic };
        var componentWideModelAbilityB = new Ability { Name = "SUPPORT", Text = "...", Scope = AbilityScope.Model, Origin = AbilityOrigin.Intrinsic };

        var view = new AttachedUnitAggregateView(
            Name: "Test Unit",
            IsAttachedUnit: true,
            Statlines:
            [
                new AggregateStatlineEntry("Squad A", "Sword Brother", statlineA, 1, 1, []),
                new AggregateStatlineEntry("Squad A", "Initiate", statlineA, 5, 5, []),
                new AggregateStatlineEntry("Squad A", "Neophyte", statlineB, 4, 4, []),
                new AggregateStatlineEntry("Squad B", "Guardian", statlineB, 1, 1, [])
            ],
            Weapons: [],
            Abilities:
            [
                new AggregateAbilityEntry("Squad A", "Initiate", rowBoundModelAbility),
                new AggregateAbilityEntry("Squad A", null, componentWideUnitAbilityA),
                new AggregateAbilityEntry("Squad B", null, componentWideModelAbilityB)
            ],
            Keywords: new HashSet<string>());

        var block = LivePlayModel.BuildUnitBlock(view);

        // Row-bound: "Iron Halo" is tied to "Initiate", which merged into run 0 (Sword
        // Brother/Initiate) - it appears on that run only, never on run 1 or 2.
        block.Statlines[0].ModelAbilities.Should().ContainSingle(a => a.Name == "Iron Halo");
        block.Statlines[1].ModelAbilities.Should().BeEmpty();
        block.Statlines[2].ModelAbilities.Should().BeEmpty();

        // Component-wide: Squad A occupies runs 0-1 (Sword Brother/Initiate, then Neophyte) -
        // "Righteous Zeal" spans that whole range, not just run 0.
        block.ComponentAbilitySpans.Should().ContainSingle(s =>
            s.FirstRunIndex == 0 && s.LastRunIndex == 1 && s.UnitAbilities.Single().Name == "Righteous Zeal");

        // Squad B occupies only run 2 (Guardian) - "SUPPORT" spans just that single run, routed
        // into ModelAbilities per its Scope.
        block.ComponentAbilitySpans.Should().ContainSingle(s =>
            s.FirstRunIndex == 2 && s.LastRunIndex == 2 && s.ModelAbilities.Single().Name == "SUPPORT");
    }

    [Fact]
    public void BuildContributionBreakdown_CollapsesSameNameContributions_WhenPerModelAttacksAgree()
    {
        // Real fixture: Crusader Squad's Bolt pistol (A1) is carried by the Neophyte line (x4) and
        // both differently-loadout Initiate lines (Power-fist x2 / Astartes-chainsword x3, both
        // still carrying the same Bolt pistol entry) - plus the attached Crusade Ancient's own
        // "Bolt Pistol" (x1), which is structurally EqualityKey-equal (same Skill/S/Ap/D/Pistol)
        // and so merges into the same AggregateWeaponEntry from a different component. The
        // uniform Initiate group now always emits its merged row (SelectKey null) *and* its two raw
        // per-loadout rows (SelectKey set, Label the compressed distinguishing weapon) - the raw
        // rows exist for live-play.js's selection filtering even though they stay hidden by default
        // whenever the group's loadouts currently agree (see live-play.js recomputeWeaponRow).
        var view = AttachedUnitAggregator.Build(Units.CrusaderSquad_Helbrecht_Ancient());
        var boltPistol = view.Weapons.Single(w =>
            w.Profile.Name.Equals("Bolt pistol", StringComparison.OrdinalIgnoreCase));
        var loadoutLabels = LivePlayModel.BuildLoadoutLabelLookup(view.Statlines);

        var breakdown = LivePlayModel.BuildContributionBreakdown(boltPistol, loadoutLabels);

        breakdown.Should().BeEquivalentTo(
        [
            new WeaponContributionRow("Neophyte", 4, DiceExpression.Fixed(1), DiceExpression.Fixed(4),
                "Crusader Squad::Neophyte", "Crusader Squad::Neophyte"),
            new WeaponContributionRow("Initiate", 5, DiceExpression.Fixed(1), DiceExpression.Fixed(5),
                "Crusader Squad::Initiate", null),
            new WeaponContributionRow("Initiate w/ Power fist", 2, DiceExpression.Fixed(1), DiceExpression.Fixed(2),
                "Crusader Squad::Initiate", "Crusader Squad::Initiate::0"),
            new WeaponContributionRow("Initiate w/ Astartes chainsword", 3, DiceExpression.Fixed(1), DiceExpression.Fixed(3),
                "Crusader Squad::Initiate", "Crusader Squad::Initiate::1"),
            new WeaponContributionRow("Crusade Ancient", 1, DiceExpression.Fixed(1), DiceExpression.Fixed(1),
                "Crusade Ancient::Crusade Ancient", "Crusade Ancient::Crusade Ancient")
        ]);
    }

    [Fact]
    public void BuildContributionBreakdown_ListsContributionsSeparately_WhenPerModelAttacksDisagree()
    {
        // Synthetic: EqualityKey excludes Attacks, so two contributions sharing a StatlineName
        // could in principle disagree on PerModelAttacks even though no real fixture produces this.
        // No merged row is possible for a disagreeing group - both contributions render as their
        // own raw row, exactly as before this change, now each also carrying its GroupKey/SelectKey.
        var profile = new RangedWeapon("Test Weapon", 12, DiceExpression.Fixed(1), 3, 4, 0, 1);
        var entry = new AggregateWeaponEntry(
            Profile: profile,
            TotalAttacks: DiceExpression.Fixed(7),
            Contributions:
            [
                new WeaponContribution("Squad A", "Initiate", 2, DiceExpression.Fixed(1)),
                new WeaponContribution("Squad A", "Initiate", 1, DiceExpression.Fixed(5))
            ]);

        var breakdown = LivePlayModel.BuildContributionBreakdown(entry, EmptyLoadoutLabels);

        breakdown.Should().BeEquivalentTo(
        [
            new WeaponContributionRow("Initiate", 2, DiceExpression.Fixed(1), DiceExpression.Fixed(2),
                "Squad A::Initiate", "Squad A::Initiate"),
            new WeaponContributionRow("Initiate", 1, DiceExpression.Fixed(5), DiceExpression.Fixed(5),
                "Squad A::Initiate", "Squad A::Initiate")
        ]);
    }

    [Fact]
    public void BuildContributionBreakdown_ReturnsOneRow_WhenEntryHasSingleContribution()
    {
        // A group of exactly one contribution never gets a merged row alongside it - its lone raw
        // row already is the single rendered row, matching the pre-existing "single contribution
        // still renders as one row" behavior (now also carrying its GroupKey/SelectKey).
        var profile = new RangedWeapon("Test Weapon", 12, DiceExpression.Fixed(1), 3, 4, 0, 1);
        var entry = new AggregateWeaponEntry(
            Profile: profile,
            TotalAttacks: DiceExpression.Fixed(3),
            Contributions: [new WeaponContribution("Squad A", "Sword Brother", 3, DiceExpression.Fixed(1))]);

        var breakdown = LivePlayModel.BuildContributionBreakdown(entry, EmptyLoadoutLabels);

        breakdown.Should().BeEquivalentTo(
        [
            new WeaponContributionRow("Sword Brother", 3, DiceExpression.Fixed(1), DiceExpression.Fixed(3),
                "Squad A::Sword Brother", "Squad A::Sword Brother")
        ]);
    }

    [Fact]
    public void OnGet_AttachesContributionBreakdown_ForCrusaderSquadsMergedBoltPistolRow()
    {
        var units = LivePlayModel.BuildUnitBlocks(View.Roster());

        var unit = units.Single(u => u.Name == "Crusader Squad with High Marshal Helbrecht and Crusade Ancient");
        var boltPistolRow = unit.RangedWeapons.Single(w =>
            w.Entry.Profile.Name.Equals("Bolt pistol", StringComparison.OrdinalIgnoreCase));

        boltPistolRow.Breakdown.Should().BeEquivalentTo(
        [
            new WeaponContributionRow("Neophyte", 4, DiceExpression.Fixed(1), DiceExpression.Fixed(4),
                "Crusader Squad::Neophyte", "Crusader Squad::Neophyte"),
            new WeaponContributionRow("Initiate", 5, DiceExpression.Fixed(1), DiceExpression.Fixed(5),
                "Crusader Squad::Initiate", null),
            new WeaponContributionRow("Initiate w/ Power fist", 2, DiceExpression.Fixed(1), DiceExpression.Fixed(2),
                "Crusader Squad::Initiate", "Crusader Squad::Initiate::0"),
            new WeaponContributionRow("Initiate w/ Astartes chainsword", 3, DiceExpression.Fixed(1), DiceExpression.Fixed(3),
                "Crusader Squad::Initiate", "Crusader Squad::Initiate::1"),
            new WeaponContributionRow("Crusade Ancient", 1, DiceExpression.Fixed(1), DiceExpression.Fixed(1),
                "Crusade Ancient::Crusade Ancient", "Crusade Ancient::Crusade Ancient")
        ]);
    }

    [Fact]
    public void OnGet_AttachesSingleContributorBreakdown_ForCrusaderSquadsPyrePistolRow()
    {
        // Pyre pistol is carried only by the Sword Brother (1 model, D6 attacks) - the exact
        // "it was the Sword Brother that contributed it" case: a single contributor, still shown
        // because the unit as a whole has several other ModelLines.
        var units = LivePlayModel.BuildUnitBlocks(View.Roster());

        var unit = units.Single(u => u.Name == "Crusader Squad with High Marshal Helbrecht and Crusade Ancient");
        var pyrePistolRow = unit.RangedWeapons.Single(w => w.Entry.Profile.Name == "Pyre pistol");

        pyrePistolRow.Breakdown.Should().BeEquivalentTo(
        [
            new WeaponContributionRow("Sword Brother", 1, DiceExpression.D6, DiceExpression.D6,
                "Crusader Squad::Sword Brother", "Crusader Squad::Sword Brother")
        ]);
    }

    [Fact]
    public void OnGet_HidesBreakdownTrigger_ForAnyWeapon_WhenUnitHasOnlyOneModelLineTotal()
    {
        // Impulsor is a single-model, single-ModelLine Unit - there's no second source any of its
        // weapons could be distinguished from, so no weapon on it should ever show a clickable
        // expand trigger. Breakdown itself still has one raw row per weapon (its own SelectKey) -
        // that data isn't gated by ShowsBreakdownTrigger, since selection-scoped filtering still
        // needs it even for a unit with only one addressable statline.
        var units = LivePlayModel.BuildUnitBlocks(View.Roster());

        var unit = units.Single(u => u.Name == "Impulsor");

        unit.RangedWeapons.Should().OnlyContain(w => !w.ShowsBreakdownTrigger && w.Breakdown.Count == 1);
        unit.MeleeWeapons.Should().OnlyContain(w => !w.ShowsBreakdownTrigger && w.Breakdown.Count == 1);
    }

    [Fact]
    public void CompressLoadoutLabels_CompressesCrusaderSquadInitiateLoadouts_ToDistinguishingWeaponsOnly()
    {
        // Real fixture: both Initiate loadouts also carry Bolt pistol and Heavy Bolt pistol, so
        // only the weapon that actually differs (Power fist / Astartes chainsword) should render.
        var view = AttachedUnitAggregator.Build(Units.CrusaderSquad_Helbrecht_Ancient());
        var initiate = view.Statlines.Single(s => s.StatlineName == "Initiate");

        var labels = LivePlayModel.CompressLoadoutLabels(initiate.Loadouts);

        labels.Should().Equal("Power fist", "Astartes chainsword");
    }

    [Fact]
    public void CompressLoadoutLabels_ExcludesSharedWeapons_RegardlessOfListPosition()
    {
        // Shared weapons ("Chainsword", "Frag grenades") appear at different positions in each
        // loadout's list - proves a true multiset intersection, not a common-prefix strip.
        var loadouts = new[]
        {
            new ModelLineLoadout("", ["Bolt pistol", "Chainsword", "Frag grenades"], 1, 1),
            new ModelLineLoadout("", ["Chainsword", "Plasma pistol", "Frag grenades"], 1, 1)
        };

        var labels = LivePlayModel.CompressLoadoutLabels(loadouts);

        labels.Should().Equal("Bolt pistol", "Plasma pistol");
    }

    [Fact]
    public void CompressLoadoutLabels_KeepsAGenuinelyExtraCopy_OfAnOtherwiseSharedWeapon()
    {
        // Loadout A carries two Chainswords, loadout B carries one - the shared multiset only
        // covers one copy, so A's second copy is genuinely distinguishing and must survive.
        var loadouts = new[]
        {
            new ModelLineLoadout("", ["Chainsword", "Chainsword"], 1, 1),
            new ModelLineLoadout("", ["Chainsword"], 1, 1)
        };

        var labels = LivePlayModel.CompressLoadoutLabels(loadouts);

        labels.Should().Equal("Chainsword", "");
    }

    [Fact]
    public void CompressLoadoutLabels_LeavesASingleLoadoutStatlineUnaffected()
    {
        var loadouts = new[] { new ModelLineLoadout("Bolt pistol, Chainsword", ["Bolt pistol", "Chainsword"], 1, 1) };

        var labels = LivePlayModel.CompressLoadoutLabels(loadouts);

        labels.Should().Equal("Bolt pistol, Chainsword");
    }

    [Fact]
    public void CasualtyKey_ForASingleLoadoutEntry_PrependsUnitIndexToSelectKey()
    {
        var key = LivePlayModel.CasualtyKey(0, "Crusader Squad", "Neophyte", -1);

        key.Should().Be("0::Crusader Squad::Neophyte");
    }

    [Fact]
    public void CasualtyKey_ForALoadoutLine_IncludesTheLoadoutIndex()
    {
        var key = LivePlayModel.CasualtyKey(2, "Crusader Squad", "Initiate", 1);

        key.Should().Be("2::Crusader Squad::Initiate::1");
    }

    [Fact]
    public void RebuildRoster_WithNoAdjustments_MatchesThePristineSortedRoster()
    {
        var pristine = LivePlayModel.SortRoster(View.MyArmyRoster()).Select(AttachedUnitAggregator.Build).ToList();

        var rebuilt = LivePlayModel.RebuildRoster(View.MyArmyRoster(),[]);

        rebuilt.Select(v => v.Name).Should().Equal(pristine.Select(v => v.Name));
    }

    [Fact]
    public void RebuildRoster_AppliesAnAdjustment_ToTheSingleModelLineItAddresses()
    {
        // Index 0 in the sorted roster is "Crusader Squad with High Marshal Helbrecht and Crusade
        // Ancient" (see OnGet_OrdersAttachedUnitsBeforePlainUnits...); Neophyte is a single-ModelLine
        // statline (count 4) on the Crusader Squad bodyguard, so LoadoutIndex is the -1 sentinel.
        var adjustment = new CasualtyAdjustment(new CasualtyCoordinate(0, "Crusader Squad", "Neophyte", -1), RemainingCount: 2);

        var rebuilt = LivePlayModel.RebuildRoster(View.MyArmyRoster(),[adjustment]);

        var unit = rebuilt[0];
        unit.Name.Should().Be("Crusader Squad with High Marshal Helbrecht and Crusade Ancient");
        unit.Statlines.Single(s => s.StatlineName == "Neophyte").RemainingCount.Should().Be(2);
    }

    [Fact]
    public void RebuildRoster_AppliesAnAdjustment_ToASpecificLoadout()
    {
        // Initiate has two loadouts on the Crusader Squad bodyguard (Power fist at index 0,
        // Astartes chainsword at index 1 - see AttachedUnitAggregatorTests' LoadoutIndex tests).
        var adjustment = new CasualtyAdjustment(new CasualtyCoordinate(0, "Crusader Squad", "Initiate", 0), RemainingCount: 0);

        var rebuilt = LivePlayModel.RebuildRoster(View.MyArmyRoster(),[adjustment]);

        var initiate = rebuilt[0].Statlines.Single(s => s.StatlineName == "Initiate");
        initiate.Loadouts[0].RemainingCount.Should().Be(0); // Power fist loadout, fully removed
        initiate.Loadouts[1].RemainingCount.Should().Be(3); // Astartes chainsword loadout, unaffected
        initiate.RemainingCount.Should().Be(3); // summed across loadouts
    }

    [Fact]
    public void RebuildRoster_ClampsAnOutOfRangeRemainingCount_RatherThanThrowing()
    {
        var adjustment = new CasualtyAdjustment(new CasualtyCoordinate(0, "Crusader Squad", "Neophyte", -1), RemainingCount: 99);

        var rebuilt = LivePlayModel.RebuildRoster(View.MyArmyRoster(),[adjustment]);

        rebuilt[0].Statlines.Single(s => s.StatlineName == "Neophyte").RemainingCount.Should().Be(4); // clamped at Count
    }

    [Theory]
    [InlineData(99, "Crusader Squad", "Neophyte", -1)] // out-of-range UnitIndex
    [InlineData(0, "No Such Component", "Neophyte", -1)] // unknown ComponentName
    [InlineData(0, "Crusader Squad", "No Such Statline", -1)] // unknown StatlineName
    [InlineData(0, "Crusader Squad", "Initiate", 99)] // out-of-range LoadoutIndex
    public void RebuildRoster_IgnoresAnAdjustment_ThatDoesNotResolveToARealModelLine(
        int unitIndex, string componentName, string statlineName, int loadoutIndex)
    {
        var adjustment = new CasualtyAdjustment(
            new CasualtyCoordinate(unitIndex, componentName, statlineName, loadoutIndex), RemainingCount: 0);

        var act = () => LivePlayModel.RebuildRoster(View.MyArmyRoster(),[adjustment]);

        act.Should().NotThrow();
        var pristine = LivePlayModel.SortRoster(View.MyArmyRoster()).Select(AttachedUnitAggregator.Build).ToList();
        act().Select(v => v.Statlines.Sum(s => s.RemainingCount))
            .Should().Equal(pristine.Select(v => v.Statlines.Sum(s => s.RemainingCount)));
    }

    [Fact]
    public void BuildUnitBlock_WithUnit_ComputesSingleModelAndHalfStrengthAndBattleShockedFlags()
    {
        var unit = AttachedUnitFixtures.LeaderUnit(); // single-model
        unit.IsHalfStrengthOverride = true;
        unit.IsBattleShocked = true;
        var view = AttachedUnitAggregator.Build(unit);

        var block = LivePlayModel.BuildUnitBlock(view, unit);

        block.IsSingleModelUnit.Should().BeTrue();
        block.IsAtOrBelowHalfStrength.Should().BeTrue(); // the player-set override, for a single-model unit
        block.IsBattleShocked.Should().BeTrue();
    }

    [Fact]
    public void BuildUnitBlock_WithUnit_MultiModelUnit_UsesTheComputedHalfStrengthDetermination()
    {
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader(); // 5 full-health models
        unit.IsHalfStrengthOverride = true; // never read for a multi-model unit
        var view = AttachedUnitAggregator.Build(unit);

        var block = LivePlayModel.BuildUnitBlock(view, unit);

        block.IsSingleModelUnit.Should().BeFalse();
        block.IsAtOrBelowHalfStrength.Should().BeFalse();
    }

    [Fact]
    public void RebuildRosterWithStatus_AppliesAStatusAdjustment_ToOnlyTheAddressedUnit()
    {
        var statusAdjustment = new UnitStatusAdjustment(0, IsHalfStrength: false, IsBattleShocked: true);

        var rebuilt = LivePlayModel.RebuildRosterWithStatus(View.MyArmyRoster(), [], [statusAdjustment]);

        rebuilt[0].Unit.IsBattleShocked.Should().BeTrue();
        rebuilt.Skip(1).Should().OnlyContain(x => !x.Unit.IsBattleShocked);
    }

    [Fact]
    public void RebuildRosterWithStatus_LeavesStatusUnset_WhenNoAdjustmentAddressesAnyUnit()
    {
        var rebuilt = LivePlayModel.RebuildRosterWithStatus(View.MyArmyRoster(), [], []);

        rebuilt.Should().OnlyContain(x => !x.Unit.IsBattleShocked && !x.Unit.IsHalfStrengthOverride);
    }

    [Fact]
    public void RebuildRosterWithStatus_ABatchOfBothKinds_AppliesBothIndependently()
    {
        var casualtyAdjustment = new CasualtyAdjustment(new CasualtyCoordinate(0, "Crusader Squad", "Neophyte", -1), RemainingCount: 2);
        var statusAdjustment = new UnitStatusAdjustment(0, IsHalfStrength: false, IsBattleShocked: true);

        var rebuilt = LivePlayModel.RebuildRosterWithStatus(View.MyArmyRoster(), [casualtyAdjustment], [statusAdjustment]);

        rebuilt[0].View.Statlines.Single(s => s.StatlineName == "Neophyte").RemainingCount.Should().Be(2);
        rebuilt[0].Unit.IsBattleShocked.Should().BeTrue();
    }

    // Synthetic BuildContributionBreakdown tests construct WeaponContribution directly, with no
    // backing AggregateStatlineEntry/Loadouts data to look a compressed label up from - an empty
    // lookup falls back to each contribution's own StatlineName (GetValueOrDefault), matching what
    // these tests already expect for their Label assertions.
    private static readonly IReadOnlyDictionary<(string ComponentName, string StatlineName, int LoadoutIndex), string>
        EmptyLoadoutLabels = new Dictionary<(string, string, int), string>();
}
