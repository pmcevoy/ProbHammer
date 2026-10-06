using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Web.Pages;
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
    public async Task SuccessfulBattleScribeJsonImport_RedirectsToLivePlay_MatchingTheEquivalentTextImport()
    {
        // gw-app-export-templars.json is the same real Templars list as gw-app-export.txt,
        // re-exported from NewRecruit as BattleScribe roster JSON - both pipelines should produce
        // an equivalent rendered army for the same real list, which is exactly the cross-check
        // this sample was chosen for.
        var client = _factory.CreateClient();

        var response = await ImportAsync(client, ReadRealExport("gw-app-export-templars.json"));

        response.EnsureSuccessStatusCode();
        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/LivePlay");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain("High Marshal Helbrecht").And.Contain("Crusader Squad").And.Contain("Sword Brethren Squad").And.Contain("Impulsor");
    }

    [Fact]
    public async Task NonBattleScribeJson_FallsThroughToTheTextPipeline_AndFailsWithItsOwnDiagnostics()
    {
        var client = _factory.CreateClient();

        var response = await ImportAsync(client, """{"someOtherJson": true}""");

        response.EnsureSuccessStatusCode();
        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/Import");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain("import-error");
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

    [Fact]
    public async Task SiteRoot_WithACurrentList_LandsOnLivePlay()
    {
        var client = _factory.CreateClient();
        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/LivePlay");
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain("Impulsor");
    }

    [Fact]
    public async Task SiteRoot_WithoutAList_LandsOnImport()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/Import");
    }

    [Fact]
    public async Task ImportForm_PostsToImport()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/Import", TestContext.Current.CancellationToken);

        html.Should().Contain("<form method=\"post\" action=\"/Import\">");
    }

    [Fact]
    public async Task ExpiredForm_ReRendersImport_WithThePasteKept_AndLeavesTheSessionListUnchanged()
    {
        var client = _factory.CreateClient();
        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));
        var staleToken = ExtractAntiForgeryToken(
            await _factory.CreateClient().GetStringAsync("/Import", TestContext.Current.CancellationToken));

        var response = await PostImportAsync(client, ReadRealExport("gw-app-export-3-dp.txt"), staleToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/Import");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain("This page had expired").And.Contain("Company Heroes");
        (await client.GetStringAsync("/LivePlay", TestContext.Current.CancellationToken))
            .Should().Contain("Impulsor").And.NotContain("Company Heroes");
    }

    [Fact]
    public async Task ExpiredForm_SubmittedAgainFromTheReRenderedPage_Imports()
    {
        var client = _factory.CreateClient();
        var staleToken = ExtractAntiForgeryToken(
            await _factory.CreateClient().GetStringAsync("/Import", TestContext.Current.CancellationToken));
        var reRendered = await (await PostImportAsync(client, ReadRealExport("gw-app-export.txt"), staleToken))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var response = await PostImportAsync(client, ReadRealExport("gw-app-export.txt"), ExtractAntiForgeryToken(reRendered));

        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/LivePlay");
    }

    [Fact]
    public async Task ImportWithNoTokenAtAll_ImportsNothing()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/Import", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["ExportText"] = ReadRealExport("gw-app-export.txt") }),
            TestContext.Current.CancellationToken);

        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/Import");
        (await client.GetAsync("/LivePlay", TestContext.Current.CancellationToken))
            .RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/Import");
    }

    private static string ImportIdOf(string liveHtml) =>
        System.Text.RegularExpressions.Regex.Match(liveHtml, "data-import-id=\"([^\"]+)\"").Groups[1].Value;

    [Fact]
    public async Task EachSuccessfulImport_RendersADifferentImportId_StableAcrossRenders()
    {
        var client = _factory.CreateClient();
        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));
        var first = ImportIdOf(await client.GetStringAsync("/LivePlay", TestContext.Current.CancellationToken));
        var again = ImportIdOf(await client.GetStringAsync("/LivePlay", TestContext.Current.CancellationToken));

        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));
        var second = ImportIdOf(await client.GetStringAsync("/LivePlay", TestContext.Current.CancellationToken));

        first.Should().NotBeEmpty().And.Be(again);
        second.Should().NotBeEmpty().And.NotBe(first);
    }

    private static async Task SelectTheirChargeAsync(HttpClient client) =>
        (await client.PostAsJsonAsync("/api/live-play/casualties",
            new LivePlaySyncRequest([], [], new PhaseTurnAdjustment(GameTurn.Theirs, GamePhase.Charge)),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

    private static bool TheirChargeIsSelected(string liveHtml) =>
        System.Text.RegularExpressions.Regex.IsMatch(liveHtml,
            "class=\"phase-turn-cell is-active\"\\s+data-turn=\"theirs\" data-phase=\"charge\"");

    [Fact]
    public async Task ASuccessfulImport_ResetsPhaseTurnToDefault()
    {
        var client = _factory.CreateClient();
        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));
        await SelectTheirChargeAsync(client);
        TheirChargeIsSelected(await client.GetStringAsync("/LivePlay", TestContext.Current.CancellationToken))
            .Should().BeTrue();

        await ImportAsync(client, ReadRealExport("gw-app-export-3-dp.txt"));

        TheirChargeIsSelected(await client.GetStringAsync("/LivePlay", TestContext.Current.CancellationToken))
            .Should().BeFalse();
    }

    [Fact]
    public async Task AFailedOrExpiredImport_KeepsPhaseTurn()
    {
        var client = _factory.CreateClient();
        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));
        await SelectTheirChargeAsync(client);
        var staleToken = ExtractAntiForgeryToken(
            await _factory.CreateClient().GetStringAsync("/Import", TestContext.Current.CancellationToken));

        await ImportAsync(client, "not a valid export");
        await PostImportAsync(client, ReadRealExport("gw-app-export-3-dp.txt"), staleToken);

        TheirChargeIsSelected(await client.GetStringAsync("/LivePlay", TestContext.Current.CancellationToken))
            .Should().BeTrue();
    }

    [Fact]
    public async Task LivePlay_LinksToImport_AboveTheArmyHeader()
    {
        var client = _factory.CreateClient();
        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));

        var html = await client.GetStringAsync("/LivePlay", TestContext.Current.CancellationToken);

        var link = html.IndexOf("<a href=\"/Import\">Import a new list</a>", StringComparison.Ordinal);
        link.Should().BePositive().And.BeLessThan(html.IndexOf("army-header", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Import_LinksBackToTheCurrentList_OnlyWhenThereIsOne()
    {
        var client = _factory.CreateClient();
        const string backLink = "<a href=\"/LivePlay\">Back to current list</a>";

        (await client.GetStringAsync("/Import", TestContext.Current.CancellationToken)).Should().NotContain(backLink);
        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));
        (await client.GetStringAsync("/Import", TestContext.Current.CancellationToken)).Should().Contain(backLink);
    }

    [Fact]
    public async Task VisitingImport_WithoutImporting_ChangesNoState()
    {
        var client = _factory.CreateClient();
        await ImportAsync(client, ReadRealExport("gw-app-export.txt"));
        await SelectTheirChargeAsync(client);
        var before = ImportIdOf(await client.GetStringAsync("/LivePlay", TestContext.Current.CancellationToken));

        await client.GetStringAsync("/Import", TestContext.Current.CancellationToken);

        var after = await client.GetStringAsync("/LivePlay", TestContext.Current.CancellationToken);
        ImportIdOf(after).Should().Be(before);
        TheirChargeIsSelected(after).Should().BeTrue();
    }
}
