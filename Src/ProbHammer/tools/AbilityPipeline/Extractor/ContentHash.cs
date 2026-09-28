using System.Security.Cryptography;
using System.Text;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tools.AbilityPipeline.Extractor;

/// <summary>Stable hash over a <see cref="RuleEffectClassifier.Normalize"/>d text - identical
/// input always produces an identical hash, across runs and machines, which is what lets
/// <c>ability-corpus.json</c> and <c>classifications.json</c> key on it.</summary>
public static class ContentHash
{
    public static string Of(string normalizedText)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedText));
        return Convert.ToHexStringLower(bytes);
    }
}
