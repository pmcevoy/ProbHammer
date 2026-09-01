using System.Text.Json;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Web.Services;

/// <summary>Stores a session's current <see cref="PhaseTurnSelection"/> - mirrors
/// <see cref="ISessionArmyListStore"/>'s own plain JSON round-trip through ASP.NET Core Session's
/// string storage exactly, under its own session key (see live-play-phase-tracker's design.md
/// Decision 3).</summary>
public interface IPhaseTurnStore
{
    void Save(ISession session, PhaseTurnSelection selection);
    PhaseTurnSelection? Load(ISession session);
}

public sealed class PhaseTurnStore : IPhaseTurnStore
{
    private const string SessionKey = "PhaseTurn";

    public void Save(ISession session, PhaseTurnSelection selection) =>
        session.SetString(SessionKey, JsonSerializer.Serialize(selection));

    public PhaseTurnSelection? Load(ISession session)
    {
        var json = session.GetString(SessionKey);
        return json is null ? null : JsonSerializer.Deserialize<PhaseTurnSelection>(json);
    }
}
