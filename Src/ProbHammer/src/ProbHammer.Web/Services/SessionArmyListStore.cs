using System.Text.Json;
using ProbHammer.Core.Domain.Import;

namespace ProbHammer.Web.Services;

/// <summary>Stores a session's successfully-parsed army list (see army-list-import's Per-Session
/// Roster Storage requirement) - the intermediate <see cref="ParsedArmyList"/>, never the built
/// <see cref="ProbHammer.Core.Domain.Roster.ArmyRoster"/> itself (see design.md's "Session stores
/// the intermediate, not the graph"). Plain JSON round-trip via ASP.NET Core Session's string
/// storage - <see cref="ParsedArmyList"/> and everything it references are plain records of
/// primitives/lists, with no <see cref="ProbHammer.Core.Domain.Catalogue.Datasheet"/>/abstract
/// <see cref="ProbHammer.Core.Domain.Catalogue.WeaponProfile"/> graph to serialize.</summary>
public interface ISessionArmyListStore
{
    void Save(ISession session, ParsedArmyList armyList);
    ParsedArmyList? Load(ISession session);
}

public sealed class SessionArmyListStore : ISessionArmyListStore
{
    private const string SessionKey = "ParsedArmyList";

    public void Save(ISession session, ParsedArmyList armyList) =>
        session.SetString(SessionKey, JsonSerializer.Serialize(armyList));

    public ParsedArmyList? Load(ISession session)
    {
        var json = session.GetString(SessionKey);
        return json is null ? null : JsonSerializer.Deserialize<ParsedArmyList>(json);
    }
}
