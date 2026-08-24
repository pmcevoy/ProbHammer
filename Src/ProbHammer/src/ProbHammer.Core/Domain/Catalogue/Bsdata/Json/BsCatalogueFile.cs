using System.Text.Json;
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

    /// <summary>Force-type definitions (e.g. "Army Roster", "Crusade Force") - populated only on
    /// the game-system file ("Warhammer 40,000.json"). Used solely to resolve the id a
    /// game-mode-gating modifier's condition references by name (see
    /// BsdataDatasheetMapper.IsGameModeGated) - never for roster/force-composition validation
    /// itself, which stays out of scope.</summary>
    public List<BsForceEntry> ForceEntries { get; set; } = [];

    /// <summary>Faction/library-level rule text (e.g. Black Templars' "Templar Vows", reached
    /// through `Library - Astartes Heresy Legends.json`) - absent entirely on a plain faction file
    /// with no rules of its own, which deserializes to the default empty list under this loader's
    /// existing convention for optional BSData fields. See rules-glossary's "Faction and Library
    /// Rule Text Extraction".</summary>
    public List<BsRule> Rules { get; set; } = [];

    /// <summary>Universal special-rule text (e.g. "Lethal Hits", "Devastating Wounds") - populated
    /// only on the game-system file ("Warhammer 40,000.json"), reached via
    /// <see cref="BsdataClosure.GameSystem"/>. See rules-glossary's "Universal Rule Text
    /// Extraction".</summary>
    public List<BsRule> SharedRules { get; set; } = [];
}

/// <summary>Shape shared by both `sharedRules` (game-system level) and `rules` (faction/library
/// level) entries - `{name, description, alias[], id, modifiers}` plus fields this loader doesn't
/// need (`publicationId`, `page`, `hidden` as a bare bool - only the structured `modifiers`-driven
/// hidden condition is read), silently dropped on parse like every other unmapped BSData
/// field.</summary>
public sealed class BsRule
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>A rule's own gating (e.g. chapter/sub-faction exclusivity, or game-mode
    /// exclusivity) - see BsdataDatasheetMapper's generalized IsGameModeGated/
    /// IsProvablyAlwaysTrueInMatchedPlay, which reads this to decide whether a Core Rule Ability
    /// reference should be excluded for the current closure. Previously silently dropped like
    /// every other unmapped BSData field on this type.</summary>
    public List<BsModifier> Modifiers { get; set; } = [];
    public string Description { get; set; } = "";
    public List<string> Alias { get; set; } = [];
}

public sealed class BsForceEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
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

    /// <summary>See BsdataDatasheetMapper.IsGameModeGated - only the "set hidden=true, gated by a
    /// force-type condition" shape is ever interpreted; every other modifier use (points-cost
    /// scaling, composition rules, etc.) is deliberately left unmapped/unread, unchanged from this
    /// project's existing "no wargear constraint validation" stance.</summary>
    public List<BsModifier> Modifiers { get; set; } = [];
}

public sealed class BsSelectionEntryGroup
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<BsSelectionEntry> SelectionEntries { get; set; } = [];
    public List<BsSelectionEntryGroup> SelectionEntryGroups { get; set; } = [];
    public List<BsEntryLink> EntryLinks { get; set; } = [];
    public List<BsEntryLink> InfoLinks { get; set; } = [];
    public List<BsModifier> Modifiers { get; set; } = [];
}

/// <summary>A BSData "modifiers" entry - the rules-engine primitive that sets some field to some
/// value when its condition tree evaluates true. Only ever interpreted narrowly (see
/// BsdataDatasheetMapper.IsGameModeGated): "does a hidden=true modifier's condition tree reference
/// a specific force-type id anywhere" - a plain existence check, not general boolean evaluation
/// (AND/OR nesting, comparison operators, etc. are all read structurally but never actually
/// evaluated as logic).</summary>
public sealed class BsModifier
{
    public string Type { get; set; } = "";
    public string Field { get; set; } = "";
    public JsonElement Value { get; set; }
    public List<BsCondition> Conditions { get; set; } = [];
    public List<BsConditionGroup> ConditionGroups { get; set; } = [];
}

public sealed class BsCondition
{
    public string Type { get; set; } = "";
    public string ChildId { get; set; } = "";
    public string Scope { get; set; } = "";
}

public sealed class BsConditionGroup
{
    public string Type { get; set; } = "";
    public List<BsCondition> Conditions { get; set; } = [];
    public List<BsConditionGroup> ConditionGroups { get; set; } = [];
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

    /// <summary>A "type":"rule" infoLink's own display-name-building modifiers (e.g. an
    /// "append"/field:"name" modifier that turns the linked rule's bare name "Deadly Demise" into
    /// the displayed "Deadly Demise D3") - see BsdataDatasheetMapper's Core Rule ability
    /// extraction. Empty for every other link shape.</summary>
    public List<BsModifier> Modifiers { get; set; } = [];
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
