using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using ProbHammer.Core.Domain.Catalogue;
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

// The human-verified rule/ability -> characteristic-Effect vocabulary AttachedUnitAggregator
// consults at runtime (apply-rule-effect-baseline) - loaded once, mirrors BsdataCatalogueCache's own
// ContentRootPath-resolved root convention. A missing file loads as an empty baseline rather than
// throwing (RuleClassificationBaseline.Load's own contract).
var ruleClassificationBaselinePath = Path.Combine(
    builder.Environment.ContentRootPath,
    builder.Configuration["RuleEffectClassifications:FilePath"] ?? "Data/RuleEffectClassifications.json");
builder.Services.AddSingleton(RuleClassificationBaseline.Load(ruleClassificationBaselinePath));

builder.Services.AddSingleton<IArmyListParser, ArmyListParser>();
builder.Services.AddSingleton<IArmyRosterProvider, ArmyRosterProvider>();
builder.Services.AddSingleton<ISessionArmyListStore, SessionArmyListStore>();
builder.Services.AddSingleton<IPhaseTurnStore, PhaseTurnStore>();

builder.Services.AddSingleton<IRazorPartialRenderer, RazorPartialRenderer>();
builder.Services.AddScoped<ILivePlayCasualtyService, LivePlayCasualtyService>();

// Gcs:BucketName (Gcs__BucketName in Cloud Run - see terraform/cloud-run-iap.tf) is only set in
// the deployed environment. Local `docker compose up` leaves it unset, so it keeps the previous
// in-memory session/ephemeral-keys behaviour with no GCS credentials required.
var gcsBucketName = builder.Configuration["Gcs:BucketName"];
if (string.IsNullOrEmpty(gcsBucketName))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    var storageClient = StorageClient.Create();
    builder.Services.AddSingleton<IDistributedCache>(
        new GoogleCloudStorageDistributedCache(storageClient, gcsBucketName));
    builder.Services.AddDataProtection()
        .PersistKeysToGoogleCloudStorage(gcsBucketName, "dataprotection/keys.xml");
}

// The 7-day session lifetime only makes sense where the session data itself actually survives that
// long (the GCS-backed cache, gated the same way above) - local docker compose's in-memory cache
// dies with the process regardless, so it keeps ASP.NET Core's own 20-minute default rather than
// promising a week it can't deliver. IdleTimeout is largely inert even when set here -
// GoogleCloudStorageDistributedCache never reads DistributedCacheEntryOptions, so it doesn't gate
// whether a session's GCS-backed data is still readable (the bucket's own lifecycle rule does that -
// see cloud-run-iap.tf). What actually determines whether a returning player still has a session is
// the cookie itself: it defaults to a non-persistent browser-session cookie (gone once the browser
// fully closes), so Cookie.MaxAge is what actually carries it across a real week-long gap.
builder.Services.AddSession(options =>
{
    if (string.IsNullOrEmpty(gcsBucketName))
        return;

    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
});

builder.Services.AddRazorPages(options => options.Conventions.AddPageRoute("/Import", ""));

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
app.MapPost("/api/live-play/casualties",
    async (HttpContext ctx, LivePlaySyncRequest request, ILivePlayCasualtyService svc) =>
        await svc.SyncAsync(ctx, request));

app.Run();

// Exposes the top-level-statement-generated Program class to WebApplicationFactory<Program> in
// ProbHammer.Tests' integration tests.
public partial class Program;