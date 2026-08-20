using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Import;

namespace ProbHammer.Tests.Domain.Import;

public class ArmyListParserTests
{
    private static string ReadDataFile(string fileName, [CallerFilePath] string here = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "data", fileName));

    private static ParsedArmyList ParseDataFile(string fileName) => new ArmyListParser().Parse(ReadDataFile(fileName));

    [Fact]
    public void RealExport_ParsesArmyMetadata()
    {
        var army = ParseDataFile("gw-app-export.txt");

        army.Name.Should().Be("For the Emperor");
        army.PointsSpent.Should().Be(1065);
        army.Faction.Should().Equal("Space Marines", "Black Templars");
        army.Detachments.Should().Equal("Companions of Vehemence");
        army.ForceDisposition.Should().Be("Purge the Foe");
        army.BattleSize.Should().Be("Incursion");
        army.PointsLimit.Should().Be(1000);
    }

    [Fact]
    public void RealExport_SingleFactionArmyHasOneFactionEntry()
    {
        var army = ParseDataFile("gw-app-export-masters-of-the-maelstrom.txt");

        army.Faction.Should().Equal("Chaos Space Marines");
    }

    [Fact]
    public void RealExport_ParsesThreeAttachmentGroups()
    {
        var army = ParseDataFile("gw-app-export.txt");

        army.AttachmentGroups.Should().HaveCount(3);
    }

    [Fact]
    public void RealExport_FirstGroup_IdentifiesBodyguardAndPreservesAttachedOrder()
    {
        var army = ParseDataFile("gw-app-export.txt");

        var group = army.AttachmentGroups[0];
        group.Bodyguard.Name.Should().Be("Crusader Squad");
        group.Attached.Select(u => u.Name).Should().Equal("High Marshal Helbrecht", "Crusade Ancient");
    }

    [Fact]
    public void RealExport_BodyguardLineWithNoCategory_StillParsesAsBodyguard()
    {
        // Attached unit 3's "Sword Brethren Squad" role line is "Attached as: Bodyguard" - no
        // parenthetical category, unlike the other groups' "(Battleline)"/"(Character)" lines.
        var army = ParseDataFile("gw-app-export.txt");

        army.AttachmentGroups[2].Bodyguard.Name.Should().Be("Sword Brethren Squad");
    }

    [Fact]
    public void RealExport_GroupWithNoLeaderRoleMember_StillParses()
    {
        // "Attached unit 1" in the masters-of-the-maelstrom export has a Support member and a
        // Bodyguard member, but no Leader-role member at all.
        var army = ParseDataFile("gw-app-export-masters-of-the-maelstrom.txt");

        var group = army.AttachmentGroups[0];
        group.Bodyguard.Name.Should().Be("Legionaries");
        group.Attached.Select(u => u.Name).Should().Equal("Masters of the Maelstrom");
    }

    [Fact]
    public void RealExport_UniformModelGroup_ParsesToOneSubGroupWithAllWeapons()
    {
        var army = ParseDataFile("gw-app-export.txt");

        var neophyte = army.AttachmentGroups[0].Bodyguard.ModelGroups
            .Single(g => g.ModelName == "Neophyte");

        neophyte.Count.Should().Be(4);
        neophyte.Weapons.Should().BeEquivalentTo(["Astartes chainsword", "Bolt pistol"]);
    }

    [Fact]
    public void RealExport_MixedLoadoutModelGroup_SplitsIntoTwoSubGroups_SharedWeaponsOnBoth()
    {
        var army = ParseDataFile("gw-app-export.txt");

        var initiateGroups = army.AttachmentGroups[0].Bodyguard.ModelGroups
            .Where(g => g.ModelName == "Initiate")
            .ToList();

        initiateGroups.Should().HaveCount(2);

        var chainswordGroup = initiateGroups.Single(g => g.Weapons.Contains("Astartes chainsword"));
        chainswordGroup.Count.Should().Be(3);
        chainswordGroup.Weapons.Should().BeEquivalentTo(
            ["Bolt pistol", "Close combat weapon", "Heavy bolt pistol", "Astartes chainsword"]);

        var powerFistGroup = initiateGroups.Single(g => g.Weapons.Contains("Power fist"));
        powerFistGroup.Count.Should().Be(2);
        powerFistGroup.Weapons.Should().BeEquivalentTo(
            ["Bolt pistol", "Close combat weapon", "Heavy bolt pistol", "Power fist"]);
    }

    [Fact]
    public void RealExport_SingleModelUnitWithDirectWeaponBullets_SynthesizesOneImplicitGroup()
    {
        var army = ParseDataFile("gw-app-export.txt");

        var helbrecht = army.AttachmentGroups[0].Attached.Single(u => u.Name == "High Marshal Helbrecht");

        helbrecht.ModelGroups.Should().ContainSingle();
        var group = helbrecht.ModelGroups[0];
        group.ModelName.Should().Be("High Marshal Helbrecht");
        group.Count.Should().Be(1);
        group.Weapons.Should().Equal("Ferocity", "Sword of the High Marshals");
    }

    [Fact]
    public void RealExport_DirectWeaponBulletWithCountGreaterThanOne_RepeatsTheWeaponName()
    {
        // Impulsor is a single-model vehicle carrying 2 Storm bolters - "2x Storm bolter" must
        // expand to two entries in the flat per-model weapon list, not be treated as an
        // alternative-loadout split (there's only one model to assign weapons to).
        var army = ParseDataFile("gw-app-export.txt");

        var impulsor = army.StandaloneUnits.Single(u => u.Name == "Impulsor");

        impulsor.ModelGroups.Should().ContainSingle();
        var group = impulsor.ModelGroups[0];
        group.Count.Should().Be(1);
        group.Weapons.Should().Equal("Armoured hull", "Multi-melta", "Shield Dome", "Storm bolter", "Storm bolter");
    }

    [Fact]
    public void RealExport_StandaloneUnitsAreNotWrappedInAttachmentGroups()
    {
        var army = ParseDataFile("gw-app-export.txt");

        army.StandaloneUnits.Select(u => u.Name).Should().Equal(
            "Emperor’s Champion", "Assault Intercessor Squad", "Impulsor", "Scout Squad");
    }

    [Fact]
    public void RealExport_TypographicApostropheInUnitName_IsPreservedVerbatim()
    {
        var army = ParseDataFile("gw-app-export.txt");

        army.StandaloneUnits.Should().Contain(u => u.Name == "Emperor’s Champion");
    }

    [Fact]
    public void RealExport_TypographicApostropheInWeaponName_IsPreservedVerbatim()
    {
        var army = ParseDataFile("gw-app-export-masters-of-the-maelstrom.txt");

        var raiderChampion = army.AttachmentGroups[1].Bodyguard.ModelGroups
            .Single(g => g.ModelName == "Red Corsairs Raider Champion");

        raiderChampion.Weapons.Should().Contain("Reaver’s blade");
    }

    [Fact]
    public void RealExport_EnhancementLine_IsCapturedSeparatelyFromWargear()
    {
        var army = ParseDataFile("gw-app-export-masters-of-the-maelstrom.txt");

        var reaveCaptain = army.AttachmentGroups[1].Attached.Single(u => u.Name == "Red Corsairs Reave-Captain");

        reaveCaptain.Enhancements.Should().Equal("Touched by the Warp");
        reaveCaptain.ModelGroups.Single().Weapons.Should().Equal("Bolt Pistol", "Power sword");
    }

    [Fact]
    public void RealExport_TrailerLine_StopsParsingWithoutError()
    {
        var act = () => ParseDataFile("gw-app-export.txt");

        act.Should().NotThrow();
    }

    [Fact]
    public void RealExport_CommaBearingDetachmentName_IsCapturedAsOneEntry()
    {
        var army = ParseDataFile("gw-app-export-3-dp.txt");

        army.Detachments.Should().Equal("Fulguris Task Force, Marshal's Household, and Subversion Assets");
    }

    [Fact]
    public void RealExport_SecondSquadsAlternatingWeaponsAtTheSameCount_SplitIntoSeparateSubGroups()
    {
        // Company Veteran: "1x Master-crafted bolt rifle" and "1x Master-crafted heavy bolter"
        // share the same count (1) but must produce two distinct 1-model sub-groups, not one
        // sub-group carrying both weapons (design.md's verified real-data finding).
        var army = ParseDataFile("gw-app-export-3-dp.txt");

        var veteranGroups = army.StandaloneUnits.Single(u => u.Name == "Company Heroes").ModelGroups
            .Where(g => g.ModelName == "Company Veteran")
            .ToList();

        veteranGroups.Should().HaveCount(2);
        veteranGroups.Should().OnlyContain(g => g.Count == 1);

        var boltRifleGroup = veteranGroups.Single(g => g.Weapons.Contains("Master-crafted bolt rifle"));
        boltRifleGroup.Weapons.Should().BeEquivalentTo(["Bolt pistol", "Close combat weapon", "Master-crafted bolt rifle"]);

        var heavyBolterGroup = veteranGroups.Single(g => g.Weapons.Contains("Master-crafted heavy bolter"));
        heavyBolterGroup.Weapons.Should().BeEquivalentTo(["Bolt pistol", "Close combat weapon", "Master-crafted heavy bolter"]);
    }

    [Theory]
    [InlineData("gw-app-export.txt")]
    [InlineData("gw-app-export-3-dp.txt")]
    [InlineData("gw-app-export-masters-of-the-maelstrom.txt")]
    public void AllThreeRealExports_ParseWithoutThrowing(string fileName)
    {
        var act = () => ParseDataFile(fileName);

        act.Should().NotThrow();
    }

    // --- Hand-built edge-case fixtures ---

    [Fact]
    public void SingleFactionHeader_ParsesToOneFactionEntry()
    {
        var text = ArmyListText(faction: ["Orks"]);

        var army = new ArmyListParser().Parse(text);

        army.Faction.Should().Equal("Orks");
    }

    [Fact]
    public void SubFactionHeader_ParsesToTwoFactionEntriesInOrder()
    {
        var text = ArmyListText(faction: ["Space Marines", "Black Templars"]);

        var army = new ArmyListParser().Parse(text);

        army.Faction.Should().Equal("Space Marines", "Black Templars");
    }

    [Fact]
    public void AttachedAsLine_MissingCategory_ParsesSameRoleAsWithCategory()
    {
        var text = ArmyListText(attachedAsLine: "  • Attached as: Leader");

        var army = new ArmyListParser().Parse(text);

        army.AttachmentGroups[0].Attached.Should().ContainSingle(u => u.Name == "Test Leader");
    }

    [Fact]
    public void UnpartitionableWeaponCounts_ThrowsWithUnitAndRawTextDiagnostic()
    {
        var text = ArmyListText(bodyguardModelLines:
        [
            "  • 5x Test Model",
            "     ◦ 5x Boltgun",
            "     ◦ 2x Chainsword", // 2 + 4 = 6, doesn't sum to 5
            "     ◦ 4x Power fist"
        ]);

        var act = () => new ArmyListParser().Parse(text);

        act.Should().Throw<ArmyListParseException>()
            .Which.UnitName.Should().Be("Test Bodyguard");
    }

    private static string ArmyListText(
        IReadOnlyList<string>? faction = null,
        string? attachedAsLine = null,
        IReadOnlyList<string>? bodyguardModelLines = null)
    {
        faction ??= ["Chaos Space Marines"];
        attachedAsLine ??= "  • Attached as: Leader (Character)";
        bodyguardModelLines ??=
        [
            "  • 1x Test Model",
            "     ◦ 1x Boltgun"
        ];

        var lines = new List<string>
        {
            "Test Army (100 Points)",
            "",
        };
        lines.AddRange(faction);
        lines.Add("Test Detachment (1 Detachment Points)");
        lines.Add("Test Disposition");
        lines.Add("Incursion (1,000 Points)");
        lines.Add("");
        lines.Add("ATTACHED UNITS");
        lines.Add("");
        lines.Add("Attached unit 1");
        lines.Add("");
        lines.Add("Test Leader (10 Points)");
        lines.Add(attachedAsLine);
        lines.Add("  • 1x Wargear");
        lines.Add("");
        lines.Add("Test Bodyguard (10 Points)");
        lines.Add("  • Attached as: Bodyguard");
        lines.AddRange(bodyguardModelLines);
        lines.Add("");
        lines.Add("Exported with App Version: v2.4.0 (1), Data Version: v925");

        return string.Join("\n", lines);
    }
}
