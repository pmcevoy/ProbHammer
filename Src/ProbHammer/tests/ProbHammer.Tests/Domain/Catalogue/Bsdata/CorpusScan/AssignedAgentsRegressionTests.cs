using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>Not a full-corpus scan - a small, targeted, [Fact(Explicit = true)] regression check
/// (needs the live clone) for the concrete bug classify-known-army-rules fixes: "Assigned Agents"
/// carries the same BSData "primary-catalogue"-scoped gating shape as a genuine army rule (see
/// bsdata-corpus-scan's Full-Corpus Primary-Catalogue-Gated Rule Triage Scan), and the old
/// structural-only Origin signal misclassified it as ArmyRule whenever a roster included an
/// allied Agents of the Imperium unit - it would then show up in the army header alongside the
/// roster's own genuine army rule. Resolves a real Vindicare Assassin (an Agents of the Imperium
/// unit, referencing "Assigned Agents" on its own datasheet) via a Custodes-starting closure, the
/// same one-closure-for-the-whole-roster shape ArmyRosterEnricher uses in production.</summary>
public class AssignedAgentsRegressionTests
{
    [Fact(Explicit = true)]
    public void AnAlliedAgentsOfTheImperiumUnit_NeverClassifiesAssignedAgentsAsArmyRule()
    {
        var source = LiveClone.RequireSource();
        var catalogue = ResolvedBsdataCatalogue.Build(source, "Imperium - Adeptus Custodes.json");
        var knownArmyRuleNames = ArmyRuleNameLookup.Resolve(["Adeptus Custodes"]);

        var vindicare = catalogue.ResolveDatasheet("Vindicare Assassin", knownArmyRuleNames);

        var assignedAgents = vindicare.Abilities.Should().ContainSingle(a => a.Name == "Assigned Agents").Subject;
        assignedAgents.Origin.Should().Be(AbilityOrigin.CoreRule);
    }
}
