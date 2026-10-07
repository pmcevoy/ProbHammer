using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Import.BattleScribe;
using ProbHammer.Web.Services;
using static ProbHammer.Tests.Web.ImportTestHelper;

namespace ProbHammer.Tests.Web;

public class BattleScribeCoreRuleFallbackTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private ArmyRosterBuildResult Build(string json)
    {
        BattleScribeRosterFormat.TryParse(json, out var roster).Should().BeTrue();
        return factory.Services.GetRequiredService<IArmyRosterProvider>().Build(new BattleScribeArmyImport(roster!));
    }

    [Fact]
    public void AMatchingGameSystem_AddsCoreRules_AndKeepsTheRostersOwnRules()
    {
        var glossary = Build(ReadRealExport("nr-orks.json")).Glossary;

        glossary.TryResolve("LETHAL HITS").Should().NotBeNull();
        glossary.TryResolve("Waaagh!").Should().NotBeNull();
    }

    [Fact]
    public void AnUnmatchedGameSystem_StillImports_WithOnlyTheRostersOwnRules()
    {
        var json = ReadRealExport("nr-orks.json").Replace("sys-352e-adc2-7639-d610", "sys-unknown");

        var glossary = Build(json).Glossary;

        glossary.TryResolve("Waaagh!").Should().NotBeNull();
        glossary.TryResolve("LETHAL HITS").Should().BeNull();
    }

    [Fact]
    public async Task ImportingNrOrks_RendersItsWeaponKeywordChipsAsResolved()
    {
        var client = factory.CreateClient();

        var response = await ImportAsync(client, ReadRealExport("nr-orks.json"));

        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/LivePlay");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().NotContain("weapon-tag-unresolved\">LETHAL HITS: non-MONSTER/VEHICLE<")
            .And.NotContain("weapon-tag-unresolved\">CLEAVE 2<")
            .And.MatchRegex("weapon-tag weapon-tag-resolved\"[^>]*>LETHAL HITS: non-MONSTER/VEHICLE<")
            .And.MatchRegex("weapon-tag weapon-tag-resolved\"[^>]*>CLEAVE 2<");
    }

    [Fact]
    public async Task ImportingNrOrks_RendersWaaaghsConditionalInvulnerableSaveAndAssaultChip()
    {
        var client = factory.CreateClient();

        var response = await ImportAsync(client, ReadRealExport("nr-orks.json"));

        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain("stat-tile insv-tile provenance-tile provenance-cond")
            .And.MatchRegex("class=\"weapon-tag weapon-tag-cond\"[^>]*>Assault<");
    }
}
