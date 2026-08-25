using System.Text.Json;
using ProbHammer.Core.Domain.Import;

namespace ProbHammer.Web.Services;

/// <summary>Stores a session's successfully-parsed army import (see army-list-import's Per-Session
/// Roster Storage requirement) - the format-discriminated <see cref="StoredArmyImport"/> wrapper
/// around a <see cref="ParsedArmyList"/> (GW-app text) or a BattleScribe roster JSON (see
/// import-battlescribe-json-rosters' design.md), never a built
/// <see cref="ProbHammer.Core.Domain.Roster.ArmyRoster"/> itself (see design.md's "Session stores
/// the intermediate, not the graph"). Plain JSON round-trip via ASP.NET Core Session's string
/// storage, using System.Text.Json's polymorphic serialization (<see cref="StoredArmyImport"/>'s
/// own <c>[JsonDerivedType]</c> attributes) to preserve which variant was stored.</summary>
public interface ISessionArmyListStore
{
    void Save(ISession session, StoredArmyImport import);
    StoredArmyImport? Load(ISession session);
}

public sealed class SessionArmyListStore : ISessionArmyListStore
{
    private const string SessionKey = "ArmyImport";

    public void Save(ISession session, StoredArmyImport import) =>
        session.SetString(SessionKey, JsonSerializer.Serialize(import));

    public StoredArmyImport? Load(ISession session)
    {
        var json = session.GetString(SessionKey);
        return json is null ? null : JsonSerializer.Deserialize<StoredArmyImport>(json);
    }
}
