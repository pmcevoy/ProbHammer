using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class InvulnerableSaveResolutionTests
{
    private const string FixtureFile = "invulnerable-save-scenarios.json";

    private static InvulnerableSave Resolve(string entryName)
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

        save.MeleeInSv.Should().Be(4);
        save.RangedInSv.Should().Be(4);
        save.Caveated.Should().BeFalse();
        save.CaveatAbility.Should().BeNull();
    }

    [Fact]
    public void Parenthetical_ranged_restriction_resolves_without_consulting_any_ability()
    {
        var save = Resolve("Parenthetical Ranged Save Model");

        save.RangedInSv.Should().Be(5);
        save.MeleeInSv.Should().Be(0);
        save.Caveated.Should().BeFalse();
        save.CaveatAbility.Should().BeNull();
    }

    [Fact]
    public void Bare_footnote_resolves_via_a_locally_nested_ability()
    {
        // Makari's shape: the linked ability sits directly in the same entry's own Profiles,
        // not behind any link.
        var save = Resolve("Local Ability Save Model");

        save.Caveated.Should().BeTrue();
        save.MeleeInSv.Should().Be(2);
        save.RangedInSv.Should().Be(2);
        save.CaveatAbility.Should().NotBeNull();
        save.CaveatAbility!.Name.Should().Be("Invulnerable Save (2+*)");
        save.CaveatAbility.Text.Should().Contain("re-roll");
    }

    [Fact]
    public void Bare_footnote_resolves_via_an_infoLink_into_a_shared_profile()
    {
        // Canis Rex's shape: a "profile"-type infoLink into a standalone shared profile.
        var save = Resolve("Linked Ability Save Model");

        save.Caveated.Should().BeTrue();
        save.MeleeInSv.Should().Be(5);
        save.RangedInSv.Should().Be(5);
        save.CaveatAbility.Should().NotBeNull();
        save.CaveatAbility!.Name.Should().Be("Invulnerable Save (5+*)");
        save.CaveatAbility.Text.Should().Contain("ranged attacks");
    }

    [Fact]
    public void Split_with_one_footnoted_side_uses_the_unfootnoted_digit_as_the_placeholder()
    {
        // Howling Banshee's shape: "4+* / 5+" - the mapper never decides which side is melee vs
        // ranged (that needs the ability's text), so both fields get the one value known without
        // interpretation: the plain, unfootnoted 5.
        var save = Resolve("Split Save Model");

        save.Caveated.Should().BeTrue();
        save.MeleeInSv.Should().Be(5);
        save.RangedInSv.Should().Be(5);
        save.CaveatAbility.Should().NotBeNull();
        save.CaveatAbility!.Name.Should().Be("Invulnerable Save (4+*)");
        save.CaveatAbility.Text.Should().Contain("melee attacks");
    }

    [Fact]
    public void Same_named_abilities_on_different_entries_resolve_by_id_not_by_name()
    {
        // The real corpus's base catalogue has two profiles both named "Invulnerable Save (4+*)"
        // with opposite meanings (ranged vs melee) - this fixture mirrors that collision across
        // two different entries, each with its own infoLink targeting a different id. A name-only
        // lookup against a flattened, name-deduped ability list would resolve both entries to
        // whichever profile happened to be seen first; resolving by the specific infoLink's own
        // targetId must not.
        var saveA = Resolve("Collision Model A");
        var saveB = Resolve("Collision Model B");

        saveA.CaveatAbility!.Text.Should().Contain("ranged attacks");
        saveB.CaveatAbility!.Text.Should().Contain("melee attacks");
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
