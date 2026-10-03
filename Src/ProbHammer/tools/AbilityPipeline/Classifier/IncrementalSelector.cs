namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>The incremental re-classification rule (spec.md's "Incremental re-classification"
/// requirement): a hash needs (re-)classification when it has no existing record, or the record's
/// own PromptVersion doesn't match the version about to run.
///
/// spec.md's requirement also names a third trigger - "its extracted text has changed since the
/// last classification" - worded as if that's an independent condition alongside a matching hash.
/// It isn't, and can't be: Hash is Core's <c>AbilityTextKey.Hash</c> - a SHA-256 digest of
/// this exact Text (see <c>AbilityCorpusAggregator</c>, in the Extractor project this project
/// deliberately doesn't reference) - so a genuine text change always produces a genuinely
/// different hash - the OLD hash simply stops appearing in a fresh corpus walk at all, which
/// already falls under the "no existing record for this hash" branch above. "Same hash, different
/// Text" cannot happen from ordinary pipeline operation (short of a SHA-256 collision).
///
/// The <see cref="ExtractionRecord"/>/<see cref="ClassificationRecord"/> Text-equality check below
/// still exists, but for a different, real reason: this is a hand-editable, file-based pipeline by
/// design (see design.md's "Files over a DB" trade-off, which explicitly flags "one hash appearing
/// in classifications.json but not ability-corpus.json" as a consistency risk this project accepts
/// checking via tooling rather than structurally). A person can edit classifications.json's own
/// Text field by hand - e.g. patching in a corrected transcription - without touching its Hash, and
/// now the two files disagree about what this hash's text actually is. This check is what surfaces
/// that drift and forces a re-classification against the corpus's own current, authoritative Text,
/// rather than what a stale/hand-edited classification record happens to say.</summary>
public static class IncrementalSelector
{
    public static IReadOnlyList<ExtractionRecord> SelectPending(
        IReadOnlyDictionary<string, ExtractionRecord> corpus,
        IReadOnlyDictionary<string, ClassificationRecord> existingClassifications,
        string currentPromptVersion) =>
        corpus.Values
            .Where(record => NeedsClassification(record, existingClassifications, currentPromptVersion))
            .OrderBy(record => record.Names.Count > 0 ? record.Names[0] : record.Hash, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool NeedsClassification(ExtractionRecord record,
        IReadOnlyDictionary<string, ClassificationRecord> existingClassifications, string currentPromptVersion)
    {
        if (!existingClassifications.TryGetValue(record.Hash, out var existing))
            return true;

        // Data-integrity drift check against hand-editing, not a "text evolved" trigger - see this
        // type's own doc comment.
        if (!string.Equals(existing.Text, record.Text, StringComparison.Ordinal))
            return true;

        return !string.Equals(existing.PromptVersion, currentPromptVersion, StringComparison.Ordinal);
    }
}