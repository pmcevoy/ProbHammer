namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// One Bodyguard Unit plus an open collection of attached Leader/Support Units - 11e's "single
/// unit for all rules purposes." Not fixed Leader/Support slots: which ability produced each
/// attachment is not tracked (confirmed unneeded).
/// </summary>
public sealed class AttachedUnit : ICombatUnit
{
    public Unit Bodyguard { get; }
    public IReadOnlyList<Unit> Attached { get; }

    public AttachedUnit(Unit bodyguard, IEnumerable<Unit> attached)
    {
        Bodyguard = bodyguard;
        Attached = attached.ToList();
    }

    public IReadOnlyList<Unit> Components => new[] { Bodyguard }.Concat(Attached).ToList();
}
