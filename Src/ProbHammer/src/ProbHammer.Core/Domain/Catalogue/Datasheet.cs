namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>
/// Reference/rules data for a single named unit type, independent of any specific army list.
/// Statlines are named and enumerable (the aggregate view needs to list distinct statlines in
/// play). Weapon profiles resolve by name on demand only - no full options menu is materialized.
/// Optional abilities (Enhancements, and other ability grants nested inside their own selection
/// entry, e.g. Impulsor's "Shield Dome") get the same on-demand-only treatment - the public
/// Abilities list carries only intrinsic, unconditional facts about the datasheet.
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
    private readonly IReadOnlyDictionary<string, Ability> _optionalAbilities;
    private readonly IReadOnlyDictionary<string, IReadOnlySet<string>> _modelKeywordsByName;

    /// <summary><paramref name="weaponProfiles"/>/<paramref name="optionalAbilities"/> are plain
    /// enumerables, not pre-built dictionaries - the internal lookups are built from each item's
    /// own <c>Name</c> here, so a dictionary key can never disagree with its own Name (a real bug
    /// once let fixture call sites drift the two apart). <paramref name="statlines"/> is an
    /// ordered list, not a caller-supplied dictionary - Dictionary enumeration order was never a
    /// documented contract, so declared order (the Sergeant/leader entry first, matching the real
    /// NewRecruit/GW app export convention) is an explicit, tested guarantee instead of an
    /// accident of insertion order.</summary>
    public Datasheet(
        string name,
        IEnumerable<string> factionKeywords,
        IEnumerable<string> keywords,
        IEnumerable<Ability> abilities,
        IReadOnlyList<(string Name, Statline Statline)> statlines,
        IEnumerable<WeaponProfile> weaponProfiles,
        IEnumerable<Ability>? optionalAbilities = null,
        IEnumerable<(string Name, IReadOnlySet<string> Keywords)>? modelKeywords = null)
    {
        Name = name;
        FactionKeywords = new HashSet<string>(factionKeywords, StringComparer.OrdinalIgnoreCase);
        Keywords = new HashSet<string>(keywords, StringComparer.OrdinalIgnoreCase);
        Abilities = abilities.ToList();
        Statlines = statlines;
        _statlinesByName = statlines.ToDictionary(x => x.Name, x => x.Statline, StringComparer.OrdinalIgnoreCase);
        _weaponProfiles = weaponProfiles.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _optionalAbilities = (optionalAbilities ?? []).ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _modelKeywordsByName =
            (modelKeywords ?? []).ToDictionary(x => x.Name, x => x.Keywords, StringComparer.OrdinalIgnoreCase);
    }

    public Statline GetStatline(string name) =>
        _statlinesByName.TryGetValue(name, out var statline)
            ? statline
            : throw new KeyNotFoundException($"Datasheet '{Name}' has no statline named '{name}'.");

    public bool TryGetStatline(string name, out Statline statline) => _statlinesByName.TryGetValue(name, out statline!);

    /// <summary>Keywords scoped to one named statline (e.g. a squad's single named individual),
    /// distinct from the Datasheet's own top-level Keywords - empty for a name with no such data,
    /// never throwing.</summary>
    public IReadOnlySet<string> GetModelKeywords(string name) =>
        _modelKeywordsByName.TryGetValue(name, out var keywords) ? keywords : new HashSet<string>();

    /// <summary>Resolves a single named weapon profile without enumerating every profile the datasheet defines.</summary>
    public WeaponProfile ResolveWeaponProfile(string name) =>
        _weaponProfiles.TryGetValue(name, out var profile)
            ? profile
            : throw new KeyNotFoundException($"Datasheet '{Name}' has no weapon profile named '{name}'.");

    public bool TryResolveWeaponProfile(string name, out WeaponProfile profile) =>
        _weaponProfiles.TryGetValue(name, out profile!);

    /// <summary>Every weapon profile name this Datasheet defines - exposed for diagnostic purposes
    /// (e.g. army-roster-enrichment's "did you mean" suggestion on a resolution failure), not for
    /// enumerating a full options menu (see ResolveWeaponProfile's own on-demand-only design).</summary>
    public IReadOnlyList<string> WeaponNames => _weaponProfiles.Keys.ToList();

    /// <summary>Resolves a single named optional ability (an Enhancement, or any other ability
    /// grant nested inside its own selection entry, e.g. Impulsor's "Shield Dome") without
    /// enumerating every optional ability the Datasheet defines - mirrors
    /// TryResolveWeaponProfile's on-demand-only design exactly. An optional ability never appears
    /// in the public Abilities list; this is the only way to reach one.</summary>
    public bool TryResolveAbility(string name, out Ability ability) =>
        _optionalAbilities.TryGetValue(name, out ability!);

    /// <summary>Every optional ability name this Datasheet defines - exposed for diagnostic
    /// purposes (a "did you mean" suggestion on a resolution failure), not for enumerating a full
    /// options menu (see TryResolveAbility's own on-demand-only design).</summary>
    public IReadOnlyList<string> OptionalAbilityNames => _optionalAbilities.Keys.ToList();
}