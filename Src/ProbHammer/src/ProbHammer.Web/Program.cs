using System.Text.Json;
using System.Text.Json.Serialization;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Web.Pages;
using ProbHammer.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// PhaseTurnAdjustment (live-play-phase-tracker) carries GameTurn/GamePhase enum fields over the
// /api/live-play/casualties POST body - live-play.js sends/reads them as their lowercase names
// ("mine", "command", ...), matching _PhaseTurnTracker.cshtml's own data-turn/data-phase attribute
// values, rather than the numeric default System.Text.Json would otherwise use.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

var bsdataRoot = Path.Combine(
    builder.Environment.ContentRootPath,
    builder.Configuration["Bsdata:RootDirectory"] ?? "BsData");
var bsdataSource = new LocalDiskBsdataCatalogueSource(bsdataRoot);

builder.Services.AddSingleton<IBsdataCatalogueSource>(bsdataSource);
builder.Services.AddSingleton(new BsdataCatalogueCache(bsdataSource));
builder.Services.AddSingleton<IArmyListParser, ArmyListParser>();
builder.Services.AddSingleton<IArmyRosterProvider, ArmyRosterProvider>();
builder.Services.AddSingleton<ISessionArmyListStore, SessionArmyListStore>();
builder.Services.AddSingleton<IPhaseTurnStore, PhaseTurnStore>();

builder.Services.AddSingleton<IRazorPartialRenderer, RazorPartialRenderer>();
builder.Services.AddScoped<ILivePlayCasualtyService, LivePlayCasualtyService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();

// Sync /LivePlay casualty adjustments and unit-status (half-strength/Battle-shocked) toggles,
// returning rendered fragments for the affected units.
app.MapPost("/api/live-play/casualties", async (HttpContext ctx, LivePlaySyncRequest request, ILivePlayCasualtyService svc) =>
    await svc.SyncAsync(ctx, request));

app.Run();

// Exposes the top-level-statement-generated Program class to WebApplicationFactory<Program> in
// ProbHammer.Tests' integration tests.
public partial class Program;
