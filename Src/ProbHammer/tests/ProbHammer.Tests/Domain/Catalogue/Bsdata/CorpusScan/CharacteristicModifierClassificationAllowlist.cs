namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Known, expected reasons a real BSData modifier targeting a Statline characteristic ends up
/// unclassified by BsdataDatasheetMapper's characteristic-modifier-caveats classifier - see
/// CharacteristicModifierClassificationScanTests. Every occurrence the scan finds unclassified must
/// match one of these; a successfully-classified occurrence needs no entry here (see that test's
/// own doc comment for why).
/// </summary>
public static class CharacteristicModifierClassificationAllowlist
{
    // unify-characteristic-effect-resolution: InSv rejoined CharacteristicFieldIds, so a tier-1/
    // tier-2 InSv modifier now classifies successfully like any other recognized field - no longer
    // allowlisted here. A tier-3+ InSv modifier still falls through to the HasCondition entry below,
    // the same as any other characteristic's unrecognized condition shape.
    public const string InvulnerableSaveFieldId = "55a7-5b54-c60d-11dc";

    public static readonly IReadOnlyList<AllowlistEntry<ModifierOccurrence>> Entries =
    [
        new(
            "A condition shape the tier-1/tier-2 classifier doesn't recognize as locally-scoped to " +
            "its own granting entry (a sibling entry's id, live attachment state via 'associations', " +
            "or a self-reference scoped broader than 'self'/'parent', e.g. 'roster') - correctly left " +
            "unclassified (tier 3+, explicitly deferred per proposal.md). Real examples: Aeldari's " +
            "'Craftworld Warleader'/'Daemon Prince of Chaos' (sibling-selection conditions), Space " +
            "Marines' 'Ancient' ('associations' live-attachment condition), Astra Militarum's " +
            "'Deficiency' (self-referencing but 'roster'-scoped, Crusade-only content).",
            (ModifierOccurrence o) => o.HasCondition)
    ];
}