using System.Net;
using System.Text;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Web.Pages;

namespace ProbHammer.Web.Rendering;

/// <summary>Builds an ability popover's Apply section: a switch per activatable condition and an
/// option list per choice group of that ability on one unit. Empty when the ability has none.</summary>
public static class ApplySectionRenderer
{
    public static string Render(IReadOnlyList<ActivatableCondition> conditions, int unitIndex, Ability ability,
        string popoverId)
    {
        var own = conditions
            .Where(c => string.Equals(c.Ability.Name, ability.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (own.Count == 0)
            return "";

        var html = new StringBuilder("<div class=\"apply-section\"><div class=\"apply-title\">Apply</div>");
        var target = $"data-unit-index=\"{unitIndex}\" data-ability=\"{Encode(ability.Name)}\"";
        foreach (var condition in own)
        {
            switch (condition)
            {
                case ConditionToggle toggle:
                    html.Append(Row("checkbox", "role=\"switch\" ",
                        $"{target} data-condition=\"{Encode(toggle.ConditionText)}\"", toggle.IsActive,
                        SwitchLabel(toggle), SwitchCaption(toggle)));
                    break;
                case ChoiceToggle choice:
                    html.Append(Choice(choice, target, $"apply-{popoverId}-g{choice.GroupIndex}"));
                    break;
            }
        }

        return html.Append("</div>").ToString();
    }

    private static string Choice(ChoiceToggle choice, string target, string radioName)
    {
        var isSingle = choice.Group.MaxSelect <= 1;
        var html = new StringBuilder(
            $"<div class=\"apply-choice\" role=\"{(isSingle ? "radiogroup" : "group")}\" data-max=\"{choice.Group.MaxSelect}\">");
        if (!string.IsNullOrWhiteSpace(choice.Group.ConditionText))
            html.Append($"<div class=\"apply-caption\">{Encode(choice.Group.ConditionText)}</div>");

        var groupTarget = $"{target} data-group=\"{choice.GroupIndex}\"";
        var type = isSingle ? "radio" : "checkbox";
        var name = isSingle ? $"name=\"{radioName}\" " : "";
        if (isSingle)
            html.Append(Row(type, name, $"{groupTarget} data-option=\"-1\"", choice.Selected.Count == 0, "None", null));
        for (var option = 0; option < choice.Group.Options.Count; option++)
            html.Append(Row(type, name, $"{groupTarget} data-option=\"{option}\"", choice.Selected.Contains(option),
                choice.Group.Options[option], null));

        return html.Append("</div>").ToString();
    }

    private static string Row(string type, string extraAttributes, string dataAttributes, bool isChecked, string label,
        string? caption) =>
        $"<label class=\"apply-row\"><input type=\"{type}\" {extraAttributes}class=\"apply-input\" {dataAttributes}" +
        $"{(isChecked ? " checked" : "")}><span class=\"apply-label\">{Encode(label)}</span>" +
        (caption is null ? "" : $"<span class=\"apply-caption\">{Encode(caption)}</span>") + "</label>";

    private static string SwitchLabel(ConditionToggle toggle) =>
        toggle.ConditionText.Length > 0 ? toggle.ConditionText
        : toggle.UsageLimit is { } limit ? ValueProvenanceBuilder.UsageLimitText(limit)
        : toggle.TurnOwnership is { } turn ? ValueProvenanceBuilder.TurnOwnershipText(turn)
        : "In effect";

    private static string? SwitchCaption(ConditionToggle toggle)
    {
        if (toggle.ConditionText.Length == 0)
            return null;

        var parts = new[]
        {
            toggle.UsageLimit is { } limit ? ValueProvenanceBuilder.UsageLimitText(limit) : null,
            toggle.TurnOwnership is { } turn ? ValueProvenanceBuilder.TurnOwnershipText(turn) : null
        }.OfType<string>().ToList();
        return parts.Count == 0 ? null : string.Join("; ", parts);
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
