using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Web.Pages;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Real-corpus verification for `resolve-weapon-characteristic-effects` - drives the full
/// production pipeline (BsdataFactionResolver -> ResolvedBsdataCatalogue -> ArmyRosterEnricher.Enrich
/// -> AttachedUnitAggregator.Build) against the real bundled BSData snapshot
/// (`src/ProbHammer.Web/BsData/`) and the real checked-in `RuleClassificationBaseline`, not a
/// hand-built fixture - mirrors <c>UnifiedCharacteristicEffectResolutionRegressionTests</c>'s own
/// precedent and rationale.
///
/// A real, hand-verified finding drove this test's shape (recorded in tasks.md task 7.1): every one
/// of the 19 checked-in weapon-characteristic baseline entries is either caveated (15, including
/// this one - Adepta Sororitas' Zealot) or Crusade-mode-gated wargear this project's own
/// `IsGameModeGated` mechanism already excludes before it ever becomes a present `Ability` (the
/// remaining 4). No currently-importable ordinary roster produces a VISIBLE weapon-characteristic
/// mutation yet - so this test instead proves the two things real corpus data can actually confirm
/// today: the real `AttachedUnitAggregator.ApplyEffect` bug fixed alongside this change (design.md
/// D8) no longer crashes on Zealot specifically (it would have, before this change - Zealot's own
/// Target is Self, exactly the shape that threw), and a caveated match correctly leaves the real
/// printed weapon value untouched.
///
/// `render-weapon-characteristic-effects` (this change) extends this same real-corpus pipeline
/// through to `LivePlayModel.BuildUnitBlock` (task 5.1/5.2): Zealot's caveated match, invisible
/// end-to-end before this change, now surfaces as a real, visible name-marker + legend on the
/// Ministorum Priest's Power weapon row - the first real weapon-characteristic-effect result this
/// project has produced against actual bundled BSData, not just a hand-built fixture.</summary>
public class WeaponCharacteristicEffectRealCorpusTests
{
    private static string BundledBsDataRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web",
            "BsData"));

    private static string BundledBaselinePath([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web",
            "Data", "RuleEffectClassifications.json"));

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
        var baseline = RuleClassificationBaseline.Load(BundledBaselinePath());

        // The real, previously-crashing call: before design.md D8's fix, Zealot's own baseline entry
        // (Target: Self, Effects: [WeaponCharacteristicEffect(S), WeaponCharacteristicEffect(A)])
        // reached ApplyStatlineFlagRules' ApplyEffect and threw ArgumentOutOfRangeException.
        var view = AttachedUnitAggregator.Build(priest, baseline);

        view.Abilities.Should().ContainSingle(e => e.Ability.Name == "Zealot");

        var weapon = view.Weapons.Should().ContainSingle(w => w.Profile.Name == "Power weapon").Subject;
        weapon.Profile.S.IsCaveated.Should().BeFalse();
        weapon.Profile.S.Value.Should().Be((CharacteristicValue)4); // the real printed value, unmutated
        weapon.Profile.S.ContributingAbilities.Should().BeEmpty(); // Zealot is caveated - not applied
        weapon.UnresolvedAbilities.Should().ContainSingle(a => a.Name == "Zealot");
    }

    [Fact]
    public void MinistorumPriestWithZealot_RendersANameMarkerAndLegendNamingZealot()
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
        var baseline = RuleClassificationBaseline.Load(BundledBaselinePath());

        var view = AttachedUnitAggregator.Build(priest, baseline);
        var block = LivePlayModel.BuildUnitBlock(view);

        var weaponRow = block.MeleeWeapons.Should().ContainSingle(w => w.Entry.Profile.Name == "Power weapon").Subject;
        weaponRow.NameMarker.Should().Be("*");
        weaponRow.FlagLegend.Should().ContainSingle(l => l.Marker == "*" && l.Source.Name == "Zealot");
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
    // present ability, and this test proves that real end-to-end path actually applies the real,
    // checked-in, uncaveated baseline entry against a real BSData-resolved melee weapon.
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
        var baseline = RuleClassificationBaseline.Load(BundledBaselinePath());

        var view = AttachedUnitAggregator.Build(chosen, baseline);

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
}