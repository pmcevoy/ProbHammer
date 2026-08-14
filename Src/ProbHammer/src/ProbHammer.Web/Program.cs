using System.Text.Json;
using ProbHammer.Core.Catalogue;
using ProbHammer.Core.Contracts;
using ProbHammer.Core.Enrichment;
using ProbHammer.Core.Parsing;
using ProbHammer.Core.Simulation;
using ProbHammer.Web.Helpers;
using ProbHammer.Web.Pages;
using ProbHammer.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Named HTTP client — GitHub API requires a User-Agent header
builder.Services.AddHttpClient("github", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ProbHammer/1.0");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Resolve cache path, expanding ~ to home directory for local dev
var rawCachePath = builder.Configuration["Enricher:CachePath"] ?? "~/.probhammer/cache/";
var cachePath = rawCachePath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

builder.Services.AddSingleton<ICatalogueFetcher>(sp =>
    new CatalogueFetcher(
        sp.GetRequiredService<IHttpClientFactory>(),
        cachePath,
        sp.GetRequiredService<ILogger<CatalogueFetcher>>()));

builder.Services.AddSingleton<CatalogueStore>();
builder.Services.AddSingleton<ArmyListParser>();
builder.Services.AddSingleton<Enricher>();
builder.Services.AddSingleton<IDiceRoller, DiceRoller>();
builder.Services.AddSingleton<CombatSimulator>();
builder.Services.AddSingleton<SimulationAdapter>();
builder.Services.AddScoped<ISimulationService, SimulationService>();
builder.Services.AddSingleton<IRazorPartialRenderer, RazorPartialRenderer>();
builder.Services.AddScoped<ILivePlayCasualtyService, LivePlayCasualtyService>();

// Initialise catalogue store on application startup
builder.Services.AddHostedService<CatalogueStartupService>();

builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();

// Run Monte Carlo simulation
app.MapPost("/api/simulate", async (HttpContext ctx, SimulationRequest simReq, ISimulationService svc) => await svc.RunAsync(ctx, simReq));

// Sync /LivePlay casualty adjustments and return rendered fragments for the affected units
app.MapPost("/api/live-play/casualties", async (HttpContext ctx, List<CasualtyAdjustment> adjustments, ILivePlayCasualtyService svc) =>
    await svc.SyncAsync(ctx, adjustments));

// Re-download catalogues used in the current session
app.MapPost("/api/refresh-catalogues", async (HttpContext ctx, CatalogueStore store) =>
{
    await ctx.Session.LoadAsync();
    var json = ctx.Session.GetString("used_catalogue_ids");
    IEnumerable<string> ids = json is not null
        ? JsonSerializer.Deserialize<List<string>>(json) ?? []
        : [];
    await store.RefreshCataloguesAsync(ids);
    return Results.Ok(new { refreshed = true });
});

app.Run();

// Exposes the top-level-statement-generated Program class to WebApplicationFactory<Program> in
// ProbHammer.Tests' integration tests.
public partial class Program;
