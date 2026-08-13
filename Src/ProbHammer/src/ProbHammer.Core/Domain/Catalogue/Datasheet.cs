namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>
/// Reference/rules data for a single named unit type, independent of any specific army list.
/// Statlines are named and enumerable (the aggregate view needs to list distinct statlines in
/// play). Weapon profiles resolve by name on demand only - no full options menu is materialized.
/// </summary>
public sealed class Datasheet
{
    public string Name { get; }
    public IReadOnlySet<string> FactionKeywords { get; }
    public IReadOnlySet<string> Keywords { get; }
    public IReadOnlyList<Ability> Abilities { get; }
    public IReadOnlyList<(string Name, Statline Statline)> Statlines { get; }

    private readonly IReadOnlyDictionary<string, Statline> _statlinesByName;
    private readonly IReadOnlyDictionary<string, WeaponProfile> _weaponProfiles;

    public Datasheet(
        string name,
        IEnumerable<string> factionKeywords,
        IEnumerable<string> keywords,
        IEnumerable<Ability> abilities,
        IReadOnlyList<(string Name, Statline Statline)> statlines,
        IEnumerable<WeaponProfile> weaponProfiles)
    {
        Name = name;
        FactionKeywords = new HashSet<string>(factionKeywords, StringComparer.OrdinalIgnoreCase);
        Keywords = new HashSet<string>(keywords, StringComparer.OrdinalIgnoreCase);
        Abilities = abilities.ToList();
        Statlines = statlines;
        _statlinesByName = statlines.ToDictionary(x => x.Name, x => x.Statline, StringComparer.OrdinalIgnoreCase);
        _weaponProfiles = weaponProfiles.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public Statline GetStatline(string name) =>
        _statlinesByName.TryGetValue(name, out var statline)
            ? statline
            : throw new KeyNotFoundException($"Datasheet '{Name}' has no statline named '{name}'.");

    /// <summary>Resolves a single named weapon profile without enumerating every profile the datasheet defines.</summary>
    public WeaponProfile ResolveWeaponProfile(string name) =>
        _weaponProfiles.TryGetValue(name, out var profile)
            ? profile
            : throw new KeyNotFoundException($"Datasheet '{Name}' has no weapon profile named '{name}'.");
}
