using System.Text.Json;
using ProbHammer.Core.Domain.Import;

namespace ProbHammer.Web.Services;

/// <summary>Stores a session's successfully-parsed army import - the format-discriminated
/// <see cref="StoredArmyImport"/> wrapper around a <see cref="ParsedArmyList"/> (GW-app text) or a
/// BattleScribe roster JSON, never a built
/// <see cref="ProbHammer.Core.Domain.Roster.ArmyRoster"/> itself: the session stores
/// the intermediate, not the graph. Plain JSON round-trip via ASP.NET Core Session's string
/// storage, using System.Text.Json's polymorphic serialization (<see cref="StoredArmyImport"/>'s
/// own <c>[JsonDerivedType]</c> attributes) to preserve which variant was stored.</summary>
public interface ISessionArmyListStore
{
    void Save(ISession session, StoredArmyImport import);
    StoredArmyImport? Load(ISession session);

    /// <summary>Identifies the stored import, so browser state recorded for one import is never
    /// applied to the next. A session saved before ids existed is assigned one here.</summary>
    string GetOrAssignImportId(ISession session);
}

public sealed class SessionArmyListStore : ISessionArmyListStore
{
    private const string SessionKey = "ArmyImport";
    private const string ImportIdKey = "ArmyImportId";

    public void Save(ISession session, StoredArmyImport import)
    {
        session.SetString(SessionKey, JsonSerializer.Serialize(import));
        session.SetString(ImportIdKey, NewImportId());
    }

    public StoredArmyImport? Load(ISession session)
    {
        var json = session.GetString(SessionKey);
        return json is null ? null : JsonSerializer.Deserialize<StoredArmyImport>(json);
    }

    public string GetOrAssignImportId(ISession session)
    {
        if (session.GetString(ImportIdKey) is { } id)
            return id;

        id = NewImportId();
        session.SetString(ImportIdKey, id);
        return id;
    }

    private static string NewImportId() => Guid.NewGuid().ToString("N");
}
