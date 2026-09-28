namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>A "select N of the following" choice: <see cref="MinSelect"/> to <see cref="MaxSelect"/>
/// of <see cref="Options"/> active at once (1,1 for "select one"; 0,3 for "select up to three").
/// Options are labels only - a branch's own effects are ordinary <see cref="ClassifiedEffect"/>s
/// tagged with a <see cref="ChoiceBranch"/>, keeping the schema flat and non-recursive. A label with
/// no tagged effects is a branch this schema can't represent. When the text allows more picks under a
/// condition ("select both if this unit Charged"), <see cref="MaxSelect"/> is the larger count and
/// <see cref="ConditionText"/> says when it applies - the app trusts the player rather than enforcing
/// it.</summary>
public sealed record ChoiceGroup(
    int MinSelect,
    int MaxSelect,
    IReadOnlyList<string> Options,
    string? ConditionText = null);

/// <summary>Marks an effect as belonging to option <see cref="Option"/> of
/// <see cref="ClassificationResult.ChoiceGroups"/>[<see cref="Group"/>] rather than applying
/// unconditionally. Both are zero-based indices.</summary>
public sealed record ChoiceBranch(int Group, int Option);