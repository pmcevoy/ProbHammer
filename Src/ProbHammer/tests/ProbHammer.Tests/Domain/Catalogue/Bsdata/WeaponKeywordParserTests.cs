using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class WeaponKeywordParserTests
{
    private static MeleeWeapon BareMelee() => new("Test Weapon", 1, 4, 4, 0, 1);
    private static RangedWeapon BareRanged() => new("Test Weapon", 12, 1, 4, 4, 0, 1);

    [Fact]
    public void Multiple_comma_separated_tokens_are_split_and_trimmed()
    {
        // Real data: Imperium - Black Templars.json
        var result = WeaponKeywordParser.Apply(BareRanged(), "Anti-infantry 4+, Devastating Wounds");

        result.KeywordsText.Should().Equal("Anti-infantry 4+", "Devastating Wounds");
    }

    [Fact]
    public void A_single_token_is_retained_verbatim()
    {
        var result = WeaponKeywordParser.Apply(BareRanged(), "Sustained Hits 1");

        result.KeywordsText.Should().Equal("Sustained Hits 1");
    }

    [Fact]
    public void Every_token_is_retained_verbatim_regardless_of_whether_it_is_a_known_mechanic()
    {
        // Real data: Imperium - Black Templars.json - none of these correspond to a formerly-typed
        // flag; tokenization never distinguishes recognized from unrecognized tokens.
        var result = WeaponKeywordParser.Apply(BareRanged(), "Anti-Character 5+, Precision, Cleave, Close Combat");

        result.KeywordsText.Should().Equal("Anti-Character 5+", "Precision", "Cleave", "Close Combat");
    }

    [Fact]
    public void No_keywords_dash_produces_an_empty_verbatim_list()
    {
        var result = WeaponKeywordParser.Apply(BareMelee(), "-");

        result.Should().BeEquivalentTo(BareMelee(), opts => opts.Excluding(w => w.KeywordsText));
        result.KeywordsText.Should().BeEmpty();
    }

    [Fact]
    public void Blank_keywords_text_produces_an_empty_verbatim_list()
    {
        var result = WeaponKeywordParser.Apply(BareMelee(), "");

        result.KeywordsText.Should().BeEmpty();
    }

    [Fact]
    public void Empty_entries_between_commas_are_dropped()
    {
        var result = WeaponKeywordParser.Apply(BareMelee(), "Pistol, , Torrent");

        result.KeywordsText.Should().Equal("Pistol", "Torrent");
    }
}