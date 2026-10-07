using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ProbHammer.Core.Domain.Import;
using static ProbHammer.Tests.Web.ImportTestHelper;

namespace ProbHammer.Tests.Web;

public class ErrorPageTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task AnUnexpectedImportFailure_InProduction_ShowsTheErrorPage_NotABlankResponse()
    {
        var parser = new Mock<IArmyListParser>();
        parser.Setup(p => p.Parse(It.IsAny<string>())).Throws(new InvalidOperationException("Parser exploded"));
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureTestServices(services => services.AddSingleton(parser.Object));
        }).CreateClient();

        var response = await ImportAsync(client, "any text");

        ((int)response.StatusCode).Should().Be(500);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain("Something went wrong").And.Contain("Parser exploded").And.Contain("Reference");
    }

    [Fact]
    public async Task ImportingAnExportWithNoDetachment_ReportsItOnTheImportPage()
    {
        var client = factory.CreateClient();

        var response = await ImportAsync(client, ReadRealExport("gw-tanks.txt"));

        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/Import");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain("import-error").And.Contain("No Detachment line found");
    }
}
