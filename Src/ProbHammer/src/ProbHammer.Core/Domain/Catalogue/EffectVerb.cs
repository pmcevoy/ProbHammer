using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>The rulebook vocabulary a <see cref="RuleEffect"/> states, not pre-resolved
/// signed arithmetic - which sign each verb actually resolves to depends on the target
/// characteristic's own arithmetic family (a RollThreshold/ArmourPenetration/Plain "Kind" concept;
/// improving WS/Sv means *subtracting* from the printed number), resolving that is a separate
/// concern. Serializes as its own member name (e.g. "Improve") and reads case-insensitively -
/// putting the converter directly on the enum
/// means every serializer that touches it agrees with no per-call-site options to keep in
/// sync.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectVerb
{
    Improve,
    Worsen,
    Set
}