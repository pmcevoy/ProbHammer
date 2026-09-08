namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>The rulebook vocabulary a <see cref="CharacteristicEffect"/> states, not pre-resolved
/// signed arithmetic - which sign each verb actually resolves to depends on the target
/// characteristic's own arithmetic family (a RollThreshold/ArmourPenetration/Plain "Kind" concept;
/// improving WS/Sv means *subtracting* from the printed number), resolving that is out of scope for
/// this change. See classify-rule-effects-from-text/design.md's "EffectVerb uses rulebook
/// vocabulary" decision.</summary>
public enum EffectVerb
{
    Improve,
    Worsen,
    Set
}
