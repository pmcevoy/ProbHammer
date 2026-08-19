using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

/// <summary>
/// Root wrapper for a BSData 11e JSON file, either a catalogueSchema document
/// ({ "catalogue": { ... } }) or the single game-system document
/// ({ "gameSystem": { ... } }, e.g. "Warhammer 40,000.json") - both share the same nested shape
/// for every field this loader reads (id, name, sharedSelectionEntries,
/// sharedSelectionEntryGroups, sharedProfiles), so one <see cref="BsCatalogue"/> type models
/// both; exactly one of <see cref="Catalogue"/>/<see cref="GameSystem"/> is populated per file.
/// Only the fields this loader's resolution/mapping needs are modeled - constraints, modifiers,
/// conditionGroups, associations, and costs are deliberately left unmapped (see design.md); with
/// System.Text.Json's default behavior, unmapped JSON properties are silently ignored rather than
/// causing a deserialization failure.
/// </summary>
public sealed class BsCatalogueFile
{
    public BsCatalogue? Catalogue { get; set; }
    public BsCatalogue? GameSystem { get; set; }
}

public sealed class BsCatalogue
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>Every catalogue file (never a game-system file itself) references its single game
    /// system this way, not via <see cref="CatalogueLinks"/> - resolved by
    /// BsdataClosureResolver into every closure regardless, since universal shared rules/profiles
    /// (e.g. the base "Invulnerable Save (X+*)" abilities) live only there. Empty on a
    /// game-system file's own deserialized <see cref="BsCatalogue"/>.</summary>
    public string GameSystemId { get; set; } = "";

    public List<BsCatalogueLink> CatalogueLinks { get; set; } = [];
    public List<BsSelectionEntry> SharedSelectionEntries { get; set; } = [];
    public List<BsSelectionEntryGroup> SharedSelectionEntryGroups { get; set; } = [];

    /// <summary>Standalone profile definitions, addressable by id and referenced via an
    /// infoLink of type "profile" rather than being nested directly inside a selectionEntry's own
    /// "profiles" array (observed in real data: Chaos - Chaos Space Marines.json's "Legionaries"
    /// squad reaches its troop-model statline this way, marked "noindex" on the shared profile
    /// itself since it isn't meant to double-display anywhere else).</summary>
    public List<BsProfile> SharedProfiles { get; set; } = [];
}

public sealed class BsCatalogueLink
{
    public string Name { get; set; } = "";
    public string TargetId { get; set; } = "";
    public bool ImportRootEntries { get; set; }
}

/// <summary>
/// Shape shared by both top-level "sharedSelectionEntries" entries and every nested
/// "selectionEntries" child - BSData reuses the same selectionEntry shape at every nesting level.
/// </summary>
public sealed class BsSelectionEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public List<BsProfile> Profiles { get; set; } = [];
    public List<BsSelectionEntry> SelectionEntries { get; set; } = [];
    public List<BsSelectionEntryGroup> SelectionEntryGroups { get; set; } = [];
    public List<BsEntryLink> EntryLinks { get; set; } = [];

    /// <summary>References to standalone profiles (see BsCatalogue.SharedProfiles) of type
    /// "profile", resolved by id rather than nested directly in Profiles.</summary>
    public List<BsEntryLink> InfoLinks { get; set; } = [];
}

public sealed class BsSelectionEntryGroup
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<BsSelectionEntry> SelectionEntries { get; set; } = [];
    public List<BsSelectionEntryGroup> SelectionEntryGroups { get; set; } = [];
    public List<BsEntryLink> EntryLinks { get; set; } = [];
    public List<BsEntryLink> InfoLinks { get; set; } = [];
}

/// <summary>
/// A reference to another selectionEntry/selectionEntryGroup by id. The "import" flag on this
/// shape is BattleScribe's shared-entry-reuse marker, not an indicator of which catalogue file
/// the target lives in - do not use it to decide local-vs-imported resolution (see design.md's
/// "entryLink target location" risk). TargetId is resolved against an id index built over the
/// full closure, checking nearer files before farther ones, mirroring name resolution.
/// </summary>
public sealed class BsEntryLink
{
    public string Name { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string Type { get; set; } = "";
}

public sealed class BsProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string TypeName { get; set; } = "";
    public List<BsCharacteristic> Characteristics { get; set; } = [];

    public string? CharacteristicText(string name) =>
        Characteristics.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))?.Text;
}

public sealed class BsCharacteristic
{
    public string Name { get; set; } = "";

    [JsonPropertyName("$text")]
    public string Text { get; set; } = "";
}
