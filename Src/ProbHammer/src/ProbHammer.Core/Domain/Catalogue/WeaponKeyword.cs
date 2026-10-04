using System.Text.RegularExpressions;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>A weapon keyword split into a value-blind <see cref="Identity"/> and its trailing
/// <see cref="Value"/> (number, dice expression or <c>N+</c> threshold). Unlike
/// value-blind <see cref="Bsdata.RuleGlossary.TryResolve"/> lookup, an Anti keyword keeps its target and a
/// qualifier ("non-MONSTER/VEHICLE") stays part of the identity.</summary>
public sealed partial record WeaponKeyword(string Identity, string? Value, string Display)
{
    public static WeaponKeyword Parse(string text)
    {
        var display = text.Trim().Trim('[', ']').Trim();
        var (head, qualifier) = SplitQualifier(display);

        string? value = null;
        if (qualifier is not null)
            (qualifier, value) = SplitTrailingValue(qualifier);
        if (value is null)
            (head, value) = SplitTrailingValue(head);

        var identity = qualifier is null ? Fold(head) : $"{Fold(head)}:{Fold(qualifier)}";
        return new WeaponKeyword(identity, value?.ToLowerInvariant(), display);
    }

    public bool IsSameKeyword(WeaponKeyword other) => Identity == other.Identity;

    /// <summary>Positive when <paramref name="a"/>'s value is better than <paramref name="b"/>'s, zero
    /// when equal or either has no value. A threshold is better when lower, anything else when
    /// higher; dice compare by expected value.</summary>
    public static int Compare(WeaponKeyword a, WeaponKeyword b)
    {
        if (a.Value is null || b.Value is null)
            return 0;

        var isThreshold = a.Value.EndsWith('+') || b.Value.EndsWith('+');
        var comparison = Magnitude(a.Value).CompareTo(Magnitude(b.Value));
        return isThreshold ? -comparison : comparison;
    }

    private static double Magnitude(string value) =>
        DiceExpression.Parse(value.TrimEnd('+')).ExpectedValue();

    private static (string Head, string? Qualifier) SplitQualifier(string text)
    {
        var colon = text.IndexOf(':');
        return colon < 0 ? (text, null) : (text[..colon], text[(colon + 1)..]);
    }

    private static (string Remainder, string? Value) SplitTrailingValue(string text)
    {
        var match = TrailingValuePattern().Match(text);
        return match.Success ? (text[..match.Index], match.Groups["value"].Value) : (text, null);
    }

    private static string Fold(string text) => NonAlphanumericPattern().Replace(text.ToLowerInvariant(), "");

    [GeneratedRegex(@"\s+(?<value>\d*d\d+(?:\+\d+)?|\d+\+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingValuePattern();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphanumericPattern();
}
