namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Resolves a parsed army list's Faction entries to the starting catalogue file for
/// <see cref="BsdataClosureResolver.Resolve"/> - see design.md's "Faction → starting BSData file:
/// suffix match, not a literal join". Matches only the most specific (last) Faction entry against
/// the available file names: a file whose name equals that entry, or ends with " - " followed by
/// that entry, excluding any file whose name contains "Library" (shared cross-sub-faction content,
/// never itself a playable faction identity - confirmed can hold the majority of a faction's real
/// content, so exclusion is about identity, not size). Fails loud (naming the Faction entry) when
/// zero or more than one file matches, rather than guessing.
/// </summary>
public static class BsdataFactionResolver
{
    public static string ResolveStartingFileName(IReadOnlyList<string> faction, IReadOnlyList<string> availableFileNames)
    {
        var mostSpecific = faction[^1];

        var candidates = availableFileNames
            .Where(f => !f.Contains("Library", StringComparison.OrdinalIgnoreCase))
            .Where(f =>
                string.Equals(f, $"{mostSpecific}.json", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith($" - {mostSpecific}.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return candidates.Count switch
        {
            0 => throw new BsdataFactionResolutionException(mostSpecific, $"No catalogue file matches faction '{mostSpecific}'."),
            > 1 => throw new BsdataFactionResolutionException(mostSpecific,
                $"Multiple catalogue files match faction '{mostSpecific}': {string.Join(", ", candidates)}."),
            _ => candidates[0]
        };
    }
}

/// <summary>Thrown by <see cref="BsdataFactionResolver.ResolveStartingFileName"/> when a Faction
/// entry matches zero or more than one catalogue file. Carries the offending Faction entry as a
/// property, mirroring <see cref="AmbiguousCharacteristicException"/>'s existing convention.</summary>
public sealed class BsdataFactionResolutionException(string faction, string message) : Exception(message)
{
    public string Faction { get; } = faction;
}
