using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

public class WeaponKeywordGrantRealCorpusTests
{
    private static string RepoPath(string[] parts, [CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine([Path.GetDirectoryName(here)!, "..", "..", "..", "..", .. parts]));

    private static ArmyRoster BuildCapturedRoster(string exportFileName)
    {
        var parsed = new ArmyListParser().Parse(File.ReadAllText(RepoPath(["data", exportFileName])));
        var source = new LocalDiskBsdataCatalogueSource(RepoPath(["src", "ProbHammer.Web", "BsData"]));
        var catalogue = ResolvedBsdataCatalogue.Build(source,
            BsdataFactionResolver.ResolveStartingFileName(parsed.Faction, source.ListFileNames()));
        var roster = ArmyRosterEnricher.Enrich(parsed, catalogue);
        DetachmentRuleInboundAbilityResolver.Apply(roster.Units, roster.Detachments, ClassificationFixtures.CheckedIn);
        return roster;
    }

    [Fact]
    public void EmpyricWellspring_ReachesTheLegionariesRangedWeapons_AsANotAddedStrengthEffect()
    {
        var unit = BuildCapturedRoster("gw-app-export-masters-of-the-maelstrom.txt").Units
            .Single(u => u.InboundAbilities.Any(a => a.Name == "Empyric Wellspring"));

        var view = AttachedUnitAggregator.Build(unit, ClassificationFixtures.CheckedIn);

        view.Weapons.Where(w => w.Profile.Type == WeaponType.Ranged)
            .Should().OnlyContain(w => w.NotAppliedEffects.Any(e =>
                e.SourceAbility.Name == "Empyric Wellspring" && e.Characteristic == "S"));
        view.Weapons.Where(w => w.Profile.Type == WeaponType.Melee)
            .Should().NotContain(w => w.NotAppliedEffects.Any(e => e.SourceAbility.Name == "Empyric Wellspring"));
    }

    [Fact]
    public void TouchedByTheWarp_AddsPsychicToTheReaveCaptainsWeapons_AndANativeKeywordSuppressesItsGrant()
    {
        var unit = BuildCapturedRoster("gw-app-export-masters-of-the-maelstrom.txt").Units
            .Single(u => u.Name.StartsWith("Red Corsairs Raiders"));

        var view = AttachedUnitAggregator.Build(unit, ClassificationFixtures.CheckedIn);

        view.Weapons.Where(w => w.Name == "Bolt pistol").Should().HaveCount(2)
            .And.ContainSingle(w => w.KeywordGrants.Any(g => g.SourceAbility.Name == "Touched by the Warp"));
        var powerSword = view.Weapons.Should().ContainSingle(w => w.Name == "Power sword").Subject;
        powerSword.Profile.KeywordsText.Should().Contain("Psychic");
        powerSword.NotAppliedKeywordGrants.Should().NotContain(g => g.Keyword == "Sustained Hits 1");
    }
}
