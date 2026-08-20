using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using static ProbHammer.Tests.Web.ImportTestHelper;

namespace ProbHammer.Tests.Web;

/// <summary>Integration tests for the real `/Import` -> `/LivePlay` flow (army-list-import,
/// live-play-view's redirect requirement), exercising the actual HTTP pipeline - parsing,
/// enrichment against the app's bundled BsData snapshot, session storage - rather than any one
/// piece in isolation. Mirrors LivePlayCasualtyEndpointTests' existing
/// WebApplicationFactory-based convention.</summary>
public class ImportFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ImportFlowTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task SuccessfulImport_RedirectsToLivePlay_WhichRendersTheImportedArmy()
    {
        var client = _factory.CreateClient();

        var response = await ImportAsync(client, ReadRealExport("gw-app-export.txt"));

        response.EnsureSuccessStatusCode();
        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/LivePlay");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain("Impulsor");
    }

    [Fact]
    public async Task UnparseableText_IsReportedOnTheImportPage_WithoutCrashing()
    {
        var client = _factory.CreateClient();

        var response = await ImportAsync(client, "this is not a valid army list export at all");

        response.EnsureSuccessStatusCode();
        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/Import");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain("import-error");
    }

    [Fact]
    public async Task UnresolvableUnitName_IsReportedOnTheImportPage_WithoutCrashing()
    {
        var client = _factory.CreateClient();
        const string unresolvableExport = """
            Test Army (10 Points)

            Orks

            Test Detachment (1 Detachment Points)
            Test Disposition
            Incursion (1,000 Points)

            CHARACTERS

            Nonexistent Ork Warboss Xyz (10 Points)
              • 1x Nonexistent Weapon

            Exported with App Version: v2.4.0 (1), Data Version: v925
            """;

        var response = await ImportAsync(client, unresolvableExport);

        response.EnsureSuccessStatusCode();
        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/Import");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain("Nonexistent Ork Warboss Xyz");
    }

    [Fact]
    public async Task FailedImport_LeavesAPreviouslySuccessfulSessionImportUntouched()
    {
        var client = _factory.CreateClient();
        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));

        var failedResponse = await ImportAsync(client, "not a valid export");
        failedResponse.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/Import");

        var liveResponse = await client.GetAsync("/LivePlay", TestContext.Current.CancellationToken);
        liveResponse.EnsureSuccessStatusCode();
        liveResponse.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/LivePlay");
        var html = await liveResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain("Impulsor");
    }

    [Fact]
    public async Task TwoConcurrentSessions_EachSeeOnlyTheirOwnImportedArmyList()
    {
        var clientA = _factory.CreateClient();
        var clientB = _factory.CreateClient();

        await ImportAsync(clientA, ReadRealExport("gw-app-export.txt"));
        await ImportAsync(clientB, ReadRealExport("gw-app-export-3-dp.txt"));

        var htmlA = await (await clientA.GetAsync("/LivePlay", TestContext.Current.CancellationToken))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var htmlB = await (await clientB.GetAsync("/LivePlay", TestContext.Current.CancellationToken))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        htmlA.Should().Contain("Impulsor").And.NotContain("Company Heroes");
        htmlB.Should().Contain("Company Heroes").And.NotContain("Impulsor");
    }

    [Fact]
    public async Task LivePlay_WithNoActiveSessionImport_RedirectsToImport()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/LivePlay", TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().BeInRange(300, 399);
        response.Headers.Location!.OriginalString.Should().Be("/Import");
    }

    [Fact]
    public async Task LivePlay_RebuildsFreshOnEveryRequest_AcrossRepeatedRenders()
    {
        var client = _factory.CreateClient();
        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));

        var first = await (await client.GetAsync("/LivePlay", TestContext.Current.CancellationToken))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var second = await (await client.GetAsync("/LivePlay", TestContext.Current.CancellationToken))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        first.Should().Contain("Impulsor");
        second.Should().Contain("Impulsor");
    }
}
