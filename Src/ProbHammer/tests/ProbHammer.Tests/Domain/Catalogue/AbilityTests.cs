using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class AbilityTests
{
    [Fact]
    public void SimpleAbility_WithNoChoices_PreservesTextAsIs()
    {
        var ability = new Ability { Name = "And They Shall Know No Fear", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic };

        ability.Choices.Should().BeEmpty();
        ability.Text.Should().Be("...");
    }

    [Fact]
    public void AbilityWithChoices_RetainsIntroTextAlongsideStructuredChoices()
    {
        var ability = new Ability
        {
            Name = "Litanies of Hate",
            Text = "While on an objective marker, choose one of the following:",
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.Intrinsic,
            Choices =
            [
                new AbilityChoice("Rites of War", "This unit's melee weapons have the [LETHAL HITS] ability."),
                new AbilityChoice("Rites of Battle", "Add 1 to this unit's Ballistic Skill characteristic.")
            ]
        };

        ability.Text.Should().StartWith("While on an objective marker");
        ability.Choices.Should().HaveCount(2);
        ability.Choices.Should().Contain(c => c.Name == "Rites of War");
    }

    [Fact]
    public void AbilityScope_ModelOnly_IsExplicit()
    {
        var ability = new Ability { Name = "Icon of Despair", Text = "...", Scope = AbilityScope.Model, Origin = AbilityOrigin.Intrinsic };

        ability.Scope.Should().Be(AbilityScope.Model);
    }

    [Fact]
    public void AbilityScope_WholeUnit_IsExplicit()
    {
        var ability = new Ability { Name = "Oath of Moment", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic };

        ability.Scope.Should().Be(AbilityScope.Unit);
    }
}
