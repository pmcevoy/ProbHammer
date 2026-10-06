using System.Collections.Concurrent;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// App-wide cache of <see cref="ResolvedBsdataCatalogue"/>, keyed by starting file name - the
/// expensive part of enrichment (multi-file JSON parse, BFS import resolution, index building) is
/// static per faction and identical for every user, so it's built once per starting file and reused
/// for the lifetime of whatever holds this instance, never rebuilt per request or per session.
/// No invalidation - a caller
/// (ProbHammer.Web's composition root) registers one instance as a singleton; this type itself has
/// no opinion on lifetime beyond "build at most once per key".
/// </summary>
public sealed class BsdataCatalogueCache(IBsdataCatalogueSource source)
{
    private readonly ConcurrentDictionary<string, ResolvedBsdataCatalogue> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, RuleGlossary?> _gameSystemGlossaries =
        new(StringComparer.OrdinalIgnoreCase);

    public ResolvedBsdataCatalogue GetOrBuild(string startingFileName) =>
        _cache.GetOrAdd(startingFileName, fileName => ResolvedBsdataCatalogue.Build(source, fileName));

    /// <summary>The game system's shared rules alone, or null when no file has this id.</summary>
    public RuleGlossary? GetGameSystemGlossary(string gameSystemId) =>
        _gameSystemGlossaries.GetOrAdd(gameSystemId, id =>
            BsdataClosureResolver.ResolveGameSystem(source, id) is { } gameSystem
                ? RuleGlossary.Build(new BsdataClosure([], gameSystem))
                : null);
}
