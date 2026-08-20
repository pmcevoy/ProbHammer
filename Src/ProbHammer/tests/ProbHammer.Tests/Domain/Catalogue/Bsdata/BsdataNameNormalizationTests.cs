using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class BsdataNameNormalizationTests
{
    [Fact]
    public void TypographicRightSingleQuotationMark_NormalizesToAsciiApostrophe()
    {
        BsdataNameNormalization.Normalize("Reaver’s blade").Should().Be("Reaver's blade");
    }

    [Fact]
    public void TextWithNoTypographicPunctuation_IsUnchanged()
    {
        BsdataNameNormalization.Normalize("Astartes chainsword").Should().Be("Astartes chainsword");
    }
}
