namespace ProbHammer.Core.Domain.Catalogue;

public sealed record Statline(
    int M,
    int T,
    int Sv,
    int W,
    int Ld,
    int Oc)
{
    public int InSv { get; init; }
}