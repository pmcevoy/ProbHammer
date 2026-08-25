using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Import.BattleScribe;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Import.BattleScribe;

/// <summary>Fixture-based unit tests for <see cref="BattleScribeRosterMapper"/> against a trimmed
/// real excerpt of <c>data/gw-app-export-templars.json</c> (see Fixtures/templars-excerpt.json -
/// same "trimmed real excerpt" convention as <c>tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/</c>),
/// covering: attachment resolution (2-member and Leader-only real shapes), the pre-split
/// model-group shape, a multi-profile weapon (Sword of the High Marshals), an Enhancement
/// (Oathbound Exemplar), a Core Rule (Templar Vows), the shared-loadout-wrapper statline fallback
/// (Sword Brethren Squad), and a wrapper-group weapon quantity (Impulsor's "2 Storm Bolters").</summary>
public class BattleScribeRosterMapperTests
{
    private static string ReadFixture([CallerFilePath] string here = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "Fixtures", "templars-excerpt.json"));

    private static ArmyRoster BuildRoster()
    {
        BattleScribeRosterFormat.TryParse(ReadFixture(), out var roster).Should().BeTrue();
        return BattleScribeRosterMapper.Map(roster!);
    }

    [Fact]
    public void RealExcerpt_ParsesArmyMetadata()
    {
        var army = BuildRoster();

        army.PointsSpent.Should().Be(915);
        army.PointsLimit.Should().Be(1000);
        army.Faction.Should().Equal("Imperium", "Adeptus Astartes", "Black Templars");
        army.Detachments.Should().Equal("Companions of Vehemence");
        army.ForceDisposition.Should().Be("Purge the Foe");
        army.BattleSize.Should().Be("Incursion");
    }

    [Fact]
    public void TwoMemberAttachmentGroup_ResolvesBodyguardAndBothAttachedMembers()
    {
        var army = BuildRoster();

        var groups = army.Units.OfType<AttachedUnit>().Where(u => u.Bodyguard.Name == "Crusader Squad").ToList();
        var group = groups.Should().ContainSingle(g => g.Attached.Count == 2).Subject;

        group.Attached.Select(u => u.Name).Should().BeEquivalentTo("Lieutenant", "Marshal");
    }

    [Fact]
    public void LeaderOnlyAttachmentGroup_ResolvesWithOneAttachedMemberAndNoError()
    {
        var army = BuildRoster();

        var groups = army.Units.OfType<AttachedUnit>().Where(u => u.Bodyguard.Name == "Crusader Squad").ToList();
        var group = groups.Should().ContainSingle(g => g.Attached.Count == 1).Subject;

        group.Attached.Should().ContainSingle(u => u.Name == "High Marshal Helbrecht");
    }

    [Fact]
    public void StandaloneUnit_HasNoAssociations()
    {
        var army = BuildRoster();

        army.Units.OfType<Unit>().Should().ContainSingle(u => u.Name == "Impulsor");
    }

    [Fact]
    public void PreSplitModelGroup_ProducesOneModelLinePerLoadoutWithItsOwnCountAndWeapons()
    {
        var army = BuildRoster();
        var crusaderSquad = FindUnit(army, "Crusader Squad");

        crusaderSquad.ModelLines.Select(ml => ml.StatlineName)
            .Should().Equal("Sword Brother", "Initiate", "Initiate", "Neophyte");

        var chainswordInitiates = crusaderSquad.ModelLines.First(ml => ml.Weapons.Contains("Astartes Chainsword") && ml.Count == 3);
        chainswordInitiates.Weapons.Should().BeEquivalentTo("Astartes Chainsword", "Bolt pistol", "Heavy Bolt Pistol");

        var powerFistInitiates = crusaderSquad.ModelLines.First(ml => ml.Weapons.Contains("Power fist"));
        powerFistInitiates.Count.Should().Be(2);
        powerFistInitiates.Weapons.Should().BeEquivalentTo("Power fist", "Bolt pistol", "Heavy Bolt Pistol");
    }

    [Fact]
    public void MultiProfileWeaponSelection_RetainsEveryResolvedProfile()
    {
        var army = BuildRoster();
        var helbrecht = FindUnit(army, "High Marshal Helbrecht");

        var line = helbrecht.ModelLines.Should().ContainSingle().Subject;
        line.Weapons.Should().Contain(["Ferocity", "➤ Sword of the High Marshals - Sweep", "➤ Sword of the High Marshals - Strike"]);

        helbrecht.Datasheet.ResolveWeaponProfile("➤ Sword of the High Marshals - Sweep").Should().BeOfType<MeleeWeapon>();
        helbrecht.Datasheet.ResolveWeaponProfile("➤ Sword of the High Marshals - Strike").Should().BeOfType<MeleeWeapon>();
    }

    [Fact]
    public void EnhancementSelection_AttachesResolvedAbilityToTheOwningUnit()
    {
        var army = BuildRoster();
        var swordBrethren = FindUnit(army, "Sword Brethren Squad");
        var marshal = army.Units.OfType<AttachedUnit>()
            .Single(u => u.Bodyguard.Name == "Crusader Squad" && u.Attached.Count == 2)
            .Attached.Single(u => u.Name == "Marshal");

        marshal.Enhancements.Should().ContainSingle(a => a.Name == "Oathbound Exemplar" && a.Origin == AbilityOrigin.Enhancement);
        swordBrethren.Enhancements.Should().BeEmpty();
    }

    [Fact]
    public void CoreRuleReference_ResolvesWithFullTextAndNoAdditionalGating()
    {
        var army = BuildRoster();
        var crusaderSquad = FindUnit(army, "Crusader Squad");

        var vows = crusaderSquad.Datasheet.Abilities.Should().ContainSingle(a => a.Name == "Templar Vows").Subject;
        vows.Origin.Should().Be(AbilityOrigin.CoreRule);
        vows.Text.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SharedLoadoutWrapper_FallsBackToTheTopLevelSelectionsOwnStatline()
    {
        var army = BuildRoster();
        var swordBrethren = FindUnit(army, "Sword Brethren Squad");

        var line = swordBrethren.ModelLines.Should().ContainSingle().Subject;
        line.StatlineName.Should().Be("Sword Brethren");
        line.Count.Should().Be(4);
        line.Weapons.Should().BeEquivalentTo("Master-crafted Power Weapon", "Heavy Bolt Pistol");
    }

    [Fact]
    public void WrapperGroupWeaponSelection_ExpandsToItsOwnPerModelQuantity()
    {
        var army = BuildRoster();
        var impulsor = FindUnit(army, "Impulsor");

        var line = impulsor.ModelLines.Should().ContainSingle().Subject;
        line.Weapons.Where(w => w == "Storm bolter").Should().HaveCount(2);
        line.Weapons.Should().Contain(["Armoured Hull", "Multi-melta"]);
    }

    [Fact]
    public void WargearGrantedAbility_AttachesToTheOwningModelLine()
    {
        var army = BuildRoster();
        var impulsor = FindUnit(army, "Impulsor");

        var line = impulsor.ModelLines.Should().ContainSingle().Subject;
        line.Abilities.Should().ContainSingle(a => a.Name == "Shield Dome" && a.Origin == AbilityOrigin.OptionalGrant);
    }

    private static Unit FindUnit(ArmyRoster army, string name) =>
        army.Units.SelectMany(u => u.Components).First(u => u.Name == name && u.Datasheet.Name == name);
}
