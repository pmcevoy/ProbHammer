using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Catalogue;

public class InvulnerableSaveEffectResolverTests
{
    private static readonly Ability SomeAbility = new()
    {
        Name = "Some Ability",
        Text = "Irrelevant to this resolver - only Name/Text identity matters here.",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.OptionalGrant
    };

    [Fact]
    public void UniformGrant_ResolvesToEqualMeleeAndRanged()
    {
        var effect = new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(5, 5));

        var result =
            InvulnerableSaveEffectResolver.Resolve(effect, SomeAbility, InvulnerableSaveCharacteristicView.None);

        result.IsCaveated.Should().BeFalse();
        result.Value.Should().Be(new InvulnerableSave(5, 5));
        result.ContributingAbilities.Should().Equal(SomeAbility);
    }

    [Fact]
    public void RestrictedGrant_ResolvesWithTheAbsentSideAsNoInvulnerableSave()
    {
        var effect = new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(0, 4));

        var result =
            InvulnerableSaveEffectResolver.Resolve(effect, SomeAbility, InvulnerableSaveCharacteristicView.None);

        result.Value.Should().Be(new InvulnerableSave(0, 4));
    }

    [Fact]
    public void PreMutationOriginalValue_IsPreservedFromTheCurrentViewsOwnOriginal_NotTheEffectsStatedValue()
    {
        var current = InvulnerableSaveCharacteristicView.Resolved(new InvulnerableSave(6, 6));
        var effect = new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(5, 5));

        var result = InvulnerableSaveEffectResolver.Resolve(effect, SomeAbility, current);

        result.OriginalValue.Should().Be(new InvulnerableSave(6, 6));
        result.DerivedValue.Should().Be(new InvulnerableSave(5, 5));
    }

    [Fact]
    public void ReproducesShieldDomesResolvedInvulnerableSave()
    {
        // Ground-truth: resolving the Effect classified from Shield Dome's own real Name+Text
        // against Shield Dome's own Ability must reproduce the exact hand-computed expected result
        // below - proving the general resolver produces the same result a hand-authored rule would.
        var shieldDome = new Ability
        {
            Name = "Shield Dome",
            Text = "The bearer has a 5+ invulnerable save.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.OptionalGrant
        };
        var classification = RuleEffectClassifier.Classify(shieldDome.Name, shieldDome.Text);
        var effect = classification.Effects.Should().ContainSingle().Which
            .Should().BeOfType<InvulnerableSaveCharacteristicEffect>().Subject;

        var baseStatline = new Statline(12, 9, 3, 11, 6, 2);
        var resolved = InvulnerableSaveEffectResolver.Resolve(effect, shieldDome, baseStatline.InSv);

        var expected = InvulnerableSaveCharacteristicView.Resolved(
            baseStatline.InSv.OriginalValue, new InvulnerableSave(5, 5), [shieldDome]);

        resolved.IsCaveated.Should().Be(expected.IsCaveated);
        resolved.OriginalValue.Should().Be(expected.OriginalValue);
        resolved.DerivedValue.Should().Be(expected.DerivedValue);
        resolved.ContributingAbilities.Should().Equal(expected.ContributingAbilities);
    }
}