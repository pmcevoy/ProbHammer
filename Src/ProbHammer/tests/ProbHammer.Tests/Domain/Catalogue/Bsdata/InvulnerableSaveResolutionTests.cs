using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class InvulnerableSaveResolutionTests
{
    private const string FixtureFile = "invulnerable-save-scenarios.json";

    private static InvulnerableSaveCharacteristicView Resolve(string entryName)
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), FixtureFile);
        var entry = BsdataNameResolver.Resolve(closure, entryName)!;
        var idIndex = BsdataNameResolver.BuildIdIndex(closure);
        var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
        var profileIndex = BsdataNameResolver.BuildProfileIdIndex(closure);
        var sheet = BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, profileIndex);
        return sheet.GetStatline(entryName).InSv;
    }

    [Fact]
    public void Plain_value_resolves_uniform_and_non_caveated()
    {
        var save = Resolve("Plain Save Model");

        save.OriginalValue.MeleeInSv.Should().Be(4);
        save.OriginalValue.RangedInSv.Should().Be(4);
        save.Value.MeleeInSv.Should().Be(4);
        save.Value.RangedInSv.Should().Be(4);
        save.IsCaveated.Should().BeFalse();
        save.ContributingAbilities.Should().BeEmpty();
    }

    [Fact]
    public void Parenthetical_ranged_restriction_resolves_without_consulting_any_ability()
    {
        var save = Resolve("Parenthetical Ranged Save Model");

        save.OriginalValue.RangedInSv.Should().Be(5);
        save.OriginalValue.MeleeInSv.Should().Be(0);
        save.Value.RangedInSv.Should().Be(5);
        save.Value.MeleeInSv.Should().Be(0);
        save.IsCaveated.Should().BeFalse();
        save.ContributingAbilities.Should().BeEmpty();
    }

    [Fact]
    public void Bare_footnote_resolves_via_a_locally_nested_ability()
    {
        // Makari's shape: the linked ability sits directly in the same entry's own Profiles,
        // not behind any link.
        var save = Resolve("Local Ability Save Model");

        save.IsCaveated.Should().BeTrue();
        save.OriginalValue.MeleeInSv.Should().Be(2);
        save.OriginalValue.RangedInSv.Should().Be(2);
        save.ContributingAbilities.Should().ContainSingle();
        save.ContributingAbilities[0].Name.Should().Be("Invulnerable Save (2+*)");
        save.ContributingAbilities[0].Text.Should().Contain("re-roll");
    }

    [Fact]
    public void Bare_footnote_via_an_infoLink_stays_caveated_pending_build_time_resolution()
    {
        // Canis Rex's shape: a "profile"-type infoLink into a standalone shared profile whose text
        // is an exact known-template match - the mapper no longer interprets that text itself
        // (unify-characteristic-effect-resolution retired InvulnerableSaveCaveatClassifier from
        // this parse-time path); it always stays caveated, with a uniform fallback of the raw
        // footnoted digit on both sides, deferring real resolution to
        // AttachedUnitAggregator's own Build-time baseline lookup.
        var save = Resolve("Linked Ability Save Model");

        save.IsCaveated.Should().BeTrue();
        save.OriginalValue.MeleeInSv.Should().Be(5);
        save.OriginalValue.RangedInSv.Should().Be(5);
        save.ContributingAbilities.Should().ContainSingle();
        save.ContributingAbilities[0].Name.Should().Be("Invulnerable Save (5+*)");
        save.ContributingAbilities[0].Text.Should()
            .Be("This model has a 5+ invulnerable save against ranged attacks.");
    }

    [Fact]
    public void Split_with_one_footnoted_side_stays_caveated_pending_build_time_resolution()
    {
        // Howling Banshee's shape: "4+* / 5+" - the mapper no longer interprets the footnoted
        // side's linked ability text itself; it always stays caveated, with a uniform fallback of
        // the plain (non-footnoted) digit on both sides, deferring real resolution to
        // AttachedUnitAggregator's own Build-time baseline lookup.
        var save = Resolve("Split Save Model");

        save.IsCaveated.Should().BeTrue();
        save.OriginalValue.MeleeInSv.Should().Be(5);
        save.OriginalValue.RangedInSv.Should().Be(5);
        save.ContributingAbilities.Should().ContainSingle();
        save.ContributingAbilities[0].Name.Should().Be("Invulnerable Save (4+*)");
        save.ContributingAbilities[0].Text.Should()
            .Be("Models in this unit have a 4+ invulnerable save against melee attacks.");
    }

    [Fact]
    public void Same_named_abilities_on_different_entries_resolve_by_id_not_by_name()
    {
        // The real corpus's base catalogue has two profiles both named "Invulnerable Save (4+*)"
        // with opposite meanings (ranged vs melee) - this fixture mirrors that collision across
        // two different entries, each with its own infoLink targeting a different id. A name-only
        // lookup against a flattened, name-deduped ability list would resolve both entries to
        // whichever profile happened to be seen first; resolving by the specific infoLink's own
        // targetId must not. Both entries stay caveated (the mapper no longer classifies either
        // text - unify-characteristic-effect-resolution), but each carries its own distinct linked
        // ability text - still demonstrating by-id disambiguation.
        var saveA = Resolve("Collision Model A");
        var saveB = Resolve("Collision Model B");

        saveA.IsCaveated.Should().BeTrue();
        saveA.OriginalValue.MeleeInSv.Should().Be(4);
        saveA.OriginalValue.RangedInSv.Should().Be(4);
        saveA.ContributingAbilities.Should().ContainSingle();
        saveA.ContributingAbilities[0].Text.Should()
            .Be("This model has a 4+ invulnerable save against ranged attacks.");

        saveB.IsCaveated.Should().BeTrue();
        saveB.OriginalValue.MeleeInSv.Should().Be(4);
        saveB.OriginalValue.RangedInSv.Should().Be(4);
        saveB.ContributingAbilities.Should().ContainSingle();
        saveB.ContributingAbilities[0].Text.Should()
            .Be("This model has a 4+ invulnerable save against melee attacks.");
    }

    [Fact]
    public void Unrecognized_raw_shape_throws()
    {
        var act = () => Resolve("Unrecognized Shape Model");

        act.Should().Throw<AmbiguousCharacteristicException>()
            .Which.Characteristic.Should().Be("InSv");
    }

    [Fact]
    public void Footnote_with_no_reachable_candidate_ability_throws()
    {
        var act = () => Resolve("No Candidate Ability Model");

        act.Should().Throw<AmbiguousCharacteristicException>()
            .Which.Characteristic.Should().Be("InSv");
    }
}