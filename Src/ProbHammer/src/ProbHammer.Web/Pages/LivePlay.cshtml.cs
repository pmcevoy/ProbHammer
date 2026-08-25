using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Web.Services;

[assembly: InternalsVisibleTo("ProbHammer.Tests")]

namespace ProbHammer.Web.Pages;

public class LivePlayModel(ISessionArmyListStore sessionStore, IArmyRosterProvider rosterProvider) : PageModel
{
    public List<UnitBlockViewModel> Units { get; private set; } = [];

    // Consumed by _UnitBlock.cshtml (via UnitBlockRenderModel) to decide whether a weapon-keyword
    // chip or ability name is a resolvable rules-glossary reference - see live-play-view's
    // "Ability And Rule Text Popover"/rules-glossary's "Glossary Lookup By Normalized Name Or
    // Alias". Set alongside Units in OnGet so both come from the same ArmyRosterBuildResult.
    public RuleGlossary Glossary { get; private set; } = null!;

    // Per live-play-view's "Live Play Redirects Without An Active Import" requirement: a session
    // with no successfully imported army list has nothing to render, so it's sent to the import
    // page instead of rendering an empty/erroring page.
    public IActionResult OnGet()
    {
        var import = sessionStore.Load(HttpContext.Session);
        if (import is null)
            return RedirectToPage("/Import");

        var result = rosterProvider.Build(import);
        Units = BuildUnitBlocks(result.Roster);
        Glossary = result.Glossary;
        return Page();
    }

    // Factored out of OnGet() so its sort/aggregate/build-view-model pipeline is testable directly
    // against a hand-built ArmyRoster, without needing a real HttpContext.Session. Threads each
    // sorted unit alongside its own aggregate view (rather than discarding the unit once the view
    // is built) so BuildUnitBlock can read HalfStrengthResolution/IsBattleShocked off it.
    internal static List<UnitBlockViewModel> BuildUnitBlocks(ArmyRoster roster) =>
        SortRoster(roster.Units)
            .Select(unit => BuildUnitBlock(AttachedUnitAggregator.Build(unit), unit))
            .ToList();

    // Sorts the raw roster using the same criteria OnGet() has always applied to the built views
    // (IsAttachedUnit, initial total model count, Name) - factored out so a casualty-adjusted
    // rebuild (RebuildRoster) assigns each unit the same UnitIndex a pristine GET would, without
    // duplicating the sort key logic. Safe to sort by a pristine build's keys even when adjustments
    // are pending: none of the three keys depend on RemainingCount (see design.md's Risks section).
    internal static List<ICombatUnit> SortRoster(IEnumerable<ICombatUnit> roster) =>
        roster
            .Select(unit => (Unit: unit, View: AttachedUnitAggregator.Build(unit)))
            .OrderByDescending(x => x.View.IsAttachedUnit)
            .ThenByDescending(x => x.View.Statlines.Sum(s => s.InitialCount))
            .ThenBy(x => x.View.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Unit)
            .ToList();

    // Reuses SelectKey's (ComponentName, StatlineName, LoadoutIndex) convention, extended with a
    // UnitIndex qualifier - SelectKey alone is ambiguous across the whole page (it's scoped to one
    // unit block's own client-side selection Set), but a casualty coordinate is page-wide. Used both
    // as the data-* attribute value the casualty controls carry and as the localStorage map's key.
    internal static string CasualtyKey(int unitIndex, string componentName, string statlineName, int loadoutIndex) =>
        $"{unitIndex}::{SelectKey(componentName, statlineName, loadoutIndex)}";

    // No server-side counterpart to parse CasualtyKey back into its parts: the client (live-play.js)
    // POSTs already-structured JSON (a CasualtyAdjustment list), built directly from the same four
    // values it used to construct the key - CasualtyKey exists only for the DOM data-* attribute and
    // the localStorage map's key, both client-side-only concerns.

    // Rebuilds the roster from scratch (a fresh ArmyRoster re-enriched from the session's stored
    // ParsedArmyList - see design.md's "Session stores the intermediate, not the graph") and replays
    // every adjustment in the batch onto it before aggregating. The server holds no state between
    // requests, so the caller (the casualty sync endpoint) must always pass the *entire* current
    // adjustment set, not just the newest one - see design.md's "every request carries the full
    // current map" decision, added after this exact bug was caught mid-implementation. An adjustment
    // whose coordinate doesn't resolve to a real model-line (out-of-range UnitIndex, unknown
    // ComponentName/StatlineName/LoadoutIndex) is silently ignored rather than throwing - defensive
    // against stale localStorage from a roster shape that no longer matches (e.g. after a re-import).
    internal static List<AttachedUnitAggregateView> RebuildRoster(
        IReadOnlyList<ICombatUnit> units, IReadOnlyList<CasualtyAdjustment> adjustments) =>
        RebuildRosterWithStatus(units, adjustments, []).Select(x => x.View).ToList();

