using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
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

        var fragments = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>(TestContext.Current.CancellationToken);
        fragments.Should().ContainKey(0);
        fragments![0].Should().Contain("Neophyte").And.Contain("(2/4)");
    }

    [Fact]
    public async Task PostingAnEmptyBatch_ReturnsAnEmptyResponse()
    {
        var client = await ClientWithImportedArmyAsync();
        var request = new LivePlaySyncRequest(CasualtyAdjustments: [], StatusAdjustments: []);

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var fragments = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>(TestContext.Current.CancellationToken);
        fragments.Should().BeEmpty();
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

        var fragments = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>(TestContext.Current.CancellationToken);
        fragments.Should().ContainKey(0); // still rendered, just identical to pristine
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

        var fragments = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>(TestContext.Current.CancellationToken);
        fragments.Should().BeEmpty();
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

        var fragments = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>(TestContext.Current.CancellationToken);
        fragments.Should().ContainKey(0);
        fragments![0].Should().Contain("status-glyph-battleshock is-active");
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

        var fragments = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>(TestContext.Current.CancellationToken);
        fragments![0].Should().Contain("(2/4)").And.Contain("status-glyph-battleshock is-active");
    }
}
