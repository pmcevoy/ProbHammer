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
    ArmyRoster Build(ParsedArmyList parsedArmyList);
}

public sealed class ArmyRosterProvider(BsdataCatalogueCache cache, IBsdataCatalogueSource source) : IArmyRosterProvider
{
    public ArmyRoster Build(ParsedArmyList parsedArmyList)
    {
        var startingFile = BsdataFactionResolver.ResolveStartingFileName(parsedArmyList.Faction, source.ListFileNames());
        var catalogue = cache.GetOrBuild(startingFile);
        return ArmyRosterEnricher.Enrich(parsedArmyList, catalogue);
    }
}
