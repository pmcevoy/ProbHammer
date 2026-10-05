using ProbHammer.Tests.Domain.Fixtures;
using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Web.Pages;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Drives the full production pipeline (BsdataFactionResolver -> ResolvedBsdataCatalogue ->
/// ArmyRosterEnricher.Enrich -> AttachedUnitAggregator.Build) against the real bundled BSData
/// snapshot, resolving against the checked-in catalogue.</summary>
public class WeaponCharacteristicEffectRealCorpusTests
{
    private static string BundledBsDataRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web",
            "BsData"));

    [Fact]
    public void MinistorumPriestWithZealot_BuildsWithoutCrashing_AndLeavesItsWeaponUnmutated()
    {
        var parsed = new ParsedArmyList(
            Name: "Test Army", PointsSpent: 0, Faction: ["Imperium", "Adepta Sororitas"], Detachments: [],
            ForceDisposition: "Test", BattleSize: "Incursion", PointsLimit: 1000, AttachmentGroups: [],
            StandaloneUnits:
            [
                new ParsedUnit(
                    "Ministorum Priest",
                    [new ParsedModelGroup("Ministorum Priest", 1, ["Power weapon"])], [])
            ]);

        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var fileName = BsdataFactionResolver.ResolveStartingFileName(parsed.Faction, source.ListFileNames());
        var catalogue = ResolvedBsdataCatalogue.Build(source, fileName);
        var roster = ArmyRosterEnricher.Enrich(parsed, catalogue);
        var priest = roster.Units.Single();
        var classifications = ClassificationFixtures.CheckedIn;

        var view = AttachedUnitAggregator.Build(priest, classifications);

        view.Abilities.Should().ContainSingle(e => e.Ability.Name == "Zealot");

        var weapon = view.Weapons.Should().ContainSingle(w => w.Profile.Name == "Power weapon").Subject;
        weapon.Profile.S.IsCaveated.Should().BeFalse();
        weapon.Profile.S.Value.Should().Be((CharacteristicValue)4); // the real printed value, unmutated
        weapon.Profile.S.ContributingAbilities.Should().BeEmpty(); // Zealot is conditional - not applied
        weapon.NotAppliedEffects.Should().Contain(e => e.SourceAbility.Name == "Zealot");
    }

    [Fact]
    public void MinistorumPriestWithZealot_HighlightsStrengthWithANotAddedZealotLine()
    {
        var parsed = new ParsedArmyList(
            Name: "Test Army", PointsSpent: 0, Faction: ["Imperium", "Adepta Sororitas"], Detachments: [],
            ForceDisposition: "Test", BattleSize: "Incursion", PointsLimit: 1000, AttachmentGroups: [],
            StandaloneUnits:
            [
                new ParsedUnit(
                    "Ministorum Priest",
                    [new ParsedModelGroup("Ministorum Priest", 1, ["Power weapon"])], [])
            ]);

        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var fileName = BsdataFactionResolver.ResolveStartingFileName(parsed.Faction, source.ListFileNames());
        var catalogue = ResolvedBsdataCatalogue.Build(source, fileName);
        var roster = ArmyRosterEnricher.Enrich(parsed, catalogue);
        var priest = roster.Units.Single();
        var classifications = ClassificationFixtures.CheckedIn;

        var view = AttachedUnitAggregator.Build(priest, classifications);
        var block = LivePlayModel.BuildUnitBlock(view);

        var weaponRow = block.MeleeWeapons.Should().ContainSingle(w => w.Entry.Profile.Name == "Power weapon").Subject;
        weaponRow.ProvenanceFor("S")!.Lines.Should()
            .Contain(l => l.Label == "Zealot" && l.Kind == ProvenanceLineKind.NotAdded);
    }

    // resolve-weapon-attacks-effects task 4.4: proposal.md names Scorpion Tail/Writhing Tentacles
    // (Chaos Space Marines/Death Guard) as real, uncaveated corpus evidence that this gap is visible
    // today. Investigating the real bundled BSData found Scorpion Tail is Crusade Boons content (the "Chaos
    // Boons" selection group) - the same category BsdataDatasheetMapper's own IsGameModeGated doc
    // comment names by name as its motivating exclusion ("without it, /LivePlay would show abilities
    // like Chaos Boons/Mark of Chaos options that don't belong to matched play at all"). That gating
    // keeps it out of Chosen's always-present Abilities list (confirmed below) - but, unlike a
    // Statline-scoped ability, it's still reachable through the same on-demand
    // Datasheet.TryResolveAbility path a wargear-granted ability like Vexilla already uses
    // (ArmyRosterEnricher.ResolveWargearItem falls back to it for any Weapons-list item that isn't a
    // weapon profile name) - so a real import naming it as a wargear item still resolves it as a
    // present ability, and this test proves that real end-to-end path actually applies its
    // unconditional Attacks effect against a real BSData-resolved melee weapon.
    [Fact]
    public void ScorpionTail_IsExcludedFromChosensAbilitiesList_ButStillResolvesAndRendersWhenNamedAsWargear()
    {
        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var catalogue = ResolvedBsdataCatalogue.Build(source, "Chaos - Chaos Space Marines.json");
        var datasheet = catalogue.ResolveDatasheet("Chosen");
        datasheet.Abilities.Should().NotContain(a => a.Name == "Scorpion Tail");

        var parsed = new ParsedArmyList(
            Name: "Test Army", PointsSpent: 0, Faction: ["Chaos", "Chaos Space Marines"], Detachments: [],
            ForceDisposition: "Test", BattleSize: "Incursion", PointsLimit: 1000, AttachmentGroups: [],
            StandaloneUnits:
            [
                new ParsedUnit(
                    "Chosen",
                    [new ParsedModelGroup("Chosen", 1, ["Accursed weapon", "Scorpion Tail"])], [])
            ]);

        var roster = ArmyRosterEnricher.Enrich(parsed, catalogue);
        var chosen = roster.Units.Single();
        var classifications = ClassificationFixtures.CheckedIn;

        var view = AttachedUnitAggregator.Build(chosen, classifications);

        view.Abilities.Should().ContainSingle(e => e.Ability.Name == "Scorpion Tail");
        var weapon = view.Weapons.Should().ContainSingle(w => w.Profile.Name == "Accursed weapon").Subject;
        var contribution = weapon.Contributions.Single();
        contribution.AttacksContributions.Should()
            .ContainSingle(c => c.SourceAbility.Name == "Scorpion Tail" && c.Amount == 1);
        weapon.TotalAttacks.Should().Be(contribution.PerModelAttacks + 1);

        var block = LivePlayModel.BuildUnitBlock(view);
        var weaponRow = block.MeleeWeapons.Should().ContainSingle(w => w.Entry.Profile.Name == "Accursed weapon")
            .Subject;
        weaponRow.ShowsBreakdownTrigger.Should().BeTrue();
        var groupWideLine = weaponRow.GroupWideAttacksLines.Should().ContainSingle().Subject;
        groupWideLine.SourceAbility.Name.Should().Be("Scorpion Tail");
        groupWideLine.Amount.Should().Be(1);
    }

    [Fact]
    public void ChaosLordWithChanceForGlory_RecordsFourNotAppliedEffects_AndLeavesTheDaemonHammerUnmutated()
    {
        var parsed = new ArmyListParser().Parse(RealExportText("gw-app-export-chaos-lord-terminator-armour.txt"));
        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var fileName = BsdataFactionResolver.ResolveStartingFileName(parsed.Faction, source.ListFileNames());
        var roster = ArmyRosterEnricher.Enrich(parsed, ResolvedBsdataCatalogue.Build(source, fileName));
        var chaosLord = roster.Units.Single(u => u.Name == "Chaos Lord");

        var view = AttachedUnitAggregator.Build(chaosLord, ClassificationFixtures.CheckedIn);

        var hammer = view.Weapons.Single(w => w.Name == "Daemon hammer");
        hammer.Profile.S.ContributingAbilities.Should().BeEmpty();
        hammer.Profile.Ap.ContributingAbilities.Should().BeEmpty();
        hammer.Profile.D.ContributingAbilities.Should().BeEmpty();
        hammer.Contributions.Single().AttacksContributions.Should().BeEmpty();
        hammer.NotAppliedEffects
            .Where(e => e.SourceAbility.Name == "Chance for Glory")
            .Select(e => (e.Characteristic, e.Amount, e.Condition.UsageLimit))
            .Should().BeEquivalentTo(new (string, int, UsageLimit?)[]
            {
                ("S", 1, UsageLimit.OncePerBattle),
                ("A", 1, UsageLimit.OncePerBattle),
                ("AP", -1, UsageLimit.OncePerBattle),
                ("D", 1, UsageLimit.OncePerBattle)
            });
    }

    [Fact]
    public void ChaosLordWithChanceForGloryActivated_ImprovesTheDaemonHammer_AndRecordsNothingNotApplied()
    {
        var parsed = new ArmyListParser().Parse(RealExportText("gw-app-export-chaos-lord-terminator-armour.txt"));
        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var fileName = BsdataFactionResolver.ResolveStartingFileName(parsed.Faction, source.ListFileNames());
        var roster = ArmyRosterEnricher.Enrich(parsed, ResolvedBsdataCatalogue.Build(source, fileName));
        var chaosLord = roster.Units.Single(u => u.Name == "Chaos Lord");
        var baseline = AttachedUnitAggregator.Build(chaosLord, ClassificationFixtures.CheckedIn)
            .Weapons.Single(w => w.Name == "Daemon hammer");
        chaosLord.ConditionActivations = ActivationFixtures.Condition("Chance for Glory");

        var view = AttachedUnitAggregator.Build(chaosLord, ClassificationFixtures.CheckedIn);

        var hammer = view.Weapons.Single(w => w.Name == "Daemon hammer");
        Number(hammer.Profile.S).Should().Be(Number(baseline.Profile.S) + 1);
        Number(hammer.Profile.Ap).Should().Be(Number(baseline.Profile.Ap) - 1);
        hammer.Profile.D.Value.Should().NotBe(baseline.Profile.D.Value);
        new[] { hammer.Profile.S, hammer.Profile.Ap, hammer.Profile.D }.Should().OnlyContain(v =>
            v.ContributingAbilities.Count == 1 && v.ContributingAbilities[0].Name == "Chance for Glory");
        hammer.Contributions.Single().AttacksContributions.Should()
            .ContainSingle(c => c.SourceAbility.Name == "Chance for Glory" && c.Amount == 1);
        hammer.NotAppliedEffects.Should().NotContain(e => e.SourceAbility.Name == "Chance for Glory");
    }

    [Fact]
    public void ChaosLordWithChanceForGlory_ReportsOneOncePerBattleCondition()
    {
        var parsed = new ArmyListParser().Parse(RealExportText("gw-app-export-chaos-lord-terminator-armour.txt"));
        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var fileName = BsdataFactionResolver.ResolveStartingFileName(parsed.Faction, source.ListFileNames());
        var roster = ArmyRosterEnricher.Enrich(parsed, ResolvedBsdataCatalogue.Build(source, fileName));
        var chaosLord = roster.Units.Single(u => u.Name == "Chaos Lord");

        var view = AttachedUnitAggregator.Build(chaosLord, ClassificationFixtures.CheckedIn);

        var toggle = view.ActivatableConditions.Where(c => c.Ability.Name == "Chance for Glory")
            .Should().ContainSingle().Which.Should().BeOfType<ConditionToggle>().Subject;
        toggle.ConditionText.Should().BeEmpty();
        toggle.UsageLimit.Should().Be(UsageLimit.OncePerBattle);
        toggle.IsActive.Should().BeFalse();
    }

    private static int Number(ScalarCharacteristicView view) => ((NumericCharacteristicValue)view.Value).Value;

    private static string RealExportText(string fileName, [CallerFilePath] string here = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "data", fileName));
}