namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// One "known limitation" pattern a corpus scan checks its results against - a human-readable
/// description (used in failure output) paired with a predicate over a produced result. Kept as
/// a plain predicate rather than a JSON/config shape: a pattern like "an
/// AmbiguousCharacteristicException where Characteristic == InSv" needs to inspect a typed
/// value, not compare flat strings, and this project prefers no invented pattern language where
/// a C# predicate already says the same thing directly (see design.md).
/// </summary>
public sealed record AllowlistEntry<T>(string Description, Func<T, bool> Matches);
