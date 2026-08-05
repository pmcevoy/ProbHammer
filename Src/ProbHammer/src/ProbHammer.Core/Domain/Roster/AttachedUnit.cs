namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// One Bodyguard Unit plus an open collection of attached Leader/Support Units - 11e's "single
/// unit for all rules purposes." Not fixed Leader/Support slots: which ability produced each
/// attachment is not tracked (confirmed unneeded).
/// </summary>
public sealed class AttachedUnit(Unit bodyguard, IEnumerable<Unit> attached) : ICombatUnit
{
    public Unit Bodyguard { get; } = bodyguard;
    public IReadOnlyList<Unit> Attached { get; } = [.. attached];

    public IReadOnlyList<Unit> Components => [Bodyguard, .. Attached];
}
