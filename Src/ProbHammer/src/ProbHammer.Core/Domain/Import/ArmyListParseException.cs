namespace ProbHammer.Core.Domain.Import;

/// <summary>Thrown by ArmyListParser when export text doesn't match a shape this parser
/// recognizes - most notably a model group whose non-common weapon counts don't sum exactly to
/// its total model count (see "Model Group and Weapon Selection Parsing"'s fail-loud requirement).
/// Carries the offending unit name and the raw wargear text as properties, not just baked into the
/// message, mirroring AmbiguousCharacteristicException's existing convention.</summary>
public sealed class ArmyListParseException(string message, string? unitName = null, string? rawText = null)
    : Exception(message)
{
    public string? UnitName { get; } = unitName;
    public string? RawText { get; } = rawText;
}
