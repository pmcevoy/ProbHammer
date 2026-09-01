using ProbHammer.Core.Domain.Roster;
using ProbHammer.Web.Pages;

namespace ProbHammer.Web.Services;

/// <summary>The casualty/status/phase-turn sync endpoint's request handler: rebuilds the roster
/// with the posted casualty and unit-status batches applied and returns rendered `_UnitBlock`
/// fragments for whichever units either batch addresses (see casualty-tracking's design.md -
/// "every request carries the full current map", extended by half-strength-and-battleshock-
/// indicators to the two new unit-status maps). Since live-play-phase-tracker, also persists a
/// posted `PhaseTurnAdjustment` and, when present, expands the affected-unit set to every unit in
/// the roster and reports the newly selected cell's own Forced-section set once, page-wide. Purely
/// a thin controller-style wrapper - all the real logic
/// (<see cref="LivePlayModel.RebuildRosterWithStatus"/>, <see cref="LivePlayModel.BuildUnitBlock"/>,
/// <see cref="LivePlayModel.ExpandedSections"/>/<see cref="LivePlayModel.ForcedSections"/>) already
/// lives in <see cref="LivePlayModel"/>, matching this page's existing "page-layer logic lives in
/// LivePlayModel" convention.</summary>
public interface ILivePlayCasualtyService
{
    Task<IResult> SyncAsync(HttpContext ctx, LivePlaySyncRequest request);
}

public class LivePlayCasualtyService(
    IRazorPartialRenderer renderer,
    ISessionArmyListStore sessionStore,
    IArmyRosterProvider rosterProvider,
    IPhaseTurnStore phaseTurnStore)
    : ILivePlayCasualtyService
{
    private static readonly LivePlaySyncResponse EmptyResponse = new([], []);

    public async Task<IResult> SyncAsync(HttpContext ctx, LivePlaySyncRequest request)
    {
        // Distinct UnitIndexes referenced by either batch, not "units whose rendering actually
        // differs" - a coordinate/index that fails to resolve (see
        // LivePlayModel.RebuildRosterWithStatus) still renders its unit's fragment, just identical
        // to pristine; simpler than diffing, and two empty batches still yield an empty response.
        var partialUnitIndexes = request.CasualtyAdjustments.Select(a => a.Coordinate.UnitIndex)
            .Concat(request.StatusAdjustments.Select(a => a.UnitIndex))
            .Distinct()
            .ToList();
        var import = sessionStore.Load(ctx.Session);
        if (import is null || (partialUnitIndexes.Count == 0 && request.PhaseTurnAdjustment is null))
            return Results.Json(EmptyResponse);

        // Saved before building/rendering below, so the rest of this request (and every later
        // request in the same session) reads the just-updated selection.
        if (request.PhaseTurnAdjustment is { } adjustment)
            phaseTurnStore.Save(ctx.Session, new PhaseTurnSelection(adjustment.Turn, adjustment.Phase));

        var result = rosterProvider.Build(import);

        // A phase/turn adjustment can change every unit's own disclosure state, not just the units
        // a casualty/status batch happened to also touch in the same request - see design.md
        // Decision 3.
        var unitIndexes = request.PhaseTurnAdjustment is not null
            ? Enumerable.Range(0, result.Roster.Units.Count).ToList()
            : partialUnitIndexes;

        var selection = phaseTurnStore.Load(ctx.Session) ?? PhaseTurnSelection.Default;
        var expandedSections = LivePlayModel.ExpandedSections(selection);
        var forcedSections = request.PhaseTurnAdjustment is not null
            ? LivePlayModel.ForcedSections(selection).Select(LivePlayModel.SectionName).ToList()
            : [];

        var roster = LivePlayModel.RebuildRosterWithStatus(
            result.Roster.Units, request.CasualtyAdjustments, request.StatusAdjustments);
        var fragments = new Dictionary<int, string>();

        foreach (var unitIndex in unitIndexes)
        {
            if (unitIndex < 0 || unitIndex >= roster.Count)
                continue;

            var (unit, view) = roster[unitIndex];
            var block = LivePlayModel.BuildUnitBlock(view, unit);
            var html = await renderer.RenderAsync(ctx, "/Pages/Shared/_UnitBlock.cshtml",
                new UnitBlockRenderModel(unitIndex, block, result.Glossary, expandedSections));
            fragments[unitIndex] = html;
        }

        return Results.Json(new LivePlaySyncResponse(fragments, forcedSections));
    }
}