    // The half-strength/Battle-shocked-toggle counterpart to RebuildRoster - applies both a
    // casualty batch and a unit-status-toggle batch onto a freshly sorted roster before
    // aggregating, and (unlike RebuildRoster) returns each sorted unit alongside its built view so
    // the caller can read HalfStrengthResolution/IsBattleShocked off it via BuildUnitBlock. Kept as
    // a separate method rather than changing RebuildRoster's own signature/return type, since
    // several existing tests exercise RebuildRoster in isolation against casualty-only behavior.
    internal static List<(ICombatUnit Unit, AttachedUnitAggregateView View)> RebuildRosterWithStatus(
        IReadOnlyList<ICombatUnit> units,
        IReadOnlyList<CasualtyAdjustment> casualtyAdjustments,
        IReadOnlyList<UnitStatusAdjustment> statusAdjustments)
    {
        var sortedUnits = SortRoster(units);

        for (var unitIndex = 0; unitIndex < sortedUnits.Count; unitIndex++)
        {
            var unit = sortedUnits[unitIndex];
            foreach (var adjustment in casualtyAdjustments.Where(a => a.Coordinate.UnitIndex == unitIndex))
                ApplyAdjustment(unit, adjustment.Coordinate, adjustment.RemainingCount);

            var status = statusAdjustments.FirstOrDefault(a => a.UnitIndex == unitIndex);
            if (status is not null)
            {
                unit.IsHalfStrengthOverride = status.IsHalfStrength;
                unit.IsBattleShocked = status.IsBattleShocked;
            }
        }

        return sortedUnits.Select(unit => (unit, AttachedUnitAggregator.Build(unit))).ToList();
    }

