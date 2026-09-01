namespace ProbHammer.Core.Domain.Roster;

/// <summary>Which player's turn is current - part of the player-asserted "live game state" this
/// namespace already covers (see <see cref="ICombatUnit.IsBattleShocked"/>).</summary>
public enum GameTurn { Mine, Theirs }

/// <summary>The five phases of a battle round, in play order.</summary>
public enum GamePhase { Command, Movement, Shooting, Charge, Fight }

/// <summary>The player-asserted current turn/phase of the game - see live-play-phase-tracker's
/// "Phase/Turn Selection Is Genuine Server-Side State" requirement. A <c>null</c> <see cref="Phase"/>
/// represents a row-label-only selection (the whole Turn, no specific Phase). Never computed or
/// inferred - purely what the player last selected.</summary>
public sealed record PhaseTurnSelection(GameTurn Turn, GamePhase? Phase)
{
    public static readonly PhaseTurnSelection Default = new(GameTurn.Mine, GamePhase.Command);
}
