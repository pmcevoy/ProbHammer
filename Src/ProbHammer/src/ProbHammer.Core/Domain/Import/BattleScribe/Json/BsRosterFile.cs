using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Import.BattleScribe.Json;

/// <summary>
/// Root wrapper for a BattleScribe/NewRecruit <c>rosterSchema</c> JSON export
/// ({ "roster": { ... } }) - see battlescribe-roster-import's Format Recognition requirement.
/// Only the fields this pipeline's mapping needs are modeled, mirroring
/// <c>Domain.Catalogue.Bsdata.Json.BsCatalogueFile</c>'s existing precedent of modeling only what a
/// loader actually reads - constraints/modifiers/conditionGroups are deliberately left unmapped;
/// with System.Text.Json's default behavior, unmapped JSON properties are silently ignored.
/// </summary>
public sealed class BsRosterFile
{
    public BsRoster? Roster { get; set; }
}

public sealed class BsRoster
{
    public string Xmlns { get; set; } = "";
    public string Name { get; set; } = "";
    public List<BsRosterCost> Costs { get; set; } = [];
    public List<BsRosterCost> CostLimits { get; set; } = [];
    public List<BsRosterForce> Forces { get; set; } = [];
}

public sealed class BsRosterForce
{
    public string CatalogueName { get; set; } = "";

    /// <summary>Force-wide rule text (distinct from any one selection's own <see cref="BsRosterSelection.Rules"/>)
    /// - included in the roster-scoped RuleGlossary alongside every selection's own rules.</summary>
    public List<BsRosterRule> Rules { get; set; } = [];

    public List<BsRosterSelection> Selections { get; set; } = [];
}

/// <summary>A named cost entry (e.g. <c>{"name":"pts","value":915}</c>,
/// <c>{"name":"Detachment Points","value":2}</c>, <c>{"name":"Enhancements","value":1}</c>) - the
/// same shape used for a roster's own totals (<see cref="BsRoster.Costs"/>/
/// <see cref="BsRoster.CostLimits"/>) and for a single selection's own cost tags.</summary>
public sealed class BsRosterCost
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
}

/// <summary>One node in a roster's selection tree - shared shape for a top-level army-configuration
/// selection (Battle Size, Detachment, Force Disposition), a top-level unit/model selection, and
/// every nested wargear/model-loadout selection beneath one - BattleScribe reuses the same
/// selection shape at every nesting level, mirroring <c>BsSelectionEntry</c>'s own precedent.</summary>
public sealed class BsRosterSelection
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>"model" | "unit" | "upgrade" - only "model"/"unit" selections at the top level are
    /// real army entries; "upgrade" covers both army-configuration selections (Battle Size,
    /// Detachment, Force Disposition) and every nested wargear/loadout choice.</summary>
    public string Type { get; set; } = "";

    public int Number { get; set; }
    public List<BsRosterProfile> Profiles { get; set; } = [];
    public List<BsRosterRule> Rules { get; set; } = [];
    public List<BsRosterCost> Costs { get; set; } = [];
    public List<BsRosterSelection> Selections { get; set; } = [];

    /// <summary>Outgoing <c>"Leading"</c>/<c>"Supporting"</c> attachment links only - see
    /// battlescribe-roster-import's Attachment Relationship Resolution requirement. A target
    /// selection's own <c>incomingAssociations</c> field is redundant with this and deliberately
    /// not modeled.</summary>
    public List<BsRosterAssociation> Associations { get; set; } = [];

    /// <summary>This selection's own keyword/category tags (e.g. "Infantry", "Faction: Heretic
    /// Astartes") - already fully-resolved display names, mirroring
    /// <c>Domain.Catalogue.Bsdata.Json.BsCategoryLink</c>'s identical role for the BSData
    /// pipeline. See resolve-category-keywords.</summary>
    public List<BsRosterCategory> Categories { get; set; } = [];
}

/// <summary>One `categories` entry - see <see cref="BsRosterSelection.Categories"/>.</summary>
public sealed class BsRosterCategory
{
    public string Name { get; set; } = "";
}

public sealed class BsRosterAssociation
{
    public string Type { get; set; } = "";
    public string To { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class BsRosterProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string TypeName { get; set; } = "";
    public List<BsRosterCharacteristic> Characteristics { get; set; } = [];

    public string? CharacteristicText(string name) =>
        Characteristics.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))?.Text;
}

public sealed class BsRosterCharacteristic
{
    public string Name { get; set; } = "";

    [JsonPropertyName("$text")]
    public string Text { get; set; } = "";
}

/// <summary>One already-resolved rule-text entry (e.g. "Templar Vows", "Scouts 6\"", a weapon's own
/// "Devastating Wounds"/"Anti" keyword rule) - carried through as opaque display text, matching
/// <c>BsRule</c>'s own shape. No <c>modifiers</c>/gating field is modeled here: unlike the BSData
/// pipeline, a roster JSON contains only rules that already apply to the exported army (see
/// battlescribe-roster-import's Core Rule Extraction - "SHALL NOT apply any separate ... gating").</summary>
public sealed class BsRosterRule
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
