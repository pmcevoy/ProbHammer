using ProbHammer.Web.Pages;

namespace ProbHammer.Web.Services;

/// <summary>The casualty/status sync endpoint's request handler: rebuilds the roster with the
/// posted casualty and unit-status batches applied and returns rendered `_UnitBlock` fragments for
/// whichever units either batch addresses (see casualty-tracking's design.md - "every request
/// carries the full current map", extended by half-strength-and-battleshock-indicators to the two
/// new unit-status maps). Purely a thin controller-style wrapper - all the real logic
/// (<see cref="LivePlayModel.RebuildRosterWithStatus"/>, <see cref="LivePlayModel.BuildUnitBlock"/>)
/// already lives in <see cref="LivePlayModel"/>, matching this page's existing "page-layer logic
/// lives in LivePlayModel" convention.</summary>
public interface ILivePlayCasualtyService
{
    Task<IResult> SyncAsync(HttpContext ctx, LivePlaySyncRequest request);
}

public class LivePlayCasualtyService(
    IRazorPartialRenderer renderer, ISessionArmyListStore sessionStore, IArmyRosterProvider rosterProvider)
    : ILivePlayCasualtyService
{
    public async Task<IResult> SyncAsync(HttpContext ctx, LivePlaySyncRequest request)
    {
        // Distinct UnitIndexes referenced by either batch, not "units whose rendering actually
        // differs" - a coordinate/index that fails to resolve (see
        // LivePlayModel.RebuildRosterWithStatus) still renders its unit's fragment, just identical
        // to pristine; simpler than diffing, and two empty batches still yield an empty response.
        var unitIndexes = request.CasualtyAdjustments.Select(a => a.Coordinate.UnitIndex)
            .Concat(request.StatusAdjustments.Select(a => a.UnitIndex))
            .Distinct()
            .ToList();
        var parsedArmyList = sessionStore.Load(ctx.Session);
        if (unitIndexes.Count == 0 || parsedArmyList is null)
            return Results.Json(new Dictionary<int, string>());

        var result = rosterProvider.Build(parsedArmyList);
        var roster = LivePlayModel.RebuildRosterWithStatus(
            result.Roster.Units, request.CasualtyAdjustments, request.StatusAdjustments);
        var fragments = new Dictionary<int, string>();

        foreach (var unitIndex in unitIndexes)
        {
            if (unitIndex < 0 || unitIndex >= roster.Count)
                continue;

            var (unit, view) = roster[unitIndex];
            var block = LivePlayModel.BuildUnitBlock(view, unit);
            var html = await renderer.RenderAsync(ctx, "/Pages/Shared/_UnitBlock.cshtml", new UnitBlockRenderModel(unitIndex, block, result.Glossary));
            fragments[unitIndex] = html;
        }

        return Results.Json(fragments);
    }
}
