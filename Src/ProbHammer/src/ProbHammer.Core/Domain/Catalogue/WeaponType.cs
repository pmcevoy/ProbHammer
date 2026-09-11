using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Serializes as its own member name ("Ranged"/"Melee"), not the default int - mirrors
/// <see cref="EffectVerb"/>'s own converter-on-the-enum convention, for the same reason: every
/// serializer that touches it (the checked-in <c>RuleClassificationBaseline</c> JSON included)
/// agrees with no per-call-site options to keep in sync.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WeaponType
{
    Ranged,
    Melee
}