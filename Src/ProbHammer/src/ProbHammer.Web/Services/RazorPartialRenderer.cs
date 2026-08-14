using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace ProbHammer.Web.Services;

/// <summary>Renders a Razor partial view to a string outside the normal page-rendering pipeline -
/// needed by the casualty sync endpoint (a Minimal API route, not a Razor Page), which reuses
/// LivePlay's existing `_UnitBlock.cshtml` rendering rather than duplicating any of its
/// view-shaping logic in the response it returns. Standard `IRazorViewEngine` + `ITempDataProvider`
/// + hand-built `ViewContext` pattern for rendering a partial view to a string from outside an MVC
/// action.</summary>
public interface IRazorPartialRenderer
{
    Task<string> RenderAsync<TModel>(HttpContext httpContext, string partialViewPath, TModel model);
}

public class RazorPartialRenderer(IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider) : IRazorPartialRenderer
{
    public async Task<string> RenderAsync<TModel>(HttpContext httpContext, string partialViewPath, TModel model)
    {
        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

        // A rooted app-relative path (leading "/") resolves directly, without needing an
        // executingFilePath anchor or Razor Pages' page-relative view-location expanders - the most
        // reliable way to locate a partial from outside an actual page execution.
        var viewResult = viewEngine.GetView(executingFilePath: null, viewPath: partialViewPath, isMainPage: false);
        if (!viewResult.Success)
            throw new InvalidOperationException($"Partial view '{partialViewPath}' not found.");

        await using var writer = new StringWriter();
        var viewData = new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };
        var tempData = new TempDataDictionary(httpContext, tempDataProvider);
        var viewContext = new ViewContext(actionContext, viewResult.View, viewData, tempData, writer, new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }
}
