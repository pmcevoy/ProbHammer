using ProbHammer.Web.Pages;

namespace ProbHammer.Web.Services;

/// <summary>The casualty sync endpoint's request handler: rebuilds the roster with the posted
/// adjustment batch applied and returns rendered `_UnitBlock` fragments for whichever units the
/// batch actually addresses (see casualty-tracking's design.md - "every request carries the full
/// current map"). Purely a thin controller-style wrapper - all the real logic
/// (<see cref="LivePlayModel.RebuildRoster"/>, <see cref="LivePlayModel.BuildUnitBlock"/>) already
/// lives in <see cref="LivePlayModel"/>, matching this page's existing "page-layer logic lives in
/// LivePlayModel" convention.</summary>
public interface ILivePlayCasualtyService
{
    Task<IResult> SyncAsync(HttpContext ctx, List<CasualtyAdjustment> adjustments);
}

public class LivePlayCasualtyService(
    IRazorPartialRenderer renderer, ISessionArmyListStore sessionStore, IArmyRosterProvider rosterProvider)
    : ILivePlayCasualtyService
{
    public async Task<IResult> SyncAsync(HttpContext ctx, List<CasualtyAdjustment> adjustments)
    {
        // Distinct UnitIndexes referenced by the batch, not "units whose rendering actually
        // differs" - a coordinate that fails to resolve (see LivePlayModel.RebuildRoster) still
        // renders its unit's fragment, just identical to pristine; simpler than diffing, and an
        // empty batch (no stored adjustments) still yields an empty response either way.
        var unitIndexes = adjustments.Select(a => a.Coordinate.UnitIndex).Distinct().ToList();
        var parsedArmyList = sessionStore.Load(ctx.Session);
        if (unitIndexes.Count == 0 || parsedArmyList is null)
            return Results.Json(new Dictionary<int, string>());

        var armyRoster = rosterProvider.Build(parsedArmyList);
        var roster = LivePlayModel.RebuildRoster(armyRoster.Units, adjustments);
        var fragments = new Dictionary<int, string>();

        foreach (var unitIndex in unitIndexes)
        {
            if (unitIndex < 0 || unitIndex >= roster.Count)
                continue;

            var block = LivePlayModel.BuildUnitBlock(roster[unitIndex]);
            var html = await renderer.RenderAsync(ctx, "/Pages/Shared/_UnitBlock.cshtml", new UnitBlockRenderModel(unitIndex, block));
            fragments[unitIndex] = html;
        }

        return Results.Json(fragments);
    }
}
