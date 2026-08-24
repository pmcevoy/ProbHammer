using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// One BSData rule-text entry - a universal special rule (`sharedRules`, game-system level) or a
/// faction/library rule (`rules`, catalogue level) - carried through as opaque display text. See
/// rules-glossary's "Universal Rule Text Extraction"/"Faction and Library Rule Text Extraction".
/// </summary>
/// <param name="Modifiers">The source `BsRule`'s own gating modifiers (e.g. chapter/sub-faction
/// exclusivity) - read only by BsdataDatasheetMapper's Core Rule gating, since gating-derived
/// content decisions never belong on this project's per-page rendering; see
/// gate-and-dedupe-core-rule-abilities.</param>
public sealed record RuleDefinition(string Name, IReadOnlyList<string> Aliases, string Text, IReadOnlyList<BsModifier> Modifiers);
