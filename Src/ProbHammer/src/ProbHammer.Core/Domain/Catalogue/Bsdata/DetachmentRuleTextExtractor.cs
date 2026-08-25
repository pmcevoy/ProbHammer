using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Extracts zero or more (Name, Text) rule pairs from an already-resolved Detachment
/// <see cref="BsSelectionEntry"/> (see army-roster-enrichment's Detachment Name Resolution for how
/// that entry is located in the first place) - the two real shapes confirmed in the BSData corpus:
/// a rule declared locally on the entry itself (<see cref="BsSelectionEntry.Rules"/>), and a rule
/// reached via a "type": "rule" infoLink resolved against the closure's own <see cref="RuleGlossary"/>
/// - the same glossary lookup <see cref="BsdataDatasheetMapper"/>'s Core Rule Ability Extraction
/// already uses. Deliberately does NOT reuse that extraction's "nested inside a type: upgrade
/// entry" ancestry guard - that guard exists specifically to exclude a weapon's own keyword
/// cross-reference from leaking into a datasheet's abilities; a Detachment entry's own direct
/// infoLinks carry no equivalent ambiguity (see design.md).
/// </summary>
public static class DetachmentRuleTextExtractor
{
    public static IReadOnlyList<(string Name, string Text)> Extract(BsSelectionEntry detachmentEntry, RuleGlossary glossary)
    {
        var pairs = new List<(string Name, string Text)>();

        foreach (var rule in detachmentEntry.Rules)
            pairs.Add((rule.Name, rule.Description));

        foreach (var link in detachmentEntry.InfoLinks.Where(l => l.Type == "rule"))
        {
            var resolved = glossary.TryResolve(link.Name);
            if (resolved is not null)
                pairs.Add((resolved.Name, resolved.Text));
        }

        return pairs;
    }
}
