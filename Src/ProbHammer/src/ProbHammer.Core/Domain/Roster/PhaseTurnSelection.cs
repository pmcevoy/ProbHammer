using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>The player-asserted current turn/phase of the game - genuine server-side state, not
/// derived. A <c>null</c> <see cref="Phase"/> represents a row-label-only selection (the whole
/// Turn, no specific Phase). Never computed or inferred - purely what the player last
/// selected.</summary>
public sealed record PhaseTurnSelection(GameTurn Turn, GamePhase? Phase)
{
    public static readonly PhaseTurnSelection Default = new(GameTurn.Mine, GamePhase.Command);
}
