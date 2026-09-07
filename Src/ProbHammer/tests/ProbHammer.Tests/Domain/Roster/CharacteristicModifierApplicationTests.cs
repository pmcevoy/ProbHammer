using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Covers characteristic-modifier-caveats' presence-gated application requirements against
/// a hand-built Datasheet carrying a classified CharacteristicModifierCandidate, via
/// AttachedUnitAggregator.Build directly - mirrors StatlineFlagRuleTests' own construction
/// pattern.</summary>
public class CharacteristicModifierApplicationTests
{
    private static readonly Ability GrantingAbility = new()
    {
        Name = "Auric Mantle",
        Text = "A relic that toughens its bearer.",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.Enhancement
    };

    private static Datasheet DatasheetWithCandidate(string entryName, string characteristic) =>
        new(
            "Custodian Guard", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Custodian Guard", new Statline(6, 6, 2, 4, 7, 2))], weaponProfiles: [],
            characteristicModifierCandidates:
            [
                new CharacteristicModifierCandidate(entryName, characteristic, "1")
            ]);

    [Fact]
    public void A_present_candidate_caveats_the_targeted_characteristic_with_no_derived_value()
    {
        var datasheet = DatasheetWithCandidate("Auric Mantle", "W");
        var unit = new Unit(datasheet, [],
            [new ModelLine("Custodian Guard", [], count: 1, abilities: [GrantingAbility])]);

        var view = AttachedUnitAggregator.Build(unit);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.W.IsCaveated.Should().BeTrue();
        entry.Statline.W.DerivedValue.Should().BeNull();
        entry.Statline.W.ContributingAbilities.Should().ContainSingle(a => a.Name == "Auric Mantle");
        // pre-mutation catalogue value must still be readable via OriginalValue
        entry.Statline.W.OriginalValue.Should().Be((CharacteristicValue)4);
    }

    [Fact]
    public void An_absent_candidate_produces_no_caveat()
    {
        var datasheet = DatasheetWithCandidate("Auric Mantle", "W");
        var unit = new Unit(datasheet, [], [new ModelLine("Custodian Guard", [], count: 1, abilities: [])]);

        var view = AttachedUnitAggregator.Build(unit);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.W.IsCaveated.Should().BeFalse();
        entry.Statline.W.ContributingAbilities.Should().BeEmpty();
    }

    [Fact]
    public void A_casualty_removes_the_caveat_on_the_next_build()
    {
        var datasheet = DatasheetWithCandidate("Auric Mantle", "W");
        var unit = new Unit(datasheet, [],
            [new ModelLine("Custodian Guard", [], count: 1, abilities: [GrantingAbility])]);
        unit.ModelLines[0].RemoveCasualties(1);

        var view = AttachedUnitAggregator.Build(unit);

        // the whole statline entry still reports (RemainingCount 0 / InitialCount 1, "persist at
        // zero" - AttachedUnitAggregator's own documented behavior), but its own Ability is no
        // longer present, so the caveat must not apply.
        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.RemainingCount.Should().Be(0);
        entry.Statline.W.IsCaveated.Should().BeFalse();
    }

    [Fact]
    public void A_candidate_does_not_leak_onto_a_different_unit_lacking_its_granting_selection()
    {
        var bodyguardDatasheet = DatasheetWithCandidate("Auric Mantle", "W");
        var bodyguard = new Unit(bodyguardDatasheet, [],
            [new ModelLine("Custodian Guard", [], count: 4, abilities: [GrantingAbility])]);

        var wardenDatasheet = new Datasheet(
            "Custodian Warden", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Custodian Warden", new Statline(6, 6, 2, 5, 7, 2))], weaponProfiles: []);
        var warden = new Unit(wardenDatasheet, [], [new ModelLine("Custodian Warden", [], count: 1)]);

        var attachedUnit = new AttachedUnit(bodyguard, [warden]);

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Guard" && s.Statline.W.IsCaveated);
        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Warden" && !s.Statline.W.IsCaveated);
    }

    [Fact]
    public void
        A_data_derived_candidate_and_a_hand_authored_StatlineFlagRule_coexist_without_regressing_the_resolved_rule()
    {
        // Real corpus overlap (Adeptus Custodes' "Vexilla"): the same wargear entry carries both a
        // hand-authored StatlineFlagRule match (fully resolving Oc) and a classified structural Oc
        // candidate. The candidate application step must not overwrite the already-resolved value.
        var vexilla = new Ability
        {
            Name = "Vexilla",
            Text = "Add 1 to the Objective Control characteristic of models in the bearer's unit.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.OptionalGrant
        };
        var datasheet = DatasheetWithCandidate("Vexilla", "Oc");
        var unit = new Unit(datasheet, [], [new ModelLine("Custodian Guard", [], count: 1, abilities: [vexilla])]);

        var view = AttachedUnitAggregator.Build(unit);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.Oc.IsCaveated.Should().BeFalse();
        entry.Statline.Oc.Value.Should().Be((CharacteristicValue)3);
        entry.Statline.Oc.ContributingAbilities.Should().ContainSingle(a => a.Name == "Vexilla");
    }

    [Fact]
    public void Two_present_candidates_targeting_the_same_characteristic_the_first_applied_wins_the_second_is_dropped()
    {
        // No real corpus example of this shape was found reachable from one bearer (a full-corpus
        // probe found every near-miss - two structurally-classified modifiers on the same field -
        // resolves to an unresolvable wargear-bundle wrapper entry on one side, per §1.3's own
        // fail-closed finding). This documents the CURRENT, real behavior for the hypothetical case
        // anyway: ScalarCharacteristicView.Caveated only ever carries ONE contributing ability (a
        // pre-existing domain-model constraint, not something this change could relax without
        // widening CharacteristicView's own shape) - so once the first candidate touches a field,
        // the "skip if already touched" guard (needed for the real Vexilla-overlap case above) means
        // a second candidate on the same field is silently dropped, not merged. Whichever candidate
        // Datasheet.CharacteristicModifierCandidates enumerates first wins; there is no guaranteed
        // ordering across two independently-classified candidates today.
        var enhancementA = new Ability
        {
            Name = "First Relic", Text = "Grants a bonus.", Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Enhancement
        };
        var enhancementB = new Ability
        {
            Name = "Second Relic", Text = "Grants a different bonus.", Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Enhancement
        };
        var datasheet = new Datasheet(
            "Custodian Guard", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Custodian Guard", new Statline(6, 6, 2, 4, 7, 2))], weaponProfiles: [],
            characteristicModifierCandidates:
            [
                new CharacteristicModifierCandidate("First Relic", "W", "1"),
                new CharacteristicModifierCandidate("Second Relic", "W", "1")
            ]);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Custodian Guard", [], count: 1, abilities: [enhancementA, enhancementB])]);

        var view = AttachedUnitAggregator.Build(unit);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.W.IsCaveated.Should().BeTrue();
        entry.Statline.W.ContributingAbilities.Should().ContainSingle();
        entry.Statline.W.ContributingAbilities[0].Name.Should().Be("First Relic");
        // Both source Enhancements still render normally in the ability list either way - only the
        // statline tile's own caveat attribution is limited to one source, never the ability listing.
        view.Abilities.Select(a => a.Ability.Name).Should().Contain(["First Relic", "Second Relic"]);
    }
}