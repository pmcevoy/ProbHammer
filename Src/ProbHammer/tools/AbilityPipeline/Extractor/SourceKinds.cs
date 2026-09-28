using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tools.AbilityPipeline.Extractor;

/// <summary>The extraction corpus's source-kind vocabulary. Covers the spec's minimum six plus
/// "Optional grant" (Impulsor's "Shield Dome" and similar wargear-selection ability grants) -
/// <see cref="AbilityOrigin.OptionalGrant"/> is a real, distinct BSData shape from
/// <see cref="AbilityOrigin.Enhancement"/> (a formal Enhancements pool) and collapsing the two
/// would lose that distinction for no reason.</summary>
public static class SourceKinds
{
    public const string DatasheetAbility = "Datasheet ability";
    public const string Enhancement = "Enhancement";
    public const string OptionalGrant = "Optional grant";
    public const string DetachmentRule = "Detachment rule";
    public const string CoreRule = "Core rule";
    public const string ArmyRule = "Army rule";
    public const string RawSharedRule = "raw shared rule";

    /// <summary>When the same normalized text is observed under more than one kind across the
    /// corpus walk (e.g. a raw <c>Catalogue.Rules</c> definition that's also resolved into a
    /// CoreRule-origin Ability elsewhere), the most specific kind wins - lower number means more
    /// specific, and wins ties in <see cref="AbilityCorpusAggregator"/>.</summary>
    public static readonly IReadOnlyDictionary<string, int> Specificity = new Dictionary<string, int>
    {
        [DetachmentRule] = 0,
        [ArmyRule] = 1,
        [CoreRule] = 2,
        [Enhancement] = 3,
        [OptionalGrant] = 4,
        [DatasheetAbility] = 5,
        [RawSharedRule] = 6
    };

    public static string ForOrigin(AbilityOrigin origin) => origin switch
    {
        AbilityOrigin.Intrinsic => DatasheetAbility,
        AbilityOrigin.Enhancement => Enhancement,
        AbilityOrigin.OptionalGrant => OptionalGrant,
        AbilityOrigin.CoreRule => CoreRule,
        AbilityOrigin.ArmyRule => ArmyRule,
        AbilityOrigin.DetachmentRule => DetachmentRule,
        _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unrecognized AbilityOrigin")
    };
}
