using ProbHammer.Web.Pages;
using ProbHammer.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRazorPartialRenderer, RazorPartialRenderer>();
builder.Services.AddScoped<ILivePlayCasualtyService, LivePlayCasualtyService>();

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// Sync /LivePlay casualty adjustments and return rendered fragments for the affected units
app.MapPost("/api/live-play/casualties", async (HttpContext ctx, List<CasualtyAdjustment> adjustments, ILivePlayCasualtyService svc) =>
    await svc.SyncAsync(ctx, adjustments));

app.Run();

// Exposes the top-level-statement-generated Program class to WebApplicationFactory<Program> in
// ProbHammer.Tests' integration tests.
public partial class Program;
