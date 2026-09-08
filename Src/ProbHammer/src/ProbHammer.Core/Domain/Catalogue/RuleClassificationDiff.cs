using System.Text.Json;
using System.Text.Json.Nodes;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Whether a baselined text's freshly-computed classification matches what a human already
/// verified - see baseline-rule-effect-classifications design.md's "Per-field diff" decision.</summary>
public enum RuleClassificationBaselineStatus
{
    /// <summary>Every field matches the baseline (including any newly-added field computing to its
    /// own defined default) - collapses to a summary count, never reprinted.</summary>
    Unchanged,

    /// <summary>A field the baseline already recorded now computes a different value - always
    /// surfaced, the regression signal this whole mechanism exists to catch.</summary>
    Drift,

    /// <summary>No drift, but a field that didn't exist when the baseline entry was verified now
    /// computes to something other than its own defined default - surfaced once, as new information
    /// on an already-verified entry, not a request to re-verify the fields that didn't change.</summary>
    NewInformation
}

/// <summary>One top-level <see cref="RuleClassification"/> field (e.g. "target", "effects") whose
/// freshly-computed value didn't match the baseline. <see cref="IsSchemaGrowth"/> distinguishes real
/// drift (the field existed in the baseline and disagrees) from schema growth (the field didn't exist
/// in the baseline at all, compared instead against its own defined default).</summary>
public sealed record RuleClassificationFieldChange(
    string Field, JsonNode? BaselineValue, JsonNode? CurrentValue, bool IsSchemaGrowth);

public sealed record RuleClassificationDiffResult(
    RuleClassificationBaselineStatus Status, IReadOnlyList<RuleClassificationFieldChange> Changes);

/// <summary>Structural JSON diff between a baseline entry's stored classification and a
/// freshly-computed one - see design.md's "Per-field diff: structural JSON diff between two serialized
/// RuleClassification snapshots" decision. Reuses ordinary JSON structural comparison rather than a
/// hand-written field-by-field comparator specifically so a future field added to
/// <see cref="RuleTarget"/>/<see cref="CharacteristicEffect"/> needs no matching change here - only
/// that field's own "what's the boring default" definition, read directly off
/// <see cref="DefaultClassification"/>'s own serialization rather than a separate lookup table.</summary>
public static class RuleClassificationDiff
{
    /// <summary>Every field's own "boring default" (today: <c>Target = SelfRuleTarget</c>,
    /// <c>Effects = []</c> - the same default the report tool's own default-only bucket already uses)
    /// is read off this single default instance's serialization, rather than maintained as a separate
    /// per-field table - see design.md's "Per-field diff" decision.</summary>
    private static readonly RuleClassification DefaultClassification = new(new SelfRuleTarget());

    public static RuleClassificationDiffResult Compare(RuleClassification baseline, RuleClassification current)
    {
        var baselineNode = ToNode(baseline).AsObject();
        var currentNode = ToNode(current).AsObject();
        var defaultNode = ToNode(DefaultClassification).AsObject();

        var changes = new List<RuleClassificationFieldChange>();

        foreach (var (field, currentValue) in currentNode)
        {
            if (baselineNode.TryGetPropertyValue(field, out var baselineValue))
            {
                if (!JsonNode.DeepEquals(baselineValue, currentValue))
                    changes.Add(new RuleClassificationFieldChange(field, baselineValue, currentValue, IsSchemaGrowth: false));
            }
            else
            {
                defaultNode.TryGetPropertyValue(field, out var defaultValue);
                if (!JsonNode.DeepEquals(defaultValue, currentValue))
                    changes.Add(new RuleClassificationFieldChange(field, defaultValue, currentValue, IsSchemaGrowth: true));
            }
        }

        var status = changes.Count == 0
            ? RuleClassificationBaselineStatus.Unchanged
            : changes.Any(c => !c.IsSchemaGrowth)
                ? RuleClassificationBaselineStatus.Drift
                : RuleClassificationBaselineStatus.NewInformation;

        return new RuleClassificationDiffResult(status, changes);
    }

    private static JsonNode ToNode(RuleClassification classification) =>
        JsonSerializer.SerializeToNode(classification, RuleClassificationBaseline.Options)!;
}
