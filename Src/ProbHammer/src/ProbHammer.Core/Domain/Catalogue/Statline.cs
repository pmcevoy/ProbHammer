namespace ProbHammer.Core.Domain.Catalogue;

public sealed record Statline(
    int M,
    int T,
    int Sv,
    int W,
    int Ld,
    ScalarCharacteristicView Oc)
{
    public InvulnerableSaveCharacteristicView InSv { get; init; } = InvulnerableSaveCharacteristicView.None;
}