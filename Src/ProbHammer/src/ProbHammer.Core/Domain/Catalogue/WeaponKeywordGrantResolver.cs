namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>The outcome of granting one keyword: <see cref="Keywords"/> after the grant, the
/// <see cref="Added"/> keyword when the grant changed anything (null otherwise), and the own keyword
/// it <see cref="Replaced"/>, if any.</summary>
public sealed record WeaponKeywordGrantResult(IReadOnlyList<string> Keywords, string? Added, string? Replaced)
{
    public bool Changed => Added is not null;
}

/// <summary>Resolves a granted keyword against a weapon's own keywords: native keywords win unless
/// the grant's value is strictly better (<see cref="WeaponKeyword.Compare"/>).</summary>
public static class WeaponKeywordGrantResolver
{
    public static WeaponKeywordGrantResult Apply(IReadOnlyList<string> keywords, string grantedKeyword)
    {
        var granted = WeaponKeyword.Parse(grantedKeyword);
        var index = keywords.ToList().FindIndex(k => WeaponKeyword.Parse(k).IsSameKeyword(granted));

        if (index < 0)
            return new WeaponKeywordGrantResult([.. keywords, granted.Display], granted.Display, null);

        if (WeaponKeyword.Compare(granted, WeaponKeyword.Parse(keywords[index])) <= 0)
            return new WeaponKeywordGrantResult(keywords, null, null);

        var replaced = keywords.ToList();
        replaced[index] = granted.Display;
        return new WeaponKeywordGrantResult(replaced, granted.Display, keywords[index]);
    }
}
