using System.Text.Json;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class RuleClassificationTests
{
    [Fact]
    public void RuleTarget_EachSubtype_IsConstructibleAndDistinct()
    {
        RuleTarget self = new SelfRuleTarget();
        RuleTarget attachedUnit = new AttachedUnitRuleTarget();
        RuleTarget keyword = new KeywordRuleTarget("ADEPTUS ASTARTES");
        RuleTarget unconditional = new UnconditionalRuleTarget();

        self.Should().NotBe(attachedUnit);
        self.Should().NotBe(keyword);
        self.Should().NotBe(unconditional);
        attachedUnit.Should().NotBe(keyword);
        attachedUnit.Should().NotBe(unconditional);
        keyword.Should().NotBe(unconditional);
    }

    [Fact]
    public void WeaponSelector_EachSubtype_IsConstructibleAndDistinct()
    {
        WeaponSelector all = new AllWeapons();
        WeaponSelector weaponClass = new WeaponClass(WeaponType.Melee);
        WeaponSelector named = new NamedWeapon("Bolt Rifle");

        all.Should().NotBe(weaponClass);
        all.Should().NotBe(named);
        weaponClass.Should().NotBe(named);
    }

    [Fact]
    public void WeaponCharacteristicEffect_RoundTripsThroughJsonPolymorphicSerialization()
    {
        // Mirrors how RuleClassificationBaseline already round-trips ScalarCharacteristicEffect/
        // InvulnerableSaveCharacteristicEffect - same Options, same base-typed (de)serialization.
        CharacteristicEffect effect = new WeaponCharacteristicEffect(
            new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 3);

        var json = JsonSerializer.Serialize(effect, RuleClassificationBaseline.Options);
        var roundTripped = JsonSerializer.Deserialize<CharacteristicEffect>(json, RuleClassificationBaseline.Options);

        roundTripped.Should().Be(effect);
    }

    [Fact]
    public void WeaponCharacteristicEffect_RoundTripsThroughRuleClassificationBaselineFileLoadSave()
    {
        // A stronger check than the raw System.Text.Json round-trip above (classify-weapon-
        // characteristic-effects task 6.1): the baseline file has its own Options/converter setup
        // (RuleClassificationBaseline.Save/Load), so this exercises that exact path rather than
        // assuming it behaves identically to a bare JsonSerializer call.
        var entry = new RuleClassificationBaselineEntry(
            "Improve the Strength and Attacks characteristics of melee weapons equipped by this model by 3.",
            new SelfRuleTarget(),
            [
                new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 3),
                new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "A", EffectVerb.Improve, 3)
            ]);
        var baseline = RuleClassificationBaseline.FromEntries([entry]);
        var path = Path.Combine(Path.GetTempPath(), $"weapon-characteristic-baseline-roundtrip-{Guid.NewGuid()}.json");

        try
        {
            baseline.Save(path);
            var reloaded = RuleClassificationBaseline.Load(path);

            reloaded.TryGet(entry.Text, out var reloadedEntry).Should().BeTrue();
            reloadedEntry.Effects.Should().Equal(entry.Effects);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RuleClassification_ConstructedWithTargetOnly_DefaultsEffectsToEmptyNotNull()
    {
        var classification = new RuleClassification(new SelfRuleTarget());

        classification.Effects.Should().NotBeNull();
        classification.Effects.Should().BeEmpty();
    }

    [Fact]
    public void RuleClassification_ConstructedWithoutIsCaveated_DefaultsToFalse()
    {
        // The "boring default" RuleClassificationDiff.DefaultClassification reads off this type's
        // own serialization for schema-growth backfill.
        var classification = new RuleClassification(new SelfRuleTarget(), []);

        classification.IsCaveated.Should().BeFalse();
    }
}