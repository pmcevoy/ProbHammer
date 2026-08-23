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

    /// <summary>One flag for the whole AttachedUnit (Bodyguard + every Attached unit together),
    /// never per-component - see <see cref="ICombatUnit.IsHalfStrengthOverride"/>.</summary>
    public bool IsHalfStrengthOverride { get; set; }

    /// <summary>One flag for the whole AttachedUnit - see
    /// <see cref="ICombatUnit.IsBattleShocked"/>.</summary>
    public bool IsBattleShocked { get; set; }

    /// <summary>
    /// Humanized join of the Bodyguard's name with the Attached units' names: none attached
    /// yields the Bodyguard's name alone; one joins with "with A"; two join with "with A and B"
    /// (no comma); three or more use an Oxford comma ("with A, B, and C"). Duplicate attached
    /// names are not deduplicated - a known, accepted limitation.
    /// </summary>
    public string Name
    {
        get
        {
            var attachedNames = Attached.Select(u => u.Name).ToList();
            return attachedNames.Count switch
            {
                0 => Bodyguard.Name,
                1 => $"{Bodyguard.Name} with {attachedNames[0]}",
                2 => $"{Bodyguard.Name} with {attachedNames[0]} and {attachedNames[1]}",
                _ => $"{Bodyguard.Name} with {string.Join(", ", attachedNames[..^1])}, and {attachedNames[^1]}"
            };
        }
    }
}
