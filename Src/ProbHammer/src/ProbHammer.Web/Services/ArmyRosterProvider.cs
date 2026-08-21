using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Web.Services;

/// <summary>Builds a live <see cref="ArmyRoster"/> from a <see cref="ParsedArmyList"/>: resolves the
/// faction's starting catalogue file, fetches (or builds and caches) its
/// <see cref="ResolvedBsdataCatalogue"/> via the app-wide <see cref="BsdataCatalogueCache"/>, then
/// runs <see cref="ArmyRosterEnricher"/>. Shared by the import page (to validate before committing
/// to session) and `/LivePlay`'s own rebuild-fresh-every-request path (see design.md's "Session
/// stores the intermediate, not the graph") so both call sites share one orchestration instead of
/// duplicating the three-step sequence.</summary>
public interface IArmyRosterProvider
{
    ArmyRosterBuildResult Build(ParsedArmyList parsedArmyList);
}

/// <summary>Bundles the built <see cref="ArmyRoster"/> with the same faction closure's
/// <see cref="RuleGlossary"/> - both come out of the one <see cref="ResolvedBsdataCatalogue"/>
/// <see cref="ArmyRosterProvider.Build"/> already resolves, so callers that need to look up
/// ability/weapon-keyword rule text (rules-glossary) get it for free rather than re-resolving the
/// catalogue a second time.</summary>
public sealed record ArmyRosterBuildResult(ArmyRoster Roster, RuleGlossary Glossary);

public sealed class ArmyRosterProvider(BsdataCatalogueCache cache, IBsdataCatalogueSource source) : IArmyRosterProvider
{
    public ArmyRosterBuildResult Build(ParsedArmyList parsedArmyList)
    {
        var startingFile = BsdataFactionResolver.ResolveStartingFileName(parsedArmyList.Faction, source.ListFileNames());
        var catalogue = cache.GetOrBuild(startingFile);
        var roster = ArmyRosterEnricher.Enrich(parsedArmyList, catalogue);
        return new ArmyRosterBuildResult(roster, catalogue.Glossary);
    }
}
