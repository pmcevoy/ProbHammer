using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>One rule/ability text's classification, as produced by the ability pipeline
/// (<c>tools/AbilityPipeline</c>) and exported into <see cref="AbilityClassificationCatalogue"/>'s
/// file. Describes what the text says; nothing here is resolved against a roster.</summary>
public sealed record AbilityClassification
{
    public required RuleTarget Target { get; init; }
    public required IReadOnlyList<ClassifiedEffect> Effects { get; init; }
    public IReadOnlyList<ChoiceGroup> ChoiceGroups { get; init; } = [];

    /// <summary>Every phase in which the player needs to see this ability.</summary>
    public IReadOnlyList<GamePhase> Phases { get; init; } = [];

    public GameTurn? TurnOwnership { get; init; }
    public UsageLimit? UsageLimit { get; init; }
    public required CoverageStatus CoverageStatus { get; init; }
    public string? UnclassifiedResidue { get; init; }

    /// <summary>Whether <paramref name="effect"/> applies with no player decision or game-state check:
    /// no residual condition, no choice branch, and no usage limit or turn restriction on the
    /// ability.</summary>
    public bool IsUnconditional(ClassifiedEffect effect) =>
        effect.ResidualConditionBucket == ResidualConditionBucket.None &&
        effect.ChoiceBranch is null &&
        UsageLimit is null &&
        TurnOwnership is null;

    public IEnumerable<T> UnconditionalEffects<T>() where T : RuleEffect =>
        Effects.Where(IsUnconditional).Select(e => e.Effect).OfType<T>();

    public IEnumerable<T> ConditionalEffects<T>() where T : RuleEffect =>
        Effects.Where(e => !IsUnconditional(e)).Select(e => e.Effect).OfType<T>();
}

public sealed record ClassifiedEffect
{
    public required RuleEffect Effect { get; init; }
    public required ResidualConditionBucket ResidualConditionBucket { get; init; }
    public string? ConditionText { get; init; }
    public ChoiceBranch? ChoiceBranch { get; init; }
}

/// <summary>A "select <see cref="MinSelect"/>-<see cref="MaxSelect"/> of the following" choice;
/// each option's effects are the <see cref="ClassifiedEffect"/>s whose
/// <see cref="ClassifiedEffect.ChoiceBranch"/> points at it.</summary>
public sealed record ChoiceGroup(int MinSelect, int MaxSelect, IReadOnlyList<string> Options, string? ConditionText = null);

public sealed record ChoiceBranch(int Group, int Option);

/// <summary>Whether a condition gating an effect could be computed from tracked roster state
/// (<see cref="EvaluableNow"/>) or never can be (<see cref="Never"/>).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ResidualConditionBucket>))]
public enum ResidualConditionBucket
{
    [JsonStringEnumMemberName("none")] None,
    [JsonStringEnumMemberName("evaluable-now")] EvaluableNow,
    [JsonStringEnumMemberName("never")] Never
}

[JsonConverter(typeof(JsonStringEnumConverter<UsageLimit>))]
public enum UsageLimit
{
    [JsonStringEnumMemberName("Once per battle")] OncePerBattle,
    [JsonStringEnumMemberName("Twice per battle")] TwicePerBattle,
    [JsonStringEnumMemberName("Once per battle round")] OncePerBattleRound,
    [JsonStringEnumMemberName("Once per turn")] OncePerTurn,
    [JsonStringEnumMemberName("Once per phase")] OncePerPhase
}

/// <summary>How much of the text the classification captured. Never gates use - a partial or
/// unclassifiable record's captured fields are used as-is.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoverageStatus>))]
public enum CoverageStatus
{
    [JsonStringEnumMemberName("complete")] Complete,
    [JsonStringEnumMemberName("partial")] Partial,
    [JsonStringEnumMemberName("unclassifiable")] Unclassifiable
}
