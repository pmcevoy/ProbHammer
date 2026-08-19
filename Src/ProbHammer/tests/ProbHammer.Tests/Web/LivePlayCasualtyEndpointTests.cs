using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ProbHammer.Web.Pages;

namespace ProbHammer.Tests.Web;

/// <summary>Integration test for POST /api/live-play/casualties, exercising the real endpoint end
/// to end (roster rebuild, adjustment resolution, and Razor-partial-to-string rendering via
/// RazorPartialRenderer) rather than any one piece in isolation.</summary>
public class LivePlayCasualtyEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayCasualtyEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task PostingAnAdjustment_ReturnsAFragmentReflectingTheNewRemainingCount()
    {
        var client = _factory.CreateClient();
        var adjustments = new[]
        {
            new CasualtyAdjustment(new CasualtyCoordinate(0, "Crusader Squad", "Neophyte", -1), RemainingCount: 2)
        };

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", adjustments, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var fragments = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>(TestContext.Current.CancellationToken);
        fragments.Should().ContainKey(0);
        fragments![0].Should().Contain("Neophyte").And.Contain("(2/4)");
    }

    [Fact]
    public async Task PostingAnEmptyBatch_ReturnsAnEmptyResponse()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", Array.Empty<CasualtyAdjustment>(), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var fragments = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>(TestContext.Current.CancellationToken);
        fragments.Should().BeEmpty();
    }

    [Fact]
    public async Task PostingAnUnresolvableAdjustment_DoesNotFail_AndStillReturnsThatUnitsFragment()
    {
        var client = _factory.CreateClient();
        var adjustments = new[]
        {
            new CasualtyAdjustment(new CasualtyCoordinate(0, "No Such Component", "No Such Statline", -1), RemainingCount: 0)
        };

        var response = await client.PostAsJsonAsync("/api/live-play/casualties", adjustments, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var fragments = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>(TestContext.Current.CancellationToken);
        fragments.Should().ContainKey(0); // still rendered, just identical to pristine
    }
}
