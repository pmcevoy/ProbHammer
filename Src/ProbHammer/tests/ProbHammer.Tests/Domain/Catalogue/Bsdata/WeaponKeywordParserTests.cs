using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class WeaponKeywordParserTests
{
    private static MeleeWeapon BareMelee() => new("Test Weapon", 1, 4, 4, 0, 1);
    private static RangedWeapon BareRanged() => new("Test Weapon", 12, 1, 4, 4, 0, 1);

    [Fact]
    public void Multiple_comma_separated_recognized_tokens()
    {
        // Real data: Imperium - Black Templars.json
        var result = WeaponKeywordParser.Apply(BareRanged(), "Anti-infantry 4+, Devastating Wounds");

        result.DevastatingWounds.Should().BeTrue();
        result.Anti.Should().ContainKey("infantry").WhoseValue.Should().Be(4);
        result.KeywordsText.Should().Equal("Anti-infantry 4+", "Devastating Wounds");
    }

    [Fact]
    public void Value_carrying_token()
    {
        var result = WeaponKeywordParser.Apply(BareRanged(), "Sustained Hits 1");

        result.SustainedHits.Should().Be(1);
        result.KeywordsText.Should().Equal("Sustained Hits 1");
    }

    [Fact]
    public void Ignores_cover_pistol_torrent_all_set_independently()
    {
        // Real data: Imperium - Black Templars.json
        var result = WeaponKeywordParser.Apply(BareRanged(), "Ignores Cover, Pistol, Torrent");

        result.IgnoresCover.Should().BeTrue();
        result.Pistol.Should().BeTrue();
        result.Torrent.Should().BeTrue();
        result.KeywordsText.Should().Equal("Ignores Cover", "Pistol", "Torrent");
    }

    [Fact]
    public void No_keywords_dash_produces_no_flags_and_empty_verbatim_list()
    {
        var result = WeaponKeywordParser.Apply(BareMelee(), "-");

        result.Should().BeEquivalentTo(BareMelee(), opts => opts.Excluding(w => w.KeywordsText));
        result.KeywordsText.Should().BeEmpty();
    }

    [Fact]
    public void Token_with_no_corresponding_flag_is_retained_verbatim_without_asserting_a_flag()
    {
        // Real data: Imperium - Black Templars.json
        var result = WeaponKeywordParser.Apply(BareRanged(), "Anti-Character 5+, Precision");

        result.Anti.Should().ContainKey("character").WhoseValue.Should().Be(5);
        result.KeywordsText.Should().Contain("Precision");
        // No existing WeaponProfile flag corresponds to Precision - nothing to assert false on
        // beyond confirming every other flag stayed at its bare default.
        result.Torrent.Should().BeFalse();
        result.Blast.Should().BeFalse();
        result.Pistol.Should().BeFalse();
    }

    [Fact]
    public void Alternate_spelling_of_an_existing_flag_is_not_inferred()
    {
        var result = WeaponKeywordParser.Apply(BareMelee(), "Cleave");

        result.Blast.Should().BeFalse();
        result.KeywordsText.Should().Equal("Cleave");

        var closeCombat = WeaponKeywordParser.Apply(BareRanged(), "Close Combat");
        closeCombat.Pistol.Should().BeFalse();
        closeCombat.KeywordsText.Should().Equal("Close Combat");
    }

    [Fact]
    public void A_keyword_that_also_maps_to_a_flag_is_still_retained_verbatim()
    {
        var result = WeaponKeywordParser.Apply(BareMelee(), "Devastating Wounds");

        result.DevastatingWounds.Should().BeTrue();
        result.KeywordsText.Should().Equal("Devastating Wounds");
    }
}
