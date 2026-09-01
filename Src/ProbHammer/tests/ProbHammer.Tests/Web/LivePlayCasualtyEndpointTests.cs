using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Web.Pages;
using static ProbHammer.Tests.Web.ImportTestHelper;

namespace ProbHammer.Tests.Web;

/// <summary>Integration test for POST /api/live-play/casualties, exercising the real endpoint end
/// to end (roster rebuild, adjustment resolution, and Razor-partial-to-string rendering via
/// RazorPartialRenderer) rather than any one piece in isolation. Since `/LivePlay` now renders the
/// session's own imported-and-enriched ArmyRoster (see live-play-view's Modified Requirements),
/// each test first imports the real captured `gw-app-export.txt` via the same client so the session
/// this endpoint reads from has an active army list, matching the coordinates
/// (`"Crusader Squad"`/`"Neophyte"`) this suite has always asserted against.</summary>
public class LivePlayCasualtyEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayCasualtyEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<HttpClient> ClientWithImportedArmyAsync()
    {
        var client = _factory.CreateClient();
        var response = await ImportAsync(client, ReadRealExport("gw-app-export.txt"));
        response.EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task PostingAnAdjustment_ReturnsAFragmentReflectingTheNewRemainingCount()
    {
        var client = await ClientWithImportedArmyAsync();
        var request = new LivePlaySyncRequest(
            CasualtyAdjustments: [new CasualtyAdjustment(new CasualtyCoordinate(0, "Crusader Squad", "Neophyte", -1), RemainingCount: 2)],
            StatusAdjustments: []);

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LivePlaySyncResponse>(TestContext.Current.CancellationToken);
        body!.Fragments.Should().ContainKey(0);
        body.Fragments[0].Should().Contain("Neophyte").And.Contain("(2/4)");
        body.ForcedSections.Should().BeEmpty(); // no PhaseTurnAdjustment in this request
    }

    [Fact]
    public async Task PostingAnEmptyBatch_ReturnsAnEmptyResponse()
    {
        var client = await ClientWithImportedArmyAsync();
        var request = new LivePlaySyncRequest(CasualtyAdjustments: [], StatusAdjustments: []);

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LivePlaySyncResponse>(TestContext.Current.CancellationToken);
        body!.Fragments.Should().BeEmpty();
        body.ForcedSections.Should().BeEmpty();
    }

    [Fact]
    public async Task PostingAnUnresolvableAdjustment_DoesNotFail_AndStillReturnsThatUnitsFragment()
    {
        var client = await ClientWithImportedArmyAsync();
        var request = new LivePlaySyncRequest(
            CasualtyAdjustments: [new CasualtyAdjustment(new CasualtyCoordinate(0, "No Such Component", "No Such Statline", -1), RemainingCount: 0)],
            StatusAdjustments: []);

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LivePlaySyncResponse>(TestContext.Current.CancellationToken);
        body!.Fragments.Should().ContainKey(0); // still rendered, just identical to pristine
    }

    [Fact]
    public async Task PostingAnAdjustment_WithNoActiveSessionImport_ReturnsAnEmptyResponse()
    {
        var client = _factory.CreateClient();
        var request = new LivePlaySyncRequest(
            CasualtyAdjustments: [new CasualtyAdjustment(new CasualtyCoordinate(0, "Crusader Squad", "Neophyte", -1), RemainingCount: 2)],
            StatusAdjustments: []);

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LivePlaySyncResponse>(TestContext.Current.CancellationToken);
        body!.Fragments.Should().BeEmpty();
    }

    [Fact]
    public async Task PostingAStatusAdjustment_ReturnsAFragmentReflectingBattleShockedStatus()
    {
        var client = await ClientWithImportedArmyAsync();
        var request = new LivePlaySyncRequest(
            CasualtyAdjustments: [],
            StatusAdjustments: [new UnitStatusAdjustment(0, IsHalfStrength: false, IsBattleShocked: true)]);

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LivePlaySyncResponse>(TestContext.Current.CancellationToken);
        body!.Fragments.Should().ContainKey(0);
        body.Fragments[0].Should().Contain("status-glyph-battleshock is-active");
    }

    [Fact]
    public async Task PostingBothBatchesTogether_AppliesBothInOneRebuild()
    {
        var client = await ClientWithImportedArmyAsync();
        var request = new LivePlaySyncRequest(
            CasualtyAdjustments: [new CasualtyAdjustment(new CasualtyCoordinate(0, "Crusader Squad", "Neophyte", -1), RemainingCount: 2)],
            StatusAdjustments: [new UnitStatusAdjustment(0, IsHalfStrength: false, IsBattleShocked: true)]);

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LivePlaySyncResponse>(TestContext.Current.CancellationToken);
        body!.Fragments[0].Should().Contain("(2/4)").And.Contain("status-glyph-battleshock is-active");
    }

    [Fact]
    public async Task PostingAPhaseTurnAdjustment_PersistsSelection_AndReturnsEveryUnitsFragmentPlusForcedSections()
    {
        var client = await ClientWithImportedArmyAsync();
        var request = new LivePlaySyncRequest(
            CasualtyAdjustments: [],
            StatusAdjustments: [],
            PhaseTurnAdjustment: new PhaseTurnAdjustment(GameTurn.Theirs, GamePhase.Fight));

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LivePlaySyncResponse>(TestContext.Current.CancellationToken);
        body!.Fragments.Should().NotBeEmpty(); // every unit in the roster, not just an adjusted one
        body.ForcedSections.Should().BeEquivalentTo(["statline", "ranged", "melee", "keywords"]);

        // The selection persists across a subsequent request with no adjustment of its own -
        // reflected in the just-rendered fragment's own baked-in disclosure state (Their Turn/Fight
        // expands Statline and Melee).
        var reloadResponse = await client.GetAsync("/LivePlay", TestContext.Current.CancellationToken);
        reloadResponse.EnsureSuccessStatusCode();
        var html = await reloadResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Regex.IsMatch(html, "class=\"phase-turn-cell is-active\"\\s+data-turn=\"theirs\"\\s+data-phase=\"fight\"")
            .Should().BeTrue();
    }
}
