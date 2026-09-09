using System.Text.Json;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Unit tests for BsdataDatasheetMapper's closed-world tier-1/tier-2
/// characteristic-modifier classifier (characteristic-modifier-caveats) - hand-built
/// BsSelectionEntry objects, not fixture JSON, since the classifier's own logic depends only on
/// BsModifier/BsCondition shape, not the full closure-resolution machinery the other
/// BsdataDatasheetMapperTests fixtures exercise.</summary>
public class CharacteristicModifierClassificationTests
{
    private const string OcFieldId = "bef7-942a-1a23-59f8";
    private const string MFieldId = "e703-ecb6-5ce7-aec1";
    private const string InSvFieldId = "55a7-5b54-c60d-11dc";

    private static JsonElement JsonValue(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private static Datasheet Build(BsSelectionEntry entry) =>
        BsdataDatasheetMapper.BuildDatasheet(
            entry,
            idIndex: new Dictionary<string, BsSelectionEntry>(),
            groupIdIndex: new Dictionary<string, BsSelectionEntryGroup>(),
            profileIdIndex: new Dictionary<string, BsProfile>());

    private static BsSelectionEntry WithModifier(BsModifier modifier) =>
        new()
        {
            Id = "grant-1",
            Name = "Test Grant",
            Type = "upgrade",
            Modifiers = [modifier]
        };

    [Fact]
    public void A_modifier_with_no_condition_is_classified_tier_1()
    {
        var entry = WithModifier(new BsModifier { Field = OcFieldId, Type = "increment", Value = JsonValue("1") });

        var sheet = Build(entry);

        sheet.CharacteristicModifierCandidates.Should().ContainSingle();
        var candidate = sheet.CharacteristicModifierCandidates[0];
        candidate.EntryName.Should().Be("Test Grant");
        candidate.Characteristic.Should().Be("Oc");
        candidate.RawValue.Should().Be("1");
    }

    [Fact]
    public void An_InSv_targeting_modifier_is_classified_now_that_a_safe_consumer_exists()
    {
        // Real corpus case (unify-characteristic-effect-resolution): Black Templars' "Consecrating
        // Aura" Enhancement - tier 1, unconditional, previously discarded entirely because InSv was
        // excluded from CharacteristicFieldIds. It now classifies like any other tier-1 candidate,
        // since AttachedUnitAggregator resolves it through the same single baseline-driven pass as
        // every other characteristic-affecting ability.
        var entry = WithModifier(new BsModifier { Field = InSvFieldId, Type = "set", Value = JsonValue("\"4\"") });

        var sheet = Build(entry);

        sheet.CharacteristicModifierCandidates.Should().ContainSingle();
        sheet.CharacteristicModifierCandidates[0].Characteristic.Should().Be("InSv");
    }

    [Fact]
    public void A_modifier_scoped_to_its_own_granting_entry_is_classified_tier_2()
    {
        var entry = WithModifier(new BsModifier
        {
            Field = MFieldId,
            Type = "set",
            Value = JsonValue("\"9\\\"\""),
            Conditions =
            [
                new BsCondition { Field = "selections", ChildId = "grant-1", Scope = "self", Type = "atLeast" }
            ]
        });

        var sheet = Build(entry);

        sheet.CharacteristicModifierCandidates.Should().ContainSingle();
        sheet.CharacteristicModifierCandidates[0].Characteristic.Should().Be("M");
    }

    [Fact]
    public void A_modifier_gated_on_a_sibling_selection_is_left_unclassified()
    {
        var entry = WithModifier(new BsModifier
        {
            Field = OcFieldId,
            Type = "increment",
            Value = JsonValue("1"),
            Conditions =
            [
                new BsCondition { Field = "selections", ChildId = "some-other-entry", Scope = "self", Type = "atLeast" }
            ]
        });

        Build(entry).CharacteristicModifierCandidates.Should().BeEmpty();
    }

    [Fact]
    public void A_modifier_gated_on_an_unrecognized_condition_shape_is_left_unclassified()
    {
        // Real corpus shape: a self-referencing "selections" condition scoped to "roster" (Astra
        // Militarum's "Deficiency") - not locally bounded, so not tier 2 despite ChildId matching.
        var entry = WithModifier(new BsModifier
        {
            Field = OcFieldId,
            Type = "increment",
            Value = JsonValue("1"),
            Conditions =
            [
                new BsCondition { Field = "selections", ChildId = "grant-1", Scope = "roster", Type = "atLeast" }
            ]
        });

        Build(entry).CharacteristicModifierCandidates.Should().BeEmpty();
    }

    [Fact]
    public void A_modifier_gated_on_live_attachment_state_is_left_unclassified()
    {
        // Real corpus shape: "associations" field, not "selections" (e.g. Space Marines' "Ancient").
        var entry = WithModifier(new BsModifier
        {
            Field = OcFieldId,
            Type = "increment",
            Value = JsonValue("1"),
            Conditions =
            [
                new BsCondition { Field = "associations", ChildId = "any", Scope = "self", Type = "atLeast" }
            ]
        });

        Build(entry).CharacteristicModifierCandidates.Should().BeEmpty();
    }

    [Fact]
    public void An_unrecognized_field_is_left_unclassified()
    {
        var entry = WithModifier(new BsModifier
            { Field = "some-unmapped-field-id", Type = "set", Value = JsonValue("1") });

        Build(entry).CharacteristicModifierCandidates.Should().BeEmpty();
    }

    [Fact]
    public void A_datasheet_with_no_classifiable_modifiers_exposes_none()
    {
        var entry = new BsSelectionEntry { Id = "e1", Name = "Plain Entry", Type = "model" };

        Build(entry).CharacteristicModifierCandidates.Should().BeEmpty();
    }

    [Fact]
    public void Classification_never_mutates_the_datasheets_own_base_statline_values()
    {
        var entry = new BsSelectionEntry
        {
            Id = "e1",
            Name = "Model With Grant",
            Type = "model",
            Modifiers = [new BsModifier { Field = OcFieldId, Type = "increment", Value = JsonValue("1") }],
            Profiles =
            [
                new BsProfile
                {
                    Name = "Model With Grant",
                    TypeName = "Unit",
                    Characteristics =
                    [
                        new BsCharacteristic { Name = "M", Text = "6\"" },
                        new BsCharacteristic { Name = "T", Text = "4" },
                        new BsCharacteristic { Name = "Sv", Text = "3+" },
                        new BsCharacteristic { Name = "W", Text = "2" },
                        new BsCharacteristic { Name = "LD", Text = "6+" },
                        new BsCharacteristic { Name = "OC", Text = "1" }
                    ]
                }
            ]
        };

        var sheet = Build(entry);

        sheet.CharacteristicModifierCandidates.Should().ContainSingle();
        var (_, statline) = sheet.Statlines[0];
        ((NumericCharacteristicValue)statline.Oc.Value).Value.Should().Be(1);
        statline.Oc.IsCaveated.Should().BeFalse();
    }
}