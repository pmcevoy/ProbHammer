using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Web.Rendering;

/// <summary>
/// Builds one popover trigger/panel pair for an ability name, a resolvable weapon-keyword chip, or
/// a nested [BRACKET] reference reached from either one's own text - extracted from
/// `_UnitBlock.cshtml`'s original `BuildRulePopover`/`NextPopoverId`/`RenderNestedReference` local
/// functions (design.md's "extract, don't duplicate" decision) so the new `_ArmyHeader.cshtml`
/// partial can render the identical trigger/panel behavior with no unit-block page index to scope
/// popover ids against. One instance per render pass, scoped by an explicit id-scope prefix
/// (<paramref name="idScopePrefix"/> - e.g. "u3" for unit block 3, "hdr" for the header) instead of
/// implicitly relying on a unit's page index the way the original closures did.
/// </summary>
public sealed class RulePopoverRenderer(RuleGlossary glossary, string idScopePrefix)
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
    /// Hits", "Anti", "Cleave" - an unguarded recursion here previously stack-overflowed).</summary>
    public (string Trigger, string Trailer) BuildRulePopover(
        string triggerHtml, string triggerClass, string ruleText, IReadOnlySet<string> shownRuleNames)
    {
        var popoverId = NextPopoverId();
        var (bodyInline, bodyPopovers) = RuleTextEmphasisRenderer.Render(ruleText, raw => RenderNestedReference(raw, shownRuleNames));
        var trigger = $"<button type=\"button\" id=\"t-{popoverId}\" class=\"{triggerClass}\" popovertarget=\"p-{popoverId}\" style=\"anchor-name: --a{popoverId}\">{triggerHtml}</button>";
        var panel = $"<div id=\"p-{popoverId}\" class=\"rule-popover\" popover=\"auto\" style=\"position-anchor: --a{popoverId}\"><div class=\"rule-popover-title\">{triggerHtml}</div><div class=\"rule-popover-text\">{bodyInline}</div></div>";
        return (trigger, panel + bodyPopovers);
    }

    // The bracket delegate RuleTextEmphasisRenderer.Render calls for every [BRACKET] token it
    // encounters mid-parse. Normalizes the token's raw text the same way extraction always has
    // before resolving it against this renderer's own glossary; an unresolved token, or one
    // resolving back to a rule already in shownRuleNames, degrades to plain (emphasis-rendered)
    // text with no trigger - never causing the surrounding text's own resolution to fail, and never
    // recursing into an already-open popover.
    private (string Inline, string Popovers) RenderNestedReference(string rawInner, IReadOnlySet<string> shownRuleNames)
    {
        var normalized = RuleTextTokenizer.Normalize(rawInner);
        var rule = glossary.TryResolve(normalized);
        var (labelInline, labelPopovers) = RuleTextEmphasisRenderer.Render(rawInner, raw => RenderNestedReference(raw, shownRuleNames));
        if (rule == null || shownRuleNames.Contains(rule.Name))
            return (labelInline, labelPopovers);

        var nextShownRuleNames = new HashSet<string>(shownRuleNames) { rule.Name };
        var (trigger, trailer) = BuildRulePopover(labelInline, "rule-reference", rule.Text, nextShownRuleNames);
        return (trigger, labelPopovers + trailer);
    }
}
