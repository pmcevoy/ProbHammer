using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Covers statline-flag-rules' three requirements against the two seed rules (Shield Dome,
/// Vexilla) via <see cref="AttachedUnitAggregator.Build"/> directly - the rule pass runs inside
/// Build (design D3), so its effect is only observable through the aggregate view it produces.</summary>
public class StatlineFlagRuleTests
{
    private static readonly Ability ShieldDome = new()
    {
        Name = "Shield Dome",
        Text = "The bearer has a 5+ invulnerable save.",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.OptionalGrant
    };

    private static readonly Ability Vexilla = new()
    {
        Name = "Vexilla",
        Text = "Add 1 to the Objective Control characteristic of models in the bearer's unit.",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.OptionalGrant
    };

    private static Unit ImpulsorWithShieldDome()
    {
        var datasheet = new Datasheet(
            "Impulsor", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Impulsor", new Statline(12, 9, 3, 11, 6, 2))], weaponProfiles: []);
        return new Unit(datasheet, [], [new ModelLine("Impulsor", [], count: 1, abilities: [ShieldDome])]);
    }

    [Fact]
    public void ShieldDome_FlagsTheBearersOwnInvulnerableSave()
    {
        var unit = ImpulsorWithShieldDome();

        var view = AttachedUnitAggregator.Build(unit);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.Caveated.Should().BeFalse();
        entry.Statline.InSv.MeleeInSv.Should().Be(5);
        entry.Statline.InSv.RangedInSv.Should().Be(5);
    }

    [Fact]
    public void ShieldDome_SourceAbilityStillRendersInTheNormalAbilityListing()
    {
        var unit = ImpulsorWithShieldDome();

        var view = AttachedUnitAggregator.Build(unit);

        view.Abilities.Should().ContainSingle(e => e.Ability.Name == "Shield Dome");
    }

    [Fact]
    public void ShieldDome_FlagDisappearsOnceItsBearerIsRemoved()
    {
        var unit = ImpulsorWithShieldDome();
        unit.ModelLines[0].RemoveCasualties(1);

        var view = AttachedUnitAggregator.Build(unit);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.Caveated.Should().BeFalse();
        entry.Statline.InSv.MeleeInSv.Should().Be(0);
        entry.Statline.InSv.RangedInSv.Should().Be(0);
    }

    [Fact]
    public void UnmatchedAbility_NameMatchesButTextDoesNot_ProducesNoFlag()
    {
        var mismatched = ShieldDome with { Text = "The bearer has a 4+ invulnerable save." };
        var datasheet = new Datasheet(
            "Impulsor", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Impulsor", new Statline(12, 9, 3, 11, 6, 2))], weaponProfiles: []);
        var unit = new Unit(datasheet, [], [new ModelLine("Impulsor", [], count: 1, abilities: [mismatched])]);

        var view = AttachedUnitAggregator.Build(unit);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.Should().Be(new InvulnerableSave());
        entry.Flags.Should().BeEmpty();
    }

    private static AttachedUnit CustodianGuardWithVexilla()
    {
        var bodyguardDatasheet = new Datasheet(
            "Custodian Guard", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Custodian Guard", new Statline(6, 6, 2, 4, 7, 2))], weaponProfiles: []);
        var bodyguard = new Unit(bodyguardDatasheet, [],
            [new ModelLine("Custodian Guard", [], count: 4, abilities: [Vexilla])]);

        var wardenDatasheet = new Datasheet(
            "Custodian Warden", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Custodian Warden", new Statline(6, 6, 2, 5, 7, 2))], weaponProfiles: []);
        var warden = new Unit(wardenDatasheet, [], [new ModelLine("Custodian Warden", [], count: 1)]);

        return new AttachedUnit(bodyguard, [warden]);
    }

    [Fact]
    public void Vexilla_FlagsObjectiveControlOnEveryRowOfTheWholeUnit_NotOnlyTheBearersOwnRow()
    {
        var attachedUnit = CustodianGuardWithVexilla();

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.Statlines.Should().HaveCount(2);
        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Guard" && s.Statline.Oc == 3);
        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Warden" && s.Statline.Oc == 3);
    }

    [Fact]
    public void Vexilla_UnrelatedCasualtyElsewhereInTheUnit_LeavesTheFlagUntouched()
    {
        var attachedUnit = CustodianGuardWithVexilla();
        attachedUnit.Attached[0].ModelLines[0].RemoveCasualties(1); // the Warden, not the Vexilla bearer

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Guard" && s.Statline.Oc == 3);
    }

    [Fact]
    public void Vexilla_FlagDisappearsOnceItsBearerModelLineIsFullyRemoved()
    {
        var attachedUnit = CustodianGuardWithVexilla();
        attachedUnit.Bodyguard.ModelLines[0].RemoveCasualties(4);

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.Statlines.Should().OnlyContain(s => s.Statline.Oc == 2);
    }
}
