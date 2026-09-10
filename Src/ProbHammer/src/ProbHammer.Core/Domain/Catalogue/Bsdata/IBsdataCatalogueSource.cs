namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// The "get JSON content for this filename" boundary. Deliberately narrow: closure
/// resolution and mapping depend only on this shape, never on how or where a file's bytes come
/// from - a later in-memory cache or HTTP-fetching implementation can be dropped in behind this
/// interface without touching either.
/// </summary>
public interface IBsdataCatalogueSource
{
    /// <summary>Returns the raw JSON content of the named catalogue file (e.g.
    /// "Imperium - Black Templars.json").</summary>
    string GetJson(string fileName);

    /// <summary>Every catalogue file name this source can serve. Used only as a fallback when a
    /// catalogueLinks entry's cached <c>name</c> has drifted from its target file's actual current
    /// name (observed in real data: a link named "Chaos - Daemons Library" whose target file is
    /// actually "Chaos - Chaos Daemons Library.json") - see BsdataClosureResolver.</summary>
    IReadOnlyList<string> ListFileNames();
}
