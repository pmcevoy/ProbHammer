using System.Net;
using System.Text;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Web.Pages;

namespace ProbHammer.Web.Rendering;

/// <summary>
/// Builds one popover trigger/panel pair for an ability name, a resolvable weapon-keyword chip, or
/// a nested [BRACKET] reference reached from either one's own text - extracted from
/// `_UnitBlock.cshtml`'s original `BuildRulePopover`/`NextPopoverId`/`RenderNestedReference` local
/// functions so the `_ArmyHeader.cshtml`
/// partial can render the identical trigger/panel behavior with no unit-block page index to scope
/// popover ids against. One instance per render pass, scoped by an explicit id-scope prefix
/// (<paramref name="idScopePrefix"/> - e.g. "u3" for unit block 3, "hdr" for the header) instead of
/// implicitly relying on a unit's page index the way the original closures did.
/// <paramref name="applySection"/>, given an ability and its popover id, returns that ability's Apply
/// section HTML; only a unit block supplies one.
/// </summary>
public sealed class RulePopoverRenderer(
    RuleGlossary glossary,
    string idScopePrefix,
    Func<Ability, string, string>? applySection = null)
{
    private int _counter;

    private string NextPopoverId() => $"{idScopePrefix}-{_counter++}";

    /// <summary>Builds one trigger/popover pair: <c>Trigger</c> is already-safe inline HTML (a
    /// plain HtmlEncode'd name/tag for a top-level ability/chip trigger, or an emphasis-rendered
    /// bracket label for a nested reference); <c>Trailer</c> is the popover panel itself plus every
    /// panel any nested [BRACKET] reference inside the rule text produced, kept separate from
    /// <c>Trigger</c> so a caller can splice the trigger in place while emitting every panel as a
    /// flow-content sibling elsewhere - never nested inside the trigger's own emphasis wrapping.
    /// <paramref name="shownRuleNames"/> is every RuleDefinition.Name already displayed somewhere
    /// in this popover's own ancestor chain, threaded down so a nested reference can refuse to
    /// re-enter one already open (real BSData rules self-reference their own name, e.g. "Sustained
    /// Hits", "Anti", "Cleave" - an unguarded recursion here previously stack-overflowed).
    /// <paramref name="depth"/> is this popover's own visual nesting depth (0 for every top-level
    /// ability-name/weapon-chip trigger, N for a reference reached N levels deep) - a separate
    /// count from <paramref name="shownRuleNames"/>.Count on purpose: a top-level weapon-keyword
    /// chip's own call pre-seeds <paramref name="shownRuleNames"/> with that chip's own rule name
    /// (self-reference guard), which would otherwise make it indistinguishable from a genuinely
    /// nested popover if depth were derived from the set's size instead of tracked explicitly.
    /// Rendered as the panel's own `data-depth` attribute, which CSS uses to offset a nested
    /// popover from its parent's shared centered position. <paramref name="ability"/> is the ability
    /// whose text this is, if any, for its Apply section.</summary>
    public (string Trigger, string Trailer) BuildRulePopover(
        string triggerHtml, string triggerClass, string ruleText, IReadOnlySet<string> shownRuleNames, int depth = 0,
        Ability? ability = null)
    {
        var popoverId = NextPopoverId();
        var (bodyInline, bodyPopovers) =
            RuleTextEmphasisRenderer.Render(ruleText, raw => RenderNestedReference(raw, shownRuleNames, depth));
        var trigger =
            $"<button type=\"button\" id=\"t-{popoverId}\" class=\"{triggerClass}\" popovertarget=\"p-{popoverId}\">{triggerHtml}</button>";
        var closeButton =
            $"<button type=\"button\" class=\"rule-popover-close\" popovertarget=\"p-{popoverId}\" popovertargetaction=\"hide\" aria-label=\"Close\">&times;</button>";
        var panel =
            $"<div id=\"p-{popoverId}\" class=\"rule-popover\" popover=\"auto\" data-depth=\"{depth}\"><div class=\"rule-popover-title\">{triggerHtml}{closeButton}</div><div class=\"rule-popover-text\">{bodyInline}</div>{ApplySection(ability, popoverId)}</div>";
        return (trigger, panel + bodyPopovers);
    }

    /// <summary>Builds a highlighted value's trigger and its provenance panel: a title, the original
    /// value, one row per line (an ability line is a nested ability popover at depth 1, its panel
    /// emitted after the provenance panel) and the result. The original and result values carry
    /// <c>data-prov-original</c>/<c>data-prov-total</c> so live-play.js can update them.</summary>
    public (string Trigger, string Trailer) BuildProvenancePopover(
        string triggerHtml, string triggerClass, ValueProvenance provenance)
    {
        var popoverId = NextPopoverId();
        var nestedPanels = new StringBuilder();
        var body = new StringBuilder("<table class=\"provenance-table\">");
        body.Append($"<tr class=\"provenance-original\"><th>{Encode(provenance.OriginalLabel)}</th>" +
                    $"<td><span data-prov-original>{Encode(provenance.OriginalText)}</span></td></tr>");

        foreach (var line in provenance.Lines)
        {
            var labelHtml = Encode(line.Label);
            if (line.Source is { } source && !string.IsNullOrWhiteSpace(source.Text))
            {
                var (abilityTrigger, abilityTrailer) =
                    BuildRulePopover(labelHtml, "ability-name-line", source.Text, new HashSet<string>(), depth: 1,
                        ability: source);
                labelHtml = abilityTrigger;
                nestedPanels.Append(abilityTrailer);
            }

            var rowClass = line.InResult ? "provenance-line" : "provenance-line provenance-not-applied";
            body.Append($"<tr class=\"{rowClass}\"><td>{labelHtml}</td><td>{Encode(line.ChangeText)}</td></tr>");
            foreach (var note in line.Notes)
                body.Append($"<tr class=\"provenance-note\"><td colspan=\"2\">{Encode(note)}</td></tr>");
        }

        if (provenance.ResultLabel is { } resultLabel)
            body.Append($"<tr class=\"provenance-result\"><th>{Encode(resultLabel)}</th>" +
                        $"<td><span data-prov-total>{Encode(provenance.ResultText ?? "")}</span></td></tr>");
        body.Append("</table>");

        var trigger =
            $"<button type=\"button\" id=\"t-{popoverId}\" class=\"{triggerClass}\" popovertarget=\"p-{popoverId}\">{triggerHtml}</button>";
        var closeButton =
            $"<button type=\"button\" class=\"rule-popover-close\" popovertarget=\"p-{popoverId}\" popovertargetaction=\"hide\" aria-label=\"Close\">&times;</button>";
        var panel =
            $"<div id=\"p-{popoverId}\" class=\"rule-popover provenance-popover\" popover=\"auto\" data-depth=\"0\"><div class=\"rule-popover-title\">{Encode(provenance.Title)}{closeButton}</div><div class=\"provenance-body\">{body}</div></div>";
        return (trigger, panel + nestedPanels);
    }

    /// <summary>Builds a granted or not-added keyword chip and its panel: each granting ability (a
    /// nested ability popover at depth 1) with its note, above the keyword's rule text when
    /// <paramref name="rule"/> resolved. A trigger even with no rule, so the source stays reachable.</summary>
    public (string Trigger, string Trailer) BuildKeywordChipPopover(KeywordChip chip, string triggerClass,
        RuleDefinition? rule)
    {
        var popoverId = NextPopoverId();
        var nestedPanels = new StringBuilder();
        var rowClass = chip.Kind == ChipKind.NotAdded ? "provenance-line provenance-not-applied" : "provenance-line";
        var body = new StringBuilder("<table class=\"provenance-table\">");
        foreach (var (source, note) in chip.Sources)
        {
            var labelHtml = Encode(source.Origin == AbilityOrigin.Enhancement ? $"✦ {source.Name}" : source.Name);
            if (!string.IsNullOrWhiteSpace(source.Text))
            {
                var (abilityTrigger, abilityTrailer) =
                    BuildRulePopover(labelHtml, "ability-name-line", source.Text, new HashSet<string>(), depth: 1,
                        ability: source);
                labelHtml = abilityTrigger;
                nestedPanels.Append(abilityTrailer);
            }

            body.Append($"<tr class=\"{rowClass}\"><td colspan=\"2\">{labelHtml}</td></tr>");
            if (note.Length > 0)
                body.Append($"<tr class=\"provenance-note\"><td colspan=\"2\">{Encode(note)}</td></tr>");
        }
        body.Append("</table>");

        var ruleHtml = "";
        if (rule is not null)
        {
            var (bodyInline, bodyPopovers) = RuleTextEmphasisRenderer.Render(rule.Text,
                raw => RenderNestedReference(raw, new HashSet<string> { rule.Name }, 0));
            ruleHtml = $"<div class=\"rule-popover-text\">{bodyInline}</div>";
            nestedPanels.Append(bodyPopovers);
        }

        var textHtml = Encode(chip.Text);
        var trigger =
            $"<button type=\"button\" id=\"t-{popoverId}\" class=\"{triggerClass}\" popovertarget=\"p-{popoverId}\">{textHtml}</button>";
        var closeButton =
            $"<button type=\"button\" class=\"rule-popover-close\" popovertarget=\"p-{popoverId}\" popovertargetaction=\"hide\" aria-label=\"Close\">&times;</button>";
        var panel =
            $"<div id=\"p-{popoverId}\" class=\"rule-popover provenance-popover\" popover=\"auto\" data-depth=\"0\"><div class=\"rule-popover-title\">{textHtml}{closeButton}</div><div class=\"provenance-body\">{body}</div>{ruleHtml}</div>";
        return (trigger, panel + nestedPanels);
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private string ApplySection(Ability? ability, string popoverId) =>
        ability is null || applySection is null ? "" : applySection(ability, popoverId);

    // The bracket delegate RuleTextEmphasisRenderer.Render calls for every [BRACKET] token it
    // encounters mid-parse. Normalizes the token's raw text the same way extraction always has
    // before resolving it against this renderer's own glossary; an unresolved token, or one
    // resolving back to a rule already in shownRuleNames, degrades to plain (emphasis-rendered)
    // text with no trigger - never causing the surrounding text's own resolution to fail, and never
    // recursing into an already-open popover.
    private (string Inline, string Popovers) RenderNestedReference(string rawInner, IReadOnlySet<string> shownRuleNames,
        int depth)
    {
        var normalized = RuleTextTokenizer.Normalize(rawInner);
        var rule = glossary.TryResolve(normalized);
        var (labelInline, labelPopovers) =
            RuleTextEmphasisRenderer.Render(rawInner, raw => RenderNestedReference(raw, shownRuleNames, depth));
        if (rule == null || shownRuleNames.Contains(rule.Name))
            return (labelInline, labelPopovers);

        var nextShownRuleNames = new HashSet<string>(shownRuleNames) { rule.Name };
        var (trigger, trailer) =
            BuildRulePopover(labelInline, "rule-reference", rule.Text, nextShownRuleNames, depth + 1);
        return (trigger, labelPopovers + trailer);
    }
}