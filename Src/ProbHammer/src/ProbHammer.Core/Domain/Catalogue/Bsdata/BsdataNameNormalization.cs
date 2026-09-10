namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Normalizes typographic punctuation in export text to the plain-ASCII form BSData's own catalogue
/// names use, ahead of any <see cref="BsdataNameResolver"/>/<see cref="Datasheet"/> lookup (e.g. a
/// right single quotation mark in "Emperor's Champion" / "Reaver's blade" export text vs. BSData's
/// plain apostrophe). Only the one confirmed real case (a right single quotation mark standing in
/// for an apostrophe) is handled; any other mismatch is expected to surface as an ordinary
/// resolution failure, not silently swallowed here.
/// </summary>
public static class BsdataNameNormalization
{
    public static string Normalize(string text) => text.Replace('’', '\'');
}
