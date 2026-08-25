namespace ProbHammer.Core.Domain.Roster;

/// <summary>One resolved (Name, Text) rule pair attached to a selected Detachment (see
/// catalogue-json-ingestion's Detachment Rule Text Extraction) - a Detachment may declare zero,
/// one, or (confirmed real: Space Marines' "Black Spear Task Force") more than one.</summary>
public sealed record DetachmentRule(string Name, string Text);

/// <summary>One selected Detachment, resolved by name (see army-roster-enrichment's Detachment
/// Name Resolution) together with every rule it carries. Deliberately carries no DP cost - see
/// live-play-view's Army Header Rendering requirement ("no per-Detachment DP cost... shown
/// anywhere on the page").</summary>
public sealed record ResolvedDetachment(string Name, IReadOnlyList<DetachmentRule> Rules);
