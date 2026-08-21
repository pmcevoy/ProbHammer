namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// One BSData rule-text entry - a universal special rule (`sharedRules`, game-system level) or a
/// faction/library rule (`rules`, catalogue level) - carried through as opaque display text. See
/// rules-glossary's "Universal Rule Text Extraction"/"Faction and Library Rule Text Extraction".
/// </summary>
public sealed record RuleDefinition(string Name, IReadOnlyList<string> Aliases, string Text);
