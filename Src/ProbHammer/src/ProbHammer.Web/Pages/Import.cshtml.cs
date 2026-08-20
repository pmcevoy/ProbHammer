using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Web.Services;

namespace ProbHammer.Web.Pages;

/// <summary>The paste/submit page tying army-list-parsing and army-roster-enrichment together (see
/// army-list-import's Import Submission requirement). Enrichment is run here too - not deferred
/// entirely to `/LivePlay` - so a resolution failure is caught and reported before anything is
/// committed to session (see Import Failure Reporting's "leaves a previously-successful session
/// import untouched" scenario: only a fully-successful parse+enrich ever calls Save).</summary>
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
            var parsedArmyList = parser.Parse(ExportText);
            rosterProvider.Build(parsedArmyList); // validate before committing to session
            sessionStore.Save(HttpContext.Session, parsedArmyList);
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
            or AmbiguousCharacteristicException;
}
