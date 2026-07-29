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
    public IReadOnlyDictionary<string, Statline> Statlines { get; }

    private readonly IReadOnlyDictionary<string, WeaponProfile> _weaponProfiles;

    public Datasheet(
        string name,
        IEnumerable<string> factionKeywords,
        IEnumerable<string> keywords,
        IEnumerable<Ability> abilities,
        IReadOnlyDictionary<string, Statline> statlines,
        IEnumerable<WeaponProfile> weaponProfiles)
    {
        Name = name;
        FactionKeywords = new HashSet<string>(factionKeywords, StringComparer.OrdinalIgnoreCase);
        Keywords = new HashSet<string>(keywords, StringComparer.OrdinalIgnoreCase);
        Abilities = abilities.ToList();
        Statlines = new Dictionary<string, Statline>(statlines, StringComparer.OrdinalIgnoreCase);
        _weaponProfiles = weaponProfiles.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public Statline GetStatline(string name) =>
        Statlines.TryGetValue(name, out var statline)
            ? statline
            : throw new KeyNotFoundException($"Datasheet '{Name}' has no statline named '{name}'.");

    /// <summary>Resolves a single named weapon profile without enumerating every profile the datasheet defines.</summary>
    public WeaponProfile ResolveWeaponProfile(string name) =>
        _weaponProfiles.TryGetValue(name, out var profile)
            ? profile
            : throw new KeyNotFoundException($"Datasheet '{Name}' has no weapon profile named '{name}'.");
}
