namespace ProbHammer.Core.Domain.Import;

/// <summary>Parses raw GW-app 11th-edition army-list export text into a ParsedArmyList, without
/// depending on or resolving against any catalogue data (see army-roster-enrichment for that next
/// stage). Throws ArmyListParseException with a diagnostic when the text doesn't match the shapes
/// this parser recognizes - never guesses a best-effort result.</summary>
public interface IArmyListParser
{
    ParsedArmyList Parse(string exportText);
}