    private static void ApplyAdjustment(ICombatUnit unit, CasualtyCoordinate coordinate, int remainingCount)
    {
        var component = unit.Components.FirstOrDefault(c =>
            string.Equals(c.Datasheet.Name, coordinate.ComponentName, StringComparison.OrdinalIgnoreCase));
        if (component is null)
            return;

        var lines = component.ModelLines
            .Where(ml => string.Equals(ml.StatlineName, coordinate.StatlineName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Mirrors AttachedUnitAggregator.LoadoutIndexOf's convention in reverse: -1 addresses a
        // statline with exactly one model-line; a non-negative index addresses that position within
        // the (unfiltered, StatlineName-matched) model-line list.
        var line = coordinate.LoadoutIndex < 0
            ? (lines.Count == 1 ? lines[0] : null)
            : (coordinate.LoadoutIndex >= 0 && coordinate.LoadoutIndex < lines.Count ? lines[coordinate.LoadoutIndex] : null);

        line?.SetRemainingCount(remainingCount);
    }

    // Computes IsSingleModelUnit/IsAtOrBelowHalfStrength/IsBattleShocked off the live unit (per
    // HalfStrengthResolution and ICombatUnit.IsBattleShocked) before delegating to the view-only
    // overload below - the production path (BuildUnitBlocks, the casualty-sync endpoint) always has
    // the unit on hand.
    internal static UnitBlockViewModel BuildUnitBlock(AttachedUnitAggregateView view, ICombatUnit unit)
    {
        var isSingleModelUnit = HalfStrengthResolution.StartingStrength(unit) == 1;
        var isAtOrBelowHalfStrength = isSingleModelUnit
            ? unit.IsHalfStrengthOverride
            : HalfStrengthResolution.IsAtOrBelowHalfStrength(unit);
        return BuildUnitBlock(view, isSingleModelUnit, isAtOrBelowHalfStrength, unit.IsBattleShocked);
    }

    // View-only overload, defaulting the three status fields to false/not-applicable - kept for the
    // several existing rendering-focused tests that construct an AttachedUnitAggregateView fixture
    // directly with no backing ICombatUnit to read status off.
    internal static UnitBlockViewModel BuildUnitBlock(
        AttachedUnitAggregateView view,
        bool isSingleModelUnit = false,
        bool isAtOrBelowHalfStrength = false,
        bool isBattleShocked = false)
    {
        // Loadouts is empty when exactly one ModelLine shares a statline name (the "no redundant
        // breakdown row" rule - see AggregateStatlineEntry), so Max(.,1) recovers the true
        // per-name ModelLine count in that case; summed across every entry, this gives the unit's
        // total ModelLine count even though BuildStatlines has already collapsed same-named lines
        // into one entry each. An AttachedUnit can never total 1 here (Bodyguard + >=1 Attached,
        // each with >=1 ModelLine, is >=2 by construction) - only a single-ModelLine plain Unit
        // (e.g. Impulsor) can.
        var totalModelLines = view.Statlines.Sum(s => Math.Max(s.Loadouts.Count, 1));
        var showsBreakdownTrigger = totalModelLines > 1;

        var loadoutLabels = BuildLoadoutLabelLookup(view.Statlines);

        var orderedWeapons = view.Weapons
            .OrderByDescending(w => w.TotalAttacks.ExpectedValue())
            .Select(w => new WeaponRowViewModel(w, BuildContributionBreakdown(w, loadoutLabels), showsBreakdownTrigger))
            .ToList();

        var statlineBlocks = GroupStatlines(view.Statlines, view.Abilities);
        var wholeUnitAbilitySpans = BuildWholeUnitAbilitySpans(statlineBlocks, view.Abilities);
        var (adjustedStatlineBlocks, componentAbilitySpans) = BuildComponentAbilitySpans(statlineBlocks, view.Abilities);

        return new(
            Name: view.Name,
            Statlines: adjustedStatlineBlocks,
            RangedWeapons: orderedWeapons.Where(w => w.Entry.Profile.Type == WeaponType.Ranged).ToList(),
            MeleeWeapons: orderedWeapons.Where(w => w.Entry.Profile.Type == WeaponType.Melee).ToList(),
            ComponentAbilitySpans: componentAbilitySpans,
            WholeUnitAbilitySpans: wholeUnitAbilitySpans,
            Keywords: view.Keywords.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList(),
            IsSingleModelUnit: isSingleModelUnit,
            IsAtOrBelowHalfStrength: isAtOrBelowHalfStrength,
            IsBattleShocked: isBattleShocked);
    }

    // Selection-key convention shared between the Statline section's toggle targets and weapon
    // contribution rows' filtering data, so both sides address the exact same loadout unambiguously.
    // A LoadoutIndex of -1 means "the whole statline" (single-ModelLine statlines have no
    // independently-addressable loadouts - see WeaponContribution.LoadoutIndex).
    internal static string SelectKey(string componentName, string statlineName, int loadoutIndex) =>
        loadoutIndex < 0 ? $"{componentName}::{statlineName}" : $"{componentName}::{statlineName}::{loadoutIndex}";

    // One label per (ComponentName, StatlineName, LoadoutIndex) across the whole unit, used by
    // weapon-breakdown raw rows. For a single-loadout statline (LoadoutIndex -1) the label is just
    // the StatlineName itself, matching the existing (unmodified) single-contributor breakdown-row
    // convention (e.g. "Neophyte", "Crusade Ancient"). For a loadout under a multi-loadout statline,
    // the label is "{StatlineName} w/ {compressed label}" (e.g. "Initiate w/ Astartes chainsword") -
    // in the Statline section the compressed label alone reads fine because it's visually nested
    // under its statline's own header line, but a weapon breakdown row has no such nesting (it's a
    // flat list under the weapon's name), so the compressed label alone there would read as if the
    // weapon itself were the contributor rather than a specific squad member. This lookup is only
    // consumed by BuildContributionBreakdown - GroupStatlines computes the Statline section's own
    // (unprefixed) loadout labels separately via CompressLoadoutLabels directly.
    internal static Dictionary<(string ComponentName, string StatlineName, int LoadoutIndex), string> BuildLoadoutLabelLookup(
        IReadOnlyList<AggregateStatlineEntry> statlines)
    {
        var lookup = new Dictionary<(string, string, int), string>();
        foreach (var entry in statlines)
        {
            if (entry.Loadouts.Count <= 1)
            {
                lookup[(entry.ComponentName, entry.StatlineName, -1)] = entry.StatlineName;
                continue;
            }

            var compressed = CompressLoadoutLabels(entry.Loadouts);
            for (var i = 0; i < entry.Loadouts.Count; i++)
                lookup[(entry.ComponentName, entry.StatlineName, i)] = $"{entry.StatlineName} w/ {compressed[i]}";
        }

        return lookup;
    }

    // Rendering-only grouping over data AttachedUnitAggregator already computed correctly (mirrors
    // GroupStatlines/StatlineBlockViewModel's precedent - the domain retains ungrouped, per-
    // ModelLine provenance; only the display grouping is page-layer work). Groups Contributions by
    // (ComponentName, StatlineName) in first-seen order. Every contribution always gets its own raw
    // row (SelectKey set, GroupKey shared with its siblings) - client-side filtering needs this
    // granularity regardless of whether the group visually merges. When a group's contributions all
    // share the same PerModelAttacks and there's more than one of them, an additional merged row
    // (SelectKey null) is emitted alongside the raw rows: the merged row is what renders by default
    // (matching the pre-selection-filtering shipped behavior exactly), and only gives way to its raw
    // siblings once the client detects the group's selection state has diverged - see live-play.js.
    // A group of exactly one contribution never gets a merged row (nothing to merge - its lone raw
    // row already is the single rendered row, matching the pre-existing "single contribution still
    // renders as one row" behavior). Label is StatlineName for a merged row (matches the validated
    // UI shape, and accepts the same known limitation as ICombatUnit.Name's duplicate-leader case:
    // two components sharing a statline name would render identically-labeled rows); a raw row's
    // Label is looked up per its own (ComponentName, StatlineName, LoadoutIndex) so a split-out row
    // shows its distinguishing-weapons loadout label rather than the bare statline name.
    internal static IReadOnlyList<WeaponContributionRow> BuildContributionBreakdown(
        AggregateWeaponEntry entry,
        IReadOnlyDictionary<(string ComponentName, string StatlineName, int LoadoutIndex), string> loadoutLabels)
    {
        var groups = new List<List<WeaponContribution>>();
        var groupIndex = new Dictionary<(string ComponentName, string StatlineName), List<WeaponContribution>>();
        foreach (var contribution in entry.Contributions)
        {
            var key = (contribution.ComponentName, contribution.StatlineName);
            if (!groupIndex.TryGetValue(key, out var group))
            {
                group = [];
                groupIndex[key] = group;
                groups.Add(group);
            }
            group.Add(contribution);
        }

        var rows = new List<WeaponContributionRow>();
        foreach (var group in groups)
        {
            var groupKey = $"{group[0].ComponentName}::{group[0].StatlineName}";
            var uniform = group.All(c => c.PerModelAttacks == group[0].PerModelAttacks);

            if (uniform && group.Count > 1)
            {
                var count = group.Sum(c => c.Count);
                rows.Add(new WeaponContributionRow(
                    Label: group[0].StatlineName,
                    Count: count,
                    PerModelAttacks: group[0].PerModelAttacks,
                    Subtotal: group[0].PerModelAttacks.Scale(count),
                    GroupKey: groupKey,
                    SelectKey: null));
            }

            foreach (var c in group)
            {
                var label = loadoutLabels.GetValueOrDefault((c.ComponentName, c.StatlineName, c.LoadoutIndex), c.StatlineName);
                rows.Add(new WeaponContributionRow(
                    Label: label,
                    Count: c.Count,
                    PerModelAttacks: c.PerModelAttacks,
                    Subtotal: c.PerModelAttacks.Scale(c.Count),
                    GroupKey: groupKey,
                    SelectKey: SelectKey(c.ComponentName, c.StatlineName, c.LoadoutIndex)));
            }
        }

        return rows;
    }

    // Single forward scan: a new run starts whenever ComponentName or Statline differs from the
    // running group's first entry (equivalently, from the immediately preceding entry, since a
    // group's entries always already match each other) - never a global by-value regroup, only
    // adjacency in the already-established component/declared-order sequence from BuildStatlines.
    // Row-bound (StatlineName != null) abilities attach to whichever run contains a matching
    // (ComponentName, StatlineName) entry, split into ModelAbilities/UnitAbilities by Scope.
    private static IReadOnlyList<StatlineBlockViewModel> GroupStatlines(
        IReadOnlyList<AggregateStatlineEntry> statlines, IReadOnlyList<AggregateAbilityEntry> abilities)
    {
        var groups = new List<List<AggregateStatlineEntry>>();

        foreach (var entry in statlines)
        {
            var currentGroup = groups.Count > 0 ? groups[^1] : null;
            if (currentGroup != null
                && currentGroup[0].ComponentName == entry.ComponentName
                && currentGroup[0].Statline == entry.Statline)
            {
                currentGroup.Add(entry);
            }
            else
            {
                groups.Add([entry]);
            }
        }

        return groups.Select(g => new StatlineBlockViewModel(
                Entries: g,
                LoadoutLabels: g.Select(entry => CompressLoadoutLabels(entry.Loadouts)).ToList(),
                ModelAbilities: RowBoundAbilities(g, abilities, AbilityScope.Model),
                UnitAbilities: RowBoundAbilities(g, abilities, AbilityScope.Unit)))
            .ToList();
    }

    // Multiset (bag) intersection across every loadout under one statline entry, then per-loadout
    // multiset subtraction - a loadout's compressed label is only the weapons that distinguish it
    // from its siblings. Must be a true multiset operation, not a common-prefix strip: shared
    // weapons need not appear at the same list position in each loadout, and a loadout carrying an
    // extra copy of an otherwise-shared weapon must keep that one extra copy visible. A statline
    // with exactly one loadout has nothing to compress against, so its full WeaponsLabel passes
    // through unchanged (matches the existing Loadouts.Count > 1 render guard in LivePlay.cshtml -
    // this function's result is only ever read when there's more than one loadout to distinguish).
    internal static IReadOnlyList<string> CompressLoadoutLabels(IReadOnlyList<ModelLineLoadout> loadouts)
    {
        if (loadouts.Count <= 1)
            return loadouts.Select(l => l.WeaponsLabel).ToList();

        var shared = CountWeapons(loadouts[0].Weapons);
        foreach (var loadout in loadouts.Skip(1))
        {
            var counts = CountWeapons(loadout.Weapons);
            foreach (var weapon in shared.Keys.ToList())
                shared[weapon] = Math.Min(shared[weapon], counts.GetValueOrDefault(weapon));
        }

        return loadouts.Select(loadout => string.Join(", ", DistinguishingWeapons(loadout.Weapons, shared))).ToList();
    }

    private static Dictionary<string, int> CountWeapons(IReadOnlyList<string> weapons)
    {
        var counts = new Dictionary<string, int>();
        foreach (var weapon in weapons)
            counts[weapon] = counts.GetValueOrDefault(weapon) + 1;
        return counts;
    }

    // Walks a loadout's own weapon list in order, consuming one shared-multiset copy per matching
    // weapon before falling back to yielding it as distinguishing - so only weapons beyond what's
    // actually shared across every sibling loadout are kept.
    private static IEnumerable<string> DistinguishingWeapons(IReadOnlyList<string> weapons, Dictionary<string, int> shared)
    {
        var remaining = new Dictionary<string, int>(shared);
        foreach (var weapon in weapons)
        {
            if (remaining.TryGetValue(weapon, out var count) && count > 0)
                remaining[weapon] = count - 1;
            else
                yield return weapon;
        }
    }

    private static IReadOnlyList<Ability> RowBoundAbilities(
        List<AggregateStatlineEntry> runEntries, IReadOnlyList<AggregateAbilityEntry> abilities, AbilityScope scope) =>
        abilities
            .Where(a => a.Ability.Scope == scope && a.StatlineName != null)
            .Where(a => runEntries.Any(e => e.ComponentName == a.ComponentName && e.StatlineName == a.StatlineName))
            .Select(a => a.Ability)
            .ToList();

    // Component-wide (Datasheet-sourced, StatlineName == null, ComponentName set) abilities span
    // every run belonging to their component - from the index of that component's first rendered
    // run through its last. A ComponentName-null entry (deduplicated Core Rule ability, see
    // BuildWholeUnitAbilitySpans) is excluded here - it belongs to no single component, so it has
    // no per-component run range to compute.
    //
    // When a component's span covers exactly one run (FirstRunIndex == LastRunIndex), that run's
    // own row-bound abilities (StatlineBlockViewModel.ModelAbilities/UnitAbilities) would
    // otherwise render at the identical grid coordinates as this span - two independently
    // positioned cells occupying the same area, one painting over the other. Found via a real
    // user-reported bug: the Impulsor's row-bound "Shield Dome" (its only statline row) was
    // invisible behind its own component-wide "Transport"/"Assault Vehicle"/etc cell. Fixed by
    // absorbing that single run's row-bound abilities into this span instead of rendering them
    // separately - returned alongside an adjusted copy of statlineBlocks with that run's own
    // ability lists cleared, so the caller's final Statlines never render the now-redundant
    // row-bound cell. A multi-row span is left alone - its grid-row range strictly contains, but
    // is never identical to, any one row-bound cell's range, so the two nest visually rather than
    // colliding.
    private static (IReadOnlyList<StatlineBlockViewModel> AdjustedBlocks, IReadOnlyList<ComponentAbilitySpanViewModel> Spans) BuildComponentAbilitySpans(
        IReadOnlyList<StatlineBlockViewModel> statlineBlocks, IReadOnlyList<AggregateAbilityEntry> abilities)
    {
        var spans = abilities
            .Where(a => a.StatlineName == null && a.ComponentName != null)
            .GroupBy(a => a.ComponentName)
            .Select(group =>
            {
                var runIndices = statlineBlocks
                    .Select((block, index) => (block, index))
                    .Where(x => x.block.Entries[0].ComponentName == group.Key)
                    .Select(x => x.index)
                    .ToList();

                var firstRun = runIndices.Min();
                var lastRun = runIndices.Max();
                var absorbsRowBound = firstRun == lastRun;
                var rowBoundModel = absorbsRowBound ? statlineBlocks[firstRun].ModelAbilities : [];
                var rowBoundUnit = absorbsRowBound ? statlineBlocks[firstRun].UnitAbilities : [];

                return new ComponentAbilitySpanViewModel(
                    FirstRunIndex: firstRun,
                    LastRunIndex: lastRun,
                    ModelAbilities: [.. rowBoundModel, .. group.Where(a => a.Ability.Scope == AbilityScope.Model).Select(a => a.Ability)],
                    UnitAbilities: [.. rowBoundUnit, .. group.Where(a => a.Ability.Scope == AbilityScope.Unit).Select(a => a.Ability)],
                    IsFullyDead: runIndices.All(i => statlineBlocks[i].IsFullyDead));
            })
            .ToList();

        var absorbedRunIndices = spans.Where(s => s.FirstRunIndex == s.LastRunIndex).Select(s => s.FirstRunIndex).ToHashSet();
        var adjustedBlocks = statlineBlocks
            .Select((block, index) => absorbedRunIndices.Contains(index) ? block with { ModelAbilities = [], UnitAbilities = [] } : block)
            .ToList();

        return (adjustedBlocks, spans);
    }

    // A ComponentName-null entry (see AttachedUnitAggregator.DedupeSharedCoreRuleAbilities)
    // belongs to no single component - grouped by Ability.Name (there can in principle be more
    // than one distinct deduplicated ability on the same unit) rather than by component, since
    // there's no per-component run range to key off. IsFullyDead reads the entry's own
    // ContributingComponentNames - true only once every run belonging to any of those components
    // is itself fully dead, matching the "hidden only once every contributing component is fully
    // dead" rule (distinct from ComponentAbilitySpanViewModel's own single-component rule).
    private static IReadOnlyList<WholeUnitAbilitySpanViewModel> BuildWholeUnitAbilitySpans(
        IReadOnlyList<StatlineBlockViewModel> statlineBlocks, IReadOnlyList<AggregateAbilityEntry> abilities) =>
        abilities
            .Where(a => a.ComponentName == null)
            .GroupBy(a => a.Ability.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var contributingComponents = group.First().ContributingComponentNames
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var isFullyDead = statlineBlocks
                    .Where(block => contributingComponents.Contains(block.Entries[0].ComponentName))
                    .All(block => block.IsFullyDead);

                return new WholeUnitAbilitySpanViewModel(
                    ModelAbilities: group.Where(a => a.Ability.Scope == AbilityScope.Model).Select(a => a.Ability).ToList(),
                    UnitAbilities: group.Where(a => a.Ability.Scope == AbilityScope.Unit).Select(a => a.Ability).ToList(),
                    IsFullyDead: isFullyDead);
            })
            .ToList();
}

/// <summary>Identifies one model-line - or one specific loadout within a multi-loadout statline
/// entry - across separate requests. <see cref="UnitIndex"/> is the unit's position in the sorted
/// roster (see <see cref="LivePlayModel.SortRoster"/>); <see cref="ComponentName"/>/
/// <see cref="StatlineName"/>/<see cref="LoadoutIndex"/> reuse <see cref="LivePlayModel.SelectKey"/>'s
/// convention unchanged (<c>-1</c> = the whole statline entry, valid only when it has exactly one
/// model-line).</summary>
public sealed record CasualtyCoordinate(int UnitIndex, string ComponentName, string StatlineName, int LoadoutIndex);

/// <summary>One entry in a casualty sync request: the absolute (not delta) remaining count a
/// specific model-line/loadout should be set to. The server always applies every adjustment in the
/// request's full batch onto a freshly-rebuilt pristine roster - see
/// <see cref="LivePlayModel.RebuildRoster"/>.</summary>
public sealed record CasualtyAdjustment(CasualtyCoordinate Coordinate, int RemainingCount);

/// <summary>One entry in a status-sync request: the absolute (not delta) half-strength-override
/// and Battle-shocked values a specific unit's <see cref="ICombatUnit"/> should be set to. Unlike
/// <see cref="CasualtyAdjustment"/>, addressed by <see cref="UnitIndex"/> alone - both statuses are
/// unit-level (see <see cref="HalfStrengthResolution"/>'s "combined across every component"
/// determination and <see cref="ICombatUnit.IsBattleShocked"/>), never per-component/statline/
/// loadout.</summary>
public sealed record UnitStatusAdjustment(int UnitIndex, bool IsHalfStrength, bool IsBattleShocked);

/// <summary>The casualty-sync endpoint's full request body - bundles a casualty batch and a
/// unit-status-toggle batch into one POST/one roster rebuild/one set of re-rendered fragments,
/// rather than two independent requests that could race each other's fragment swap. Either list
/// may be empty.</summary>
public sealed record LivePlaySyncRequest(
    List<CasualtyAdjustment> CasualtyAdjustments,
    List<UnitStatusAdjustment> StatusAdjustments);

/// <summary>A contiguous, same-component, value-identical run of statline entries sharing one
/// rendered stat-tile. <see cref="Entries"/> is never empty - every group is seeded with at least
/// one entry at creation. <see cref="LoadoutLabels"/> is parallel to <see cref="Entries"/> - each
/// element is that entry's own <c>Loadouts</c> compressed to their distinguishing weapons via
/// <see cref="LivePlayModel.CompressLoadoutLabels"/>, in the same order. <see cref="ModelAbilities"/>/
/// <see cref="UnitAbilities"/> are the row-bound (ModelLine-sourced) abilities matching this
/// specific run.</summary>
public sealed record StatlineBlockViewModel(
    IReadOnlyList<AggregateStatlineEntry> Entries,
    IReadOnlyList<IReadOnlyList<string>> LoadoutLabels,
    IReadOnlyList<Ability> ModelAbilities,
    IReadOnlyList<Ability> UnitAbilities)
{
    public Statline Statline => Entries[0].Statline;

    /// <summary>True once every entry in this run has a remaining count of 0 - the server-computed
    /// collapse trigger casualty-tracking adds alongside the existing client-only fully-deselected
    /// one (see casualty-tracking's design.md - Decisions). Computed from <see cref="Entries"/>'
    /// own summed <c>RemainingCount</c>, never stored independently.</summary>
    public bool IsFullyDead => Entries.All(e => e.RemainingCount == 0);
}

/// <summary>A Datasheet-sourced (component-wide) ability group, rendered once beside the first of
/// its component's rendered runs and visually spanning through the last. <see cref="IsFullyDead"/>
/// is true only once every run within [<see cref="FirstRunIndex"/>, <see cref="LastRunIndex"/>] is
/// itself fully dead - computed once in <see cref="LivePlayModel.BuildComponentAbilitySpans"/>
/// (which already has the full statline-block list in scope) rather than as a self-computed
/// property, since this record has no direct reference to the runs it spans.</summary>
public sealed record ComponentAbilitySpanViewModel(
    int FirstRunIndex,
    int LastRunIndex,
    IReadOnlyList<Ability> ModelAbilities,
    IReadOnlyList<Ability> UnitAbilities,
    bool IsFullyDead);

/// <summary>A deduplicated Core Rule ability shared by two or more present components of one
/// AttachedUnit (see AggregateAbilityEntry.ComponentName == null) - belongs to no single
/// component, rendered in its own dedicated grid row above every component's statline rows
/// (never spanning through them, unlike ComponentAbilitySpanViewModel). <see cref="IsFullyDead"/>
/// is true only once every one of the entry's own ContributingComponentNames has no present
/// statline block left - computed once in <see cref="LivePlayModel.BuildWholeUnitAbilitySpans"/>.
/// </summary>
public sealed record WholeUnitAbilitySpanViewModel(
    IReadOnlyList<Ability> ModelAbilities,
    IReadOnlyList<Ability> UnitAbilities,
    bool IsFullyDead);

/// <summary>One row of a weapon entry's contribution breakdown - either a merged display row
/// (<see cref="SelectKey"/> null, spans every raw contribution in <see cref="GroupKey"/>) or a raw
/// per-contribution row (<see cref="SelectKey"/> set - see <see cref="LivePlayModel.SelectKey"/>).
/// See <see cref="LivePlayModel.BuildContributionBreakdown"/> for the grouping rule. Subtotal is
/// always <c>PerModelAttacks.Scale(Count)</c> - safe to render standalone since every row's own
/// Count/PerModelAttacks are shown right beside it. Raw rows always exist (used by live-play.js for
/// selection-driven filtering/recompute regardless of whether a visible expand trigger exists for
/// this weapon - see <see cref="WeaponRowViewModel.ShowsBreakdownTrigger"/>).</summary>
public sealed record WeaponContributionRow(
    string Label, int Count, DiceExpression PerModelAttacks, DiceExpression Subtotal, string GroupKey, string? SelectKey)
{
    /// <summary>The plain integer value of <see cref="Subtotal"/> when it's a fixed (non-dice)
    /// expression, for live-play.js to sum without needing any <see cref="DiceExpression"/>
    /// semantics client-side; null when Subtotal is dice-based (e.g. a lone D6 contribution) - see
    /// design.md's Risks section for the accepted scope limit this implies for client-side
    /// recompute of a dice-valued contribution sharing a weapon with others.</summary>
    public int? SubtotalValue => Subtotal.Count == 0 ? Subtotal.Modifier : null;
}

/// <summary>A weapon entry plus its contribution breakdown (always at least one row - see
/// <see cref="LivePlayModel.BuildContributionBreakdown"/>). <see cref="ShowsBreakdownTrigger"/>
/// gates only the visible click-to-expand affordance (true when the unit has more than one
/// ModelLine in total); <see cref="Breakdown"/> itself is always populated regardless, since
/// live-play.js needs its rows' selection keys to filter/recompute this entry even when no user-
/// facing trigger exists for it.</summary>
public sealed record WeaponRowViewModel(
    AggregateWeaponEntry Entry, IReadOnlyList<WeaponContributionRow> Breakdown, bool ShowsBreakdownTrigger);

public sealed record UnitBlockViewModel(
    string Name,
    IReadOnlyList<StatlineBlockViewModel> Statlines,
    IReadOnlyList<WeaponRowViewModel> RangedWeapons,
    IReadOnlyList<WeaponRowViewModel> MeleeWeapons,
    IReadOnlyList<ComponentAbilitySpanViewModel> ComponentAbilitySpans,
    IReadOnlyList<WholeUnitAbilitySpanViewModel> WholeUnitAbilitySpans,
    IReadOnlyList<string> Keywords,
    bool IsSingleModelUnit,
    bool IsAtOrBelowHalfStrength,
    bool IsBattleShocked)
{
    /// <summary>True once at least one statline entry in this unit has taken a casualty
    /// (RemainingCount != InitialCount somewhere) - gates the "reset casualties" control's
    /// visibility, mirroring the Clear-filter button's own "only shown while active" precedent.
    /// A entry's RemainingCount is already summed across its own loadouts, so checking entries
    /// alone is sufficient - no need to drill into individual Loadouts.</summary>
    public bool HasCasualties => Statlines.Any(block => block.Entries.Any(e => e.RemainingCount != e.InitialCount));
}

/// <summary>Wraps a <see cref="UnitBlockViewModel"/> with its position in
/// <see cref="LivePlayModel.Units"/> for the <c>_UnitBlock</c> partial - needed for the weapon-row
/// <c>data-weapon-id</c> derivation (<c>"w-{UnitIndex}-r-{w}"</c>/<c>"w-{UnitIndex}-m-{w}"</c>) and,
/// from casualty-tracking onward, the casualty coordinate's unit-level qualifier.
/// <see cref="Glossary"/> travels alongside the view model (rather than being read off
/// <see cref="LivePlayModel"/> directly) so the casualty-sync endpoint's own fragment re-render
/// (<see cref="ProbHammer.Web.Services.LivePlayCasualtyService"/>, which renders
/// <c>_UnitBlock.cshtml</c> directly rather than through a full page request) can supply it too.
/// </summary>
public sealed record UnitBlockRenderModel(int UnitIndex, UnitBlockViewModel Unit, RuleGlossary Glossary);
