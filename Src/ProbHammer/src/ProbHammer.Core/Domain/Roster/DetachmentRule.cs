namespace ProbHammer.Core.Domain.Roster;

/// <summary>One resolved (Name, Text) rule pair attached to a selected Detachment - a Detachment
/// may declare zero, one, or (confirmed real: Space Marines' "Black Spear Task Force") more than
/// one.</summary>
public sealed record DetachmentRule(string Name, string Text);

/// <summary>One selected Detachment, resolved by name together with every rule it carries.
/// Deliberately carries no DP cost - no per-Detachment DP cost is shown anywhere on the
/// page.</summary>
public sealed record ResolvedDetachment(string Name, IReadOnlyList<DetachmentRule> Rules);
