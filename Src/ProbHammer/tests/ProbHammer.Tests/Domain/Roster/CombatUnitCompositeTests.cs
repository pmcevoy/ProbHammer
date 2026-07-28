using FluentAssertions;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

public class CombatUnitCompositeTests
{
    [Fact]
    public void PlainUnit_YieldsItselfAsItsOnlyComponent()
    {
        ICombatUnit unit = UnitFixtures.AssaultIntercessorSquadUniform();

        unit.Components.Should().ContainSingle().Which.Should().BeSameAs(unit);
    }

    [Fact]
    public void AttachedUnit_YieldsBodyguardAndAllAttachedAsComponents()
    {
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();
        ICombatUnit combatUnit = attachedUnit;

        combatUnit.Components.Should().HaveCount(3);
        combatUnit.Components.Should().Contain(attachedUnit.Bodyguard);
        combatUnit.Components.Should().Contain(attachedUnit.Attached);
    }
}
