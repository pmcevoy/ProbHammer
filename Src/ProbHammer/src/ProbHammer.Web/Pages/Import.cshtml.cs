using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Import.BattleScribe;
using ProbHammer.Web.Services;

namespace ProbHammer.Web.Pages;

/// <summary>The paste/submit page tying the GW-app text parsing/enrichment pipeline and the
/// BattleScribe/NewRecruit JSON pipeline together. Format detection
/// (<see cref="BattleScribeRosterFormat.TryParse"/>) runs first: a payload recognized as a
/// BattleScribe roster export is routed to that pipeline; anything else falls through to the
/// existing GW-app text parser unchanged. Enrichment/mapping is run here too - not deferred
/// entirely to `/LivePlay` - so a resolution failure is caught and reported before anything is
/// committed to session: only a fully-successful parse+build ever calls Save, leaving a
/// previously-successful session import untouched.</summary>
// Validated in OnPostAsync instead, so a stale form re-renders with its text rather than a blank 400.
[IgnoreAntiforgeryToken]
public class ImportModel(
    IArmyListParser parser,
    IArmyRosterProvider rosterProvider,
    ISessionArmyListStore sessionStore,
    IPhaseTurnStore phaseTurnStore,
    IAntiforgery antiforgery)
    : PageModel
{
    [BindProperty]
    public string ExportText { get; set; } = "";

    public string? ErrorMessage { get; private set; }

    public bool HasCurrentList => sessionStore.Load(HttpContext.Session) is not null;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await antiforgery.IsRequestValidAsync(HttpContext))
        {
            ErrorMessage = "This page had expired - press Import again.";
            return Page();
        }

        try
        {
            var import = BattleScribeRosterFormat.TryParse(ExportText, out var roster)
                ? new BattleScribeArmyImport(roster!)
                : (StoredArmyImport)new TextArmyImport(parser.Parse(ExportText));

            rosterProvider.Build(import); // validate before committing to session
            sessionStore.Save(HttpContext.Session, import);
            phaseTurnStore.Clear(HttpContext.Session);
            return RedirectToPage("/LivePlay");
        }
        catch (Exception ex) when (IsExpectedImportFailure(ex))
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    private static bool IsExpectedImportFailure(Exception ex) =>
        ex is ArmyListParseException or BsdataFactionResolutionException or BsdataNameResolutionException
            or AmbiguousCharacteristicException or BattleScribeRosterParseException;
}
