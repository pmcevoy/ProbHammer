using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class WeaponProfileTests
{
    [Fact]
    public void EqualityKey_distinguishes_weapons_that_differ_only_in_verbatim_keyword_text()
    {
        // Regression: two structurally-identical weapons whose source data spells the same rule
        // differently (e.g. "Cleave" vs "Blast" - see datasheet-catalogue's verbatim keyword text
        // requirement) must not silently collapse into one AggregateWeaponEntry - see
        // domain-model-11e.md's AggregateWeaponEntry note and design.md's Risks/Trade-offs.
        var cleave = new MeleeWeapon("Chainsword", 4, 3, 4, -1, 1) { KeywordsText = ["Cleave"] };
        var plain = new MeleeWeapon("Chainsword", 4, 3, 4, -1, 1) { KeywordsText = [] };

        cleave.EqualityKey().Should().NotBe(plain.EqualityKey());
    }

    [Fact]
    public void EqualityKey_treats_identical_verbatim_keyword_text_as_equal()
    {
        var a = new MeleeWeapon("Chainsword", 4, 3, 4, -1, 1) { KeywordsText = ["Devastating Wounds"] };
        var b = new MeleeWeapon("Chainsword", 4, 3, 4, -1, 1) { KeywordsText = ["Devastating Wounds"] };

        a.EqualityKey().Should().Be(b.EqualityKey());
    }
}
