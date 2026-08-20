namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Normalizes typographic punctuation in export text to the plain-ASCII form BSData's own catalogue
/// names use, ahead of any <see cref="BsdataNameResolver"/>/<see cref="Datasheet"/> lookup - see
/// design.md's "Name normalization before BSData resolution" (the "Emperor's Champion" / "Reaver's
/// blade" gap flagged during <c>catalogue-json-ingestion</c>'s original smoke test). Only the one
/// confirmed real case (a right single quotation mark standing in for an apostrophe) is handled;
/// any other mismatch is expected to surface as an ordinary resolution failure, not silently
/// swallowed here.
/// </summary>
public static class BsdataNameNormalization
{
    public static string Normalize(string text) => text.Replace('’', '\'');
}
