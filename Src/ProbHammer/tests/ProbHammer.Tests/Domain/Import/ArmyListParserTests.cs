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

    // --- Real Android-export regression coverage ---

    [Fact]
    public void RealExport_AndroidDeathGuard_ParsesFullArmyMetadata()
    {
        // No attached units and no partition ambiguity - the whole file is expected to succeed
        // once the case-insensitivity and bullet-continuation fixes are in place. Also has no
        // ForceDisposition line at all (goes straight from the Detachment line to the BattleSize
        // line) - a third real finding beyond the two this change originally scoped to; see
        // design.md's "ForceDisposition is optional" decision.
        var army = ParseDataFile("gw-android-export-deathguard.txt");

        army.Name.Should().Be("11th First");
        army.PointsSpent.Should().Be(960);
        army.Faction.Should().Equal("Death Guard");
        army.Detachments.Should().Equal("Virulent Vectorium");
        army.ForceDisposition.Should().BeEmpty();
        army.BattleSize.Should().Be("Strike Force");
        army.PointsLimit.Should().Be(2000);
    }

    [Fact]
    public void RealExport_AndroidDeathGuard_HasNoAttachmentGroups()
    {
        var army = ParseDataFile("gw-android-export-deathguard.txt");

        army.AttachmentGroups.Should().BeEmpty();
    }

    [Fact]
    public void RealExport_AndroidDeathGuard_ParsesEveryStandaloneUnitName()
    {
        var army = ParseDataFile("gw-android-export-deathguard.txt");

        army.StandaloneUnits.Select(u => u.Name).Should().Equal(
            "Daemon Prince of Nurgle", "Plague Marines", "Plague Marines", "Defiler",
            "Foetid Bloat-Drone with Heavy Blight Launcher", "Poxwalkers", "Poxwalkers");
    }

    [Fact]
    public void RealExport_AndroidDeathGuard_DaemonPrince_DirectWeaponsIncludeTheContinuationLine()
    {
        // "1x Infernal cannon" carries no bullet of its own in the raw export - it continues
        // "1x Hellforged weapons"' own bulleted line.
        var army = ParseDataFile("gw-android-export-deathguard.txt");

        var daemonPrince = army.StandaloneUnits.Single(u => u.Name == "Daemon Prince of Nurgle");

        daemonPrince.ModelGroups.Should().ContainSingle();
        daemonPrince.ModelGroups[0].Weapons.Should().Equal("Hellforged weapons", "Infernal cannon");
    }

    [Fact]
    public void RealExport_AndroidDeathGuard_PlagueMarines_NestedWeaponsReuseTheBulletThenContinue()
    {
        // "1x Boltgun" re-adopts the "•" bullet for its model group's own first nested weapon
        // (Android has no distinct "◦" glyph); "1x Plague knives" continues that same nested list
        // with no bullet at all.
        var army = ParseDataFile("gw-android-export-deathguard.txt");

        var plagueMarines = army.StandaloneUnits.First(u => u.Name == "Plague Marines");

        var champion = plagueMarines.ModelGroups.Single(g => g.ModelName == "Plague Champion");
        champion.Count.Should().Be(1);
        champion.Weapons.Should().BeEquivalentTo(["Boltgun", "Plague knives"]);

        var troopers = plagueMarines.ModelGroups.Single(g => g.ModelName == "Plague Marine");
        troopers.Count.Should().Be(4);
        troopers.Weapons.Should().BeEquivalentTo(["Boltgun", "Plague knives"]);
    }

    [Fact]
    public void RealExport_AndroidDeathGuard_Defiler_FourConsecutiveContinuationLinesAllExtendTheSameList()
    {
        var army = ParseDataFile("gw-android-export-deathguard.txt");

        var defiler = army.StandaloneUnits.Single(u => u.Name == "Defiler");

        defiler.ModelGroups.Should().ContainSingle();
        defiler.ModelGroups[0].Weapons.Should().Equal(
            "Ectoplasma destructor", "Excruciator cannon", "Excruciator cannon", "Hades lascannon",
            "Heavy reaper autocannon", "Shearing claws");
    }

    [Fact]
    public void RealExport_AndroidDeathGuard_Poxwalkers_UniformGroupNeedsNoSplit()
    {
        var army = ParseDataFile("gw-android-export-deathguard.txt");

        var poxwalkers = army.StandaloneUnits.First(u => u.Name == "Poxwalkers");

        poxwalkers.ModelGroups.Should().ContainSingle();
        var group = poxwalkers.ModelGroups[0];
        group.ModelName.Should().Be("Poxwalker");
        group.Count.Should().Be(10);
        group.Weapons.Should().Equal("Improvised weapon");
    }

    [Fact]
    public void RealExport_AndroidCustodes_FullFile_FailsOnCustodianWardens()
    {
        // The whole file parses sequentially - Custodian Wardens (the second member of Attached
        // unit 1) is the first unit whose weapon counts can't be partitioned, so a whole-file
        // parse throws there before ever reaching Custodian Guard or Allarus Custodians. Their own
        // successful/failing behavior is verified in isolation below, using fragments extracted
        // verbatim from this same real file.
        var act = () => ParseDataFile("gw-android-export-custodes.txt");

        act.Should().Throw<ArmyListParseException>()
            .Which.UnitName.Should().Be("Custodian Wardens");
    }

    [Fact]
    public void RealExport_AndroidCustodes_BladeChampion_ParsesSuccessfully()
    {
        var army = ParseCustodesUnitInIsolation("Blade Champion (");

        var unit = army.StandaloneUnits.Single();
        unit.Name.Should().Be("Blade Champion");
        unit.Enhancements.Should().Equal("Martial Philosopher");
        unit.ModelGroups.Should().ContainSingle();
        unit.ModelGroups[0].Weapons.Should().Equal("Vaultswords");
    }

    [Fact]
    public void RealExport_AndroidCustodes_AllarusCustodians_ParsesSuccessfully()
    {
        var army = ParseCustodesUnitInIsolation("Allarus Custodians (");

        var unit = army.StandaloneUnits.Single();
        unit.Name.Should().Be("Allarus Custodians");
        unit.ModelGroups.Should().ContainSingle();
        var group = unit.ModelGroups[0];
        group.ModelName.Should().Be("Allarus Custodian");
        group.Count.Should().Be(3);
        group.Weapons.Should().BeEquivalentTo(["Balistus grenade launcher", "Guardian spear"]);
    }

    [Fact]
    public void RealExport_AndroidCustodes_CustodianWardens_FailsWithUnitAndRawTextDiagnostic()
    {
        var act = () => ParseCustodesUnitInIsolation("Custodian Wardens (");

        act.Should().Throw<ArmyListParseException>()
            .Which.UnitName.Should().Be("Custodian Wardens");
    }

    [Fact]
    public void RealExport_AndroidCustodes_CustodianGuard_FailsWithUnitAndRawTextDiagnostic()
    {
        // Once the bullet-continuation rule flattens this group's three weapon-count lines into
        // one list (1x Guardian spear, 3x Praesidium Shield, 3x Sentinel blade, total 4), the
        // existing partition rule still can't resolve it - a deliberate non-goal of this change
        // (see design.md's "Custodian Guard partition ambiguity is not resolved").
        var act = () => ParseCustodesUnitInIsolation("Custodian Guard (");

        act.Should().Throw<ArmyListParseException>()
            .Which.UnitName.Should().Be("Custodian Guard");
    }

    /// <summary>Parses one named unit from the real gw-android-export-custodes.txt in isolation,
    /// by extracting its own verbatim header+bullet-block text (unmodified real bytes, not a
    /// hand-retyped equivalent) and wrapping it in a minimal standalone-section skeleton. A real
    /// whole-file parse can only ever report one outcome (it throws on the first unit that fails),
    /// so this is what lets each of Blade Champion/Custodian Wardens/Custodian Guard/Allarus
    /// Custodians be asserted on independently even though the real file mixes succeeding and
    /// failing units together.</summary>
    private static ParsedArmyList ParseCustodesUnitInIsolation(string unitHeaderPrefix)
    {
        var fullText = ReadDataFile("gw-android-export-custodes.txt");
        var block = ExtractUnitBlock(fullText, unitHeaderPrefix);
        return new ArmyListParser().Parse(SingleUnitArmyListText(block));
    }

    private static string ExtractUnitBlock(string exportText, string unitHeaderPrefix)
    {
        var lines = exportText.Replace("\r\n", "\n").Split('\n');
        var start = Array.FindIndex(lines, l => l.StartsWith(unitHeaderPrefix, StringComparison.Ordinal));
        if (start < 0)
            throw new InvalidOperationException($"Unit header '{unitHeaderPrefix}' not found.");

        var end = start + 1;
        while (end < lines.Length && lines[end].Trim().Length > 0)
            end++;

        return string.Join("\n", lines[start..end]);
    }

    private static string SingleUnitArmyListText(string unitBlockText) => string.Join("\n",
    [
        "Test Army (100 Points)",
        "",
        "Chaos Space Marines",
        "Test Detachment (1 Detachment Points)",
        "Test Disposition",
        "Incursion (1,000 Points)",
        "",
        "OTHER DATASHEETS",
        "",
        unitBlockText,
        "",
        "Exported with App Version: v2.4.0 (1), Data Version: v925"
    ]);

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

    // --- Case-insensitive metadata/section matching ---

    [Fact]
    public void LowerCasePointsSuffix_ArmyHeaderAndBattleSizeLines_ParseIdenticallyToTitleCase()
    {
        var text = ArmyListText(
            armyHeaderLine: "Barra's Army (725 points)",
            battleSizeLine: "Incursion (1000 points)");

        var army = new ArmyListParser().Parse(text);

        army.Name.Should().Be("Barra's Army");
        army.PointsSpent.Should().Be(725);
        army.BattleSize.Should().Be("Incursion");
        army.PointsLimit.Should().Be(1000);
    }

    [Fact]
    public void TitleCaseAttachedUnitsMarkers_ParseIdenticallyToAllCapsForm()
    {
        var text = ArmyListText(
            attachedUnitsHeader: "Attached Units",
            attachedUnitGroupLine: "Attached Unit 1");

        var army = new ArmyListParser().Parse(text);

        army.AttachmentGroups.Should().HaveCount(1);
        army.AttachmentGroups[0].Bodyguard.Name.Should().Be("Test Bodyguard");
    }

    // --- Bullet-continuation rule ---

    [Fact]
    public void UnbulletedContinuationLine_ExtendsTheDirectWeaponList()
    {
        // Daemon Prince shape: one continuation line with no bullet of its own.
        var text = StandaloneArmyListText(
        [
            "  • 1x Hellforged weapons",
            "    1x Infernal cannon"
        ]);

        var army = new ArmyListParser().Parse(text);

        var unit = army.StandaloneUnits.Single();
        unit.ModelGroups.Should().ContainSingle();
        unit.ModelGroups[0].Weapons.Should().Equal("Hellforged weapons", "Infernal cannon");
    }

    [Fact]
    public void FourConsecutiveUnbulletedContinuationLines_AllExtendTheSameDirectWeaponList()
    {
        // Defiler shape: four consecutive continuation lines, all at the same indent - not
        // progressively deeper.
        var text = StandaloneArmyListText(
        [
            "  • 1x Ectoplasma destructor",
            "    2x Excruciator cannon",
            "    1x Hades lascannon",
            "    1x Heavy reaper autocannon",
            "    1x Shearing claws"
        ]);

        var army = new ArmyListParser().Parse(text);

        var unit = army.StandaloneUnits.Single();
        unit.ModelGroups.Should().ContainSingle();
        unit.ModelGroups[0].Weapons.Should().Equal(
            "Ectoplasma destructor", "Excruciator cannon", "Excruciator cannon", "Hades lascannon",
            "Heavy reaper autocannon", "Shearing claws");
    }

    [Fact]
    public void UnbulletedContinuationTwoTiersDeep_StillFailsWithUnpartitionableDiagnostic()
    {
        // Custodian Wardens shape: the nested weapon list's own first item re-adopts the "•"
        // bullet, and the second continues it with no bullet at all, two tiers deep. The
        // continuation rule correctly flattens this into one list - the resulting counts (5 and 1
        // against a total of 5) still can't be partitioned, which is the existing, unmodified
        // diagnostic this change deliberately leaves in place (see design.md).
        var text = StandaloneArmyListText(
        [
            "  • 5x Custodian Warden",
            "    • 5x Guardian spear",
            "      1x Vexilla"
        ]);

        var act = () => new ArmyListParser().Parse(text);

        act.Should().Throw<ArmyListParseException>()
            .Which.UnitName.Should().Be("Test Unit");
    }

    [Fact]
    public void UnbulletedLineBeforeAnyBulletedLine_StillFallsThroughToUnitHeaderParsing()
    {
        // A malformed export whose unit block never opens with a bulleted line at all must still
        // fail with today's unit-header diagnostic, not be silently swallowed as a continuation.
        var text = StandaloneArmyListText(["    Not a bullet line at all"]);

        var act = () => new ArmyListParser().Parse(text);

        act.Should().Throw<ArmyListParseException>()
            .WithMessage("*<Name> (<N> Points)*");
    }

    private static string ArmyListText(
        IReadOnlyList<string>? faction = null,
        string? attachedAsLine = null,
        IReadOnlyList<string>? bodyguardModelLines = null,
        string armyHeaderLine = "Test Army (100 Points)",
        string battleSizeLine = "Incursion (1,000 Points)",
        string attachedUnitsHeader = "ATTACHED UNITS",
        string attachedUnitGroupLine = "Attached unit 1")
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
            armyHeaderLine,
            "",
        };
        lines.AddRange(faction);
        lines.Add("Test Detachment (1 Detachment Points)");
        lines.Add("Test Disposition");
        lines.Add(battleSizeLine);
        lines.Add("");
        lines.Add(attachedUnitsHeader);
        lines.Add("");
        lines.Add(attachedUnitGroupLine);
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

    private static string StandaloneArmyListText(IReadOnlyList<string> unitBulletLines) => string.Join("\n",
    [
        "Test Army (100 Points)",
        "",
        "Chaos Space Marines",
        "Test Detachment (1 Detachment Points)",
        "Test Disposition",
        "Incursion (1,000 Points)",
        "",
        "OTHER DATASHEETS",
        "",
        "Test Unit (10 Points)",
        ..unitBulletLines,
        "",
        "Exported with App Version: v2.4.0 (1), Data Version: v925"
    ]);
}
