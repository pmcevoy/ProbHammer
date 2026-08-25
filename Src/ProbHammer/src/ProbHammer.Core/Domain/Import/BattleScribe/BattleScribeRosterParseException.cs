namespace ProbHammer.Core.Domain.Import.BattleScribe;

/// <summary>Thrown when a recognized BattleScribe roster JSON payload (see
/// <see cref="BattleScribeRosterFormat.TryParse"/>) doesn't resolve into an <c>ArmyRoster</c> -
/// e.g. a unit/model selection with no resolvable Unit-typeName profile anywhere in its own or its
/// enclosing selection's <c>profiles</c>. Mirrors <c>ArmyListParseException</c>'s role for the
/// GW-app text pipeline: caught and reported on the `/Import` page rather than crashing (see
/// army-list-import's Import Submission requirement).</summary>
public sealed class BattleScribeRosterParseException(string message) : Exception(message);
