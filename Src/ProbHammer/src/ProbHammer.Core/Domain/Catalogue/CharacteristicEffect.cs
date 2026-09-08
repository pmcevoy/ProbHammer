namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>One atomic, unconditional mutation a rule/ability's text states against exactly one named
/// <see cref="Statline"/> scalar characteristic. <see cref="Characteristic"/> is a plain string
/// matching Statline's own scalar property names ("M"/"T"/"Sv"/"W"/"Ld"/"Oc"), the same convention
/// <see cref="CharacteristicModifierCandidate"/> already uses - not a new enum.
/// <see cref="Amount"/> is the text's own stated value, unsigned/unresolved - see
/// <see cref="EffectVerb"/>'s own doc comment for why resolving a final signed value is out of
/// scope.</summary>
public sealed record CharacteristicEffect(string Characteristic, EffectVerb Verb, int Amount);
