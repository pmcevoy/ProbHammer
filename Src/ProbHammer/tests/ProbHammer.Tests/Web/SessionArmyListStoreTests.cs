using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Import.BattleScribe.Json;
using ProbHammer.Web.Services;

namespace ProbHammer.Tests.Web;

public class SessionArmyListStoreTests
{
    private static readonly StoredArmyImport AnyImport = new BattleScribeArmyImport(new BsRoster());

    [Fact]
    public void EachSave_GetsANewImportId()
    {
        var store = new SessionArmyListStore();
        var session = new FakeSession();

        store.Save(session, AnyImport);
        var first = store.GetOrAssignImportId(session);
        store.Save(session, AnyImport);

        store.GetOrAssignImportId(session).Should().NotBe(first);
    }

    [Fact]
    public void ASessionSavedBeforeIdsExisted_IsAssignedAStableId()
    {
        var store = new SessionArmyListStore();
        var session = new FakeSession();
        session.SetString("ArmyImport", "{}");

        var assigned = store.GetOrAssignImportId(session);

        store.GetOrAssignImportId(session).Should().Be(assigned).And.NotBeEmpty();
    }

    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = [];

        public bool IsAvailable => true;
        public string Id => "fake";
        public IEnumerable<string> Keys => _values.Keys;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _values.TryGetValue(key, out value);
        public void Set(string key, byte[] value) => _values[key] = value;
        public void Remove(string key) => _values.Remove(key);
        public void Clear() => _values.Clear();
    }
}
