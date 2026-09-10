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
public class ImportModel(IArmyListParser parser, IArmyRosterProvider rosterProvider, ISessionArmyListStore sessionStore)
    : PageModel
{
    [BindProperty]
    public string ExportText { get; set; } = "";

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        try
        {
            var import = BattleScribeRosterFormat.TryParse(ExportText, out var roster)
                ? new BattleScribeArmyImport(roster!)
                : (StoredArmyImport)new TextArmyImport(parser.Parse(ExportText));

            rosterProvider.Build(import); // validate before committing to session
            sessionStore.Save(HttpContext.Session, import);
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
