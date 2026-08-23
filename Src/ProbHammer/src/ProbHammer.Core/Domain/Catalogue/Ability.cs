namespace ProbHammer.Core.Domain.Catalogue;

public sealed record AbilityChoice(string Name, string Text);

public sealed record Ability
{
    public required string Name { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<AbilityChoice> Choices { get; init; } = [];
    public required AbilityScope Scope { get; init; }
    public required AbilityOrigin Origin { get; init; }
}
