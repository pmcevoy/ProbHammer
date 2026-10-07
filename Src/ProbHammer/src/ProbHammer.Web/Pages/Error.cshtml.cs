using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProbHammer.Web.Pages;

/// <summary>UseExceptionHandler's target: shows what failed instead of an empty 500. Handles POST
/// too, since a failed POST (e.g. /Import) is re-executed here with its own method.</summary>
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
    public string? Message { get; private set; }
    public string Reference { get; private set; } = "";
    public DateTimeOffset Time { get; private set; }

    public void OnGet() => Describe();

    public void OnPost() => Describe();

    private void Describe()
    {
        Message = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error.Message;
        Reference = HttpContext.TraceIdentifier;
        Time = DateTimeOffset.UtcNow;
    }
}
