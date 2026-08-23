using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Web.Pages;
using ProbHammer.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var bsdataRoot = Path.Combine(
    builder.Environment.ContentRootPath,
    builder.Configuration["Bsdata:RootDirectory"] ?? "BsData");
var bsdataSource = new LocalDiskBsdataCatalogueSource(bsdataRoot);

builder.Services.AddSingleton<IBsdataCatalogueSource>(bsdataSource);
builder.Services.AddSingleton(new BsdataCatalogueCache(bsdataSource));
builder.Services.AddSingleton<IArmyListParser, ArmyListParser>();
builder.Services.AddSingleton<IArmyRosterProvider, ArmyRosterProvider>();
builder.Services.AddSingleton<ISessionArmyListStore, SessionArmyListStore>();

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
