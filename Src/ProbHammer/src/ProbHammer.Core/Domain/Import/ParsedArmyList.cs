namespace ProbHammer.Core.Domain.Import;

/// <summary>One already-split per-loadout model sub-group within a parsed unit - the shared-vs-
/// mutually-exclusive weapon-count partition (see ArmyListParser's "Model Group and Weapon
/// Selection Parsing") has already been applied by the time this exists, so every ModelName+Count
/// here is ready to resolve directly into a Domain.Roster.ModelLine without any further splitting
/// downstream. Weapons is a flat, per-model list (duplicates meaningful - e.g. two entries named
/// "Storm bolter" mean two copies of that weapon on the one model), matching
/// Domain.Roster.ModelLine.Weapons' own convention.</summary>
public sealed record ParsedModelGroup(string ModelName, int Count, IReadOnlyList<string> Weapons);

/// <summary>A single named unit block from the export - either a standalone entry or one member of
/// an ATTACHED UNITS group (see ParsedAttachmentGroup). Role (Leader/Support/Bodyguard) is not a
/// field here - it's captured structurally by which list of ParsedAttachmentGroup a member ends up
/// in, mirroring Domain.Roster.AttachedUnit's own Bodyguard/Attached split. The export's optional
/// role-line category parenthetical is discarded during parsing and never reaches this type.</summary>
public sealed record ParsedUnit(string Name, IReadOnlyList<ParsedModelGroup> ModelGroups, IReadOnlyList<string> Enhancements);

/// <summary>One "Attached unit N" group: exactly one Bodyguard-role member plus zero or more
/// Leader/Support-role members, preserved in export order - mirrors
/// Domain.Roster.AttachedUnit(Bodyguard, Attached) exactly, so enrichment can build one directly
/// from the other with no reordering.</summary>
public sealed record ParsedAttachmentGroup(ParsedUnit Bodyguard, IReadOnlyList<ParsedUnit> Attached);

/// <summary>The structured result of parsing raw GW-app 11e export text - army metadata plus every
/// unit's verbatim wargear selections, independent of any catalogue/BSData resolution. See
/// IArmyListParser.</summary>
public sealed record ParsedArmyList(
    string Name,
    int PointsSpent,
    IReadOnlyList<string> Faction,
    IReadOnlyList<string> Detachments,
    string ForceDisposition,
    string BattleSize,
    int PointsLimit,
    IReadOnlyList<ParsedAttachmentGroup> AttachmentGroups,
    IReadOnlyList<ParsedUnit> StandaloneUnits);
