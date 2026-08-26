using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class BsdataDatasheetMapperTests
{
    private static ProbHammer.Core.Domain.Catalogue.Datasheet Build(string startingFileName, string entryName)
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), startingFileName);
        var entry = BsdataNameResolver.Resolve(closure, entryName)!;
        var idIndex = BsdataNameResolver.BuildIdIndex(closure);
        var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
        var profileIndex = BsdataNameResolver.BuildProfileIdIndex(closure);
        return BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, profileIndex);
    }

    [Fact]
    public void Single_model_unit_produces_one_statline_including_InSv()
    {
        // Imperium - Imperial Fists.json's Darnath Lysander: a Unit-typeName profile directly on
        // the entry itself (not nested on a child) - the single-model case.
        var sheet = Build("imperial-fists-single-model.json", "Darnath Lysander");

        sheet.Statlines.Should().ContainSingle();
        var (name, statline) = sheet.Statlines[0];
        name.Should().Be("Darnath Lysander");
        statline.Should().Be(new ProbHammer.Core.Domain.Catalogue.Statline(M: 5, T: 5, Sv: 2, W: 7, Ld: 6, Oc: 1)
            { InSv = 4 });
    }

    [Fact]
    public void Single_model_unit_extracts_its_melee_weapon_and_abilities()
    {
        var sheet = Build("imperial-fists-single-model.json", "Darnath Lysander");

        var fistOfDorn = sheet.ResolveWeaponProfile("Fist of Dorn");
        fistOfDorn.Should().BeOfType<ProbHammer.Core.Domain.Catalogue.MeleeWeapon>();
        fistOfDorn.A.Should().Be(ProbHammer.Core.Domain.Catalogue.DiceExpression.Fixed(5));
        fistOfDorn.Skill.Should().Be(2);
        fistOfDorn.S.Should().Be(10);
        fistOfDorn.Ap.Should().Be(-3);
        fistOfDorn.D.Should().Be(ProbHammer.Core.Domain.Catalogue.DiceExpression.Fixed(3));
        fistOfDorn.DevastatingWounds.Should().BeTrue();
        fistOfDorn.KeywordsText.Should().Equal("Devastating Wounds");

        sheet.Abilities.Select(a => a.Name).Should()
            .Contain(["Icon of Obstinacy", "Rampart", "Leader", "Inspiring Commander"]);
    }

    [Fact]
    public void Squad_unit_produces_one_deduped_statline_per_distinct_model_type_in_declared_order()
    {
        // Imperium - Black Templars.json's Crusader Squad: Sword Brother/Initiate/Neophyte each
        // carry their own Unit-typeName profile nested within distinct wargear-option child
        // entries - Initiate's profile is repeated across 4 sibling weapon-loadout entries and
        // Neophyte's across 2, all byte-identical duplicates of the same underlying model, not
        // distinct variants (see tasks.md 4.4/design.md's "repeated same-named child entry" risk).
        var sheet = Build("black-templars-crusader-squad.json", "Crusader Squad");

        sheet.Statlines.Select(s => s.Name).Should().Equal("Sword Brother", "Initiate", "Neophyte");
    }

    [Fact]
    public void Squad_unit_statlines_have_expected_values()
    {
        var sheet = Build("black-templars-crusader-squad.json", "Crusader Squad");

        sheet.GetStatline("Sword Brother").Should()
            .Be(new ProbHammer.Core.Domain.Catalogue.Statline(M: 6, T: 4, Sv: 3, W: 2, Ld: 6, Oc: 2));
        sheet.GetStatline("Initiate").Should()
            .Be(new ProbHammer.Core.Domain.Catalogue.Statline(M: 6, T: 4, Sv: 3, W: 2, Ld: 6, Oc: 2));
        sheet.GetStatline("Neophyte").Should()
            .Be(new ProbHammer.Core.Domain.Catalogue.Statline(M: 6, T: 4, Sv: 4, W: 2, Ld: 6, Oc: 2));
    }

    [Fact]
    public void Statline_reachable_only_via_a_profile_type_infoLink_is_still_collected()
    {
        // Chaos - Chaos Space Marines.json's Legionaries squad: the troop model's ("Legionary")
        // Unit-typeName profile is not nested inside any of the squad's own selectionEntries -
        // it exists only as a standalone entry in the catalogue's top-level "sharedProfiles" list,
        // reached through a "profile"-type infoLink (see design.md's "entryLink target location"
        // risk - the same applies to infoLinks pointing at a profile rather than an entry/group).
        var sheet = Build("chaos-space-marines-legionaries.json", "Legionaries");

        sheet.Statlines.Select(s => s.Name).Should().Contain("Legionaries");
    }

    [Fact]
    public void CategoryLinks_populate_the_datasheets_top_level_keywords()
    {
        var sheet = Build("black-templars-crusader-squad.json", "Crusader Squad");

        sheet.Keywords.Should().Contain("Infantry");
        sheet.Keywords.Should().Contain("Black Templars");
        sheet.Keywords.Should().NotContain("Faction: Black Templars");
        sheet.Keywords.Should().Contain("Crusader Squad");
    }

    [Fact]
    public void A_child_model_entrys_own_categoryLinks_populate_only_that_models_own_keywords()
    {
        var sheet = Build("black-templars-crusader-squad.json", "Crusader Squad");

        sheet.GetModelKeywords("Sword Brother").Should().Contain("Psyker");
        sheet.GetModelKeywords("Initiate").Should().NotContain("Psyker").And.BeEmpty();
        sheet.Keywords.Should().NotContain("Psyker");
    }

    [Fact]
    public void No_composition_validation_or_points_data_leaks_onto_produced_objects()
    {
        // BsSelectionEntry/BsProfile simply have no properties for constraints, modifiers,
        // conditionGroups, associations, or costs - there is no field on Datasheet, Statline,
        // WeaponProfile, or Ability such data could leak onto even if it were deserialized.
        var sheet = Build("black-templars-crusader-squad.json", "Crusader Squad");

        sheet.Should().NotBeNull();
        // Compiling this test at all is part of the guarantee: Datasheet/Statline/WeaponProfile/
        // Ability's public shape has no points/constraint-related member to assert against.
    }
}