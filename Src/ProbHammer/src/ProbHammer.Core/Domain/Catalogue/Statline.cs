namespace ProbHammer.Core.Domain.Catalogue;

public sealed record Statline(
    ScalarCharacteristicView M,
    ScalarCharacteristicView T,
    ScalarCharacteristicView Sv,
    ScalarCharacteristicView W,
    ScalarCharacteristicView Ld,
    ScalarCharacteristicView Oc)
{
    public InvulnerableSaveCharacteristicView InSv { get; init; } = InvulnerableSaveCharacteristicView.None;
}