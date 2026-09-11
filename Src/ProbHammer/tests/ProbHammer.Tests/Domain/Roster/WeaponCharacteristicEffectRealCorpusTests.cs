using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Roster;

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
/// printed weapon value untouched.</summary>
public class WeaponCharacteristicEffectRealCorpusTests
{
    private static string BundledBsDataRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web", "BsData"));

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
    }
}
