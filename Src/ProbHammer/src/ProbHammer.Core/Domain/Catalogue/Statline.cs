namespace ProbHammer.Core.Domain.Catalogue;

public sealed record Statline(
    int Movement,
    int Toughness,
    int Save,
    int Wounds,
    int Leadership,
    int ObjectiveControl);
