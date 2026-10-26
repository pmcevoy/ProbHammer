using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Import.BattleScribe;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Import.BattleScribe;

/// <summary>Fixture-based unit tests for <see cref="BattleScribeRosterMapper"/> against a trimmed
/// real NewRecruit Templars export (see Fixtures/templars-excerpt.json -
/// same "trimmed real excerpt" convention as <c>tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/</c>),
/// covering: attachment resolution (2-member and Leader-only real shapes), the pre-split
/// model-group shape, a multi-profile weapon (Sword of the High Marshals), an Enhancement
/// (Oathbound Exemplar), a Core Rule (Templar Vows), the shared-loadout-wrapper statline fallback
/// (Sword Brethren Squad), and a wrapper-group weapon quantity (Impulsor's "2 Storm Bolters").</summary>
public class BattleScribeRosterMapperTests
{
    private static string ReadFixture(string fileName = "templars-excerpt.json", [CallerFilePath] string here = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "Fixtures", fileName));

    private static ArmyRoster BuildRoster(string fileName = "templars-excerpt.json")
    {
        BattleScribeRosterFormat.TryParse(ReadFixture(fileName), out var roster).Should().BeTrue();
        return BattleScribeRosterMapper.Map(roster!);
    }

    [Fact]
    public void RealExcerpt_ParsesArmyMetadata()
    {
        var army = BuildRoster();

        army.PointsSpent.Should().Be(915);
        army.PointsLimit.Should().Be(1000);
        army.Faction.Should().Equal("Imperium", "Adeptus Astartes", "Black Templars");
        army.Detachments.Select(d => d.Name).Should().Equal("Companions of Vehemence");
        army.ForceDisposition.Should().Be("Purge the Foe");
        army.BattleSize.Should().Be("Incursion");
    }

    [Fact]
    public void SelectedDetachment_CarriesItsOwnInlineRuleTextDirectlyFromTheRosterJson()
    {
        // No BSData involvement in this pipeline - a selected Detachment's own rule text is already
        // inline on its own selections[].rules[] (real shape: "Companions of Vehemence" carries
        // "Righteous Fervour").
        var army = BuildRoster();

        var detachment = army.Detachments.Should().ContainSingle().Subject;
        detachment.Name.Should().Be("Companions of Vehemence");
        detachment.Rules.Should()
            .ContainSingle(r => r.Name == "Righteous Fervour" && !string.IsNullOrWhiteSpace(r.Text));
    }

    [Fact]
    public void PluralDetachmentsGroupName_StillResolvesTheSelectedDetachment()
    {
        // A real Adeptus Custodes NewRecruit export names
        // its top-level Detachment-choice selection "Detachments" (plural), not "Detachment"
        // (singular, as Death Guard/Black Templars use) - mirroring the same singular-vs-plural
        // inconsistency the BSData pipeline's own DetachmentGroupNameScanTests already documents
        // for catalogue group names.
        var army = BuildRoster("custodes-detachments-plural-excerpt.json");

        var detachment = army.Detachments.Should().ContainSingle().Subject;
        detachment.Name.Should().Be("Auric Champions");
        detachment.Rules.Should().ContainSingle(r => r.Name == "Assemblage of Might");
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

        var chainswordInitiates =
            crusaderSquad.ModelLines.First(ml => ml.Weapons.Contains("Astartes Chainsword") && ml.Count == 3);
        chainswordInitiates.Weapons.Should().BeEquivalentTo("Astartes Chainsword", "Bolt pistol", "Heavy Bolt Pistol");

        var powerFistInitiates = crusaderSquad.ModelLines.First(ml => ml.Weapons.Contains("Power fist"));
        powerFistInitiates.Count.Should().Be(2);
        powerFistInitiates.Weapons.Should().BeEquivalentTo("Power fist", "Bolt pistol", "Heavy Bolt Pistol");
    }

    [Fact]
    public void PreSplitModelGroup_CarriesEachLoadoutsOwnRawSelectionName_AsDisplayName()
    {
        // The roster JSON's own per-node "name" ("Initiate w/Power Fist & Heavy Bolt Pistol") is
        // distinct from the resolved catalogue StatlineName ("Initiate") both Initiate ModelLines
        // share - DisplayName should carry that raw name through, not collapse to StatlineName.
        var army = BuildRoster();
        var crusaderSquad = FindUnit(army, "Crusader Squad");

        var chainswordInitiates =
            crusaderSquad.ModelLines.First(ml => ml.Weapons.Contains("Astartes Chainsword"));
        chainswordInitiates.DisplayName.Should().Be("Initiate w/Chainsword & Heavy Bolt Pistol");

        var powerFistInitiates = crusaderSquad.ModelLines.First(ml => ml.Weapons.Contains("Power fist"));
        powerFistInitiates.DisplayName.Should().Be("Initiate w/Power Fist & Heavy Bolt Pistol");
    }

    [Fact]
    public void MultiProfileWeaponSelection_RetainsEveryResolvedProfile()
    {
        var army = BuildRoster();
        var helbrecht = FindUnit(army, "High Marshal Helbrecht");

        var line = helbrecht.ModelLines.Should().ContainSingle().Subject;
        line.Weapons.Should().Contain([
            "Ferocity", "➤ Sword of the High Marshals - Sweep", "➤ Sword of the High Marshals - Strike"
        ]);

        helbrecht.Datasheet.ResolveWeaponProfile("➤ Sword of the High Marshals - Sweep").Should()
            .BeOfType<MeleeWeapon>();
        helbrecht.Datasheet.ResolveWeaponProfile("➤ Sword of the High Marshals - Strike").Should()
            .BeOfType<MeleeWeapon>();
    }

    [Fact]
    public void EnhancementSelection_AttachesResolvedAbilityToTheOwningUnit()
    {
        var army = BuildRoster();
        var swordBrethren = FindUnit(army, "Sword Brethren Squad");
        var marshal = army.Units.OfType<AttachedUnit>()
            .Single(u => u.Bodyguard.Name == "Crusader Squad" && u.Attached.Count == 2)
            .Attached.Single(u => u.Name == "Marshal");

        marshal.Enhancements.Should()
            .ContainSingle(a => a.Name == "Oathbound Exemplar" && a.Origin == AbilityOrigin.Enhancement);
        swordBrethren.Enhancements.Should().BeEmpty();
    }

    [Fact]
    public void CoreRuleReference_ResolvesWithFullTextAndNoAdditionalGating()
    {
        // This excerpt predates the 2026 Space Marine codex, which made Templar Vows a Detachment
        // rule, so it no longer matches the Black Templars Army Rule lookup and stays CoreRule. This
        // pipeline applies no chapter/game-mode gating pass of its own.
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

    [Fact]
    public void WargearGrantedShieldDome_FlagsTheImpulsorsInvulnerableSave()
    {
        // Real end-to-end confirmation against this fixture's own real "Shield Dome" text
        // ("The bearer has a 5+ invulnerable save.") - not just a hand-built rule-engine fixture.
        var army = BuildRoster();
        var impulsor = army.Units.OfType<Unit>().Single(u => u.Name == "Impulsor");

        var view = AttachedUnitAggregator.Build(impulsor, ClassificationFixtures.ShieldDomeAndVexilla);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.IsCaveated.Should().BeFalse();
        entry.Statline.InSv.DerivedValue!.MeleeInSv.Should().Be(5);
        entry.Statline.InSv.DerivedValue.RangedInSv.Should().Be(5);
        view.Abilities.Should().ContainSingle(e => e.Ability.Name == "Shield Dome"); // still visible normally
    }

    [Fact]
    public void TopLevelSelectionsOwnCategories_PopulateTheSynthesizedDatasheetsKeywords()
    {
        // Trimmed real excerpt of a Masters of the Maelstrom NewRecruit export - see
        // Fixtures/masters-of-the-maelstrom-keywords-excerpt.json.
        var army = BuildRoster("masters-of-the-maelstrom-keywords-excerpt.json");
        var unit = FindUnit(army, "Masters of the Maelstrom");

        unit.Datasheet.Keywords.Should().Contain("Infantry");
        unit.Datasheet.Keywords.Should().Contain("Heretic Astartes");
        unit.Datasheet.Keywords.Should().NotContain("Faction: Heretic Astartes");
    }

    [Fact]
    public void NestedModelSelectionsOwnCategories_PopulateOnlyThatModelLinesKeywords()
    {
        var army = BuildRoster("masters-of-the-maelstrom-keywords-excerpt.json");
        var unit = FindUnit(army, "Masters of the Maelstrom");

        var garlon = unit.ModelLines.Single(ml => ml.StatlineName == "Garlon Souleater");
        var garreon = unit.ModelLines.Single(ml => ml.StatlineName == "Garreon the Corpsemaster");

        garlon.Keywords.Should().Contain("Psyker");
        garreon.Keywords.Should().BeEmpty();
        unit.Datasheet.Keywords.Should().NotContain("Psyker");
    }

    [Fact]
    public void LeaderSupportAndAttachedUnit_AreExcludedFromBothExtractionPaths()
    {
        // Real fixture shape: "Lieutenant" carries BOTH a rules[]-sourced "Support" entry (Core
        // Rule Extraction) and a profiles[]-sourced "Support" Abilities entry (Statline/Weapon/
        // Ability Extraction) - both restate attachment eligibility the roster's own Attachment
        // Relationship Resolution already represents directly.
        var army = BuildRoster();
        var lieutenant = FindUnit(army, "Lieutenant");

        lieutenant.Datasheet.Abilities.Select(a => a.Name).Should().NotContain(["Leader", "Support", "Attached Unit"]);
    }

    [Fact]
    public void SameNameMultiProfileWeapon_IsCountedOnceNotOncePerProfile()
    {
        // Guardian Spear exports as two profiles (melee and thrown) sharing one name; it is one weapon
        // per model, so a unit's total Attacks must count it once, not once per profile.
        var json = File.ReadAllText(RealCorpusRosterPath("nr-custodes-for-unit-tests.json"));
        BattleScribeRosterFormat.TryParse(json, out var roster).Should().BeTrue();
        var army = BattleScribeRosterMapper.Map(roster!);

        var guard = army.Units.SelectMany(u => u.Components)
            .Single(u => u.Datasheet.Name == "Custodian Guard Sodality");
        var guardianSpearLines = guard.ModelLines.Where(ml => ml.Weapons.Contains("Guardian Spear")).ToList();
        guardianSpearLines.Should().NotBeEmpty();
        foreach (var line in guardianSpearLines)
            line.Weapons.Count(w => w == "Guardian Spear").Should().Be(1);

        var view = AttachedUnitAggregator.Build(guard, AbilityClassificationCatalogue.Empty);
        var guardianSpear = view.Weapons.Single(w => w.Name == "Guardian Spear");
        var totalGuard = guardianSpearLines.Sum(ml => ml.Count);
        guardianSpear.TotalAttacks.Should().Be(DiceExpression.Fixed(6 * totalGuard));
    }

    [Fact]
    public void UnitLevelWargearAbility_IsADatasheetAbility()
    {
        var json = File.ReadAllText(RealCorpusRosterPath("nr-custodes-for-unit-tests.json"));
        BattleScribeRosterFormat.TryParse(json, out var roster).Should().BeTrue();
        var army = BattleScribeRosterMapper.Map(roster!);

        var wardens = army.Units.SelectMany(u => u.Components).Single(u => u.Datasheet.Name == "Custodian Wardens");

        wardens.Datasheet.Abilities.Should().ContainSingle(a =>
            a.Name == "Vexilla" && a.Origin == AbilityOrigin.OptionalGrant && a.Text.Contains("+1 OC"));
    }

    private static string RealCorpusRosterPath(string fileName, [CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "..", "data", fileName));

    private static Unit FindUnit(ArmyRoster army, string name) =>
        army.Units.SelectMany(u => u.Components).First(u => u.Name == name && u.Datasheet.Name == name);
}