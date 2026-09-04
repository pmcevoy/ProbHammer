using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class CharacteristicValueTests
{
    [Fact]
    public void Numeric_RoundTripsValue()
    {
        var value = new NumericCharacteristicValue(4);

        value.Value.Should().Be(4);
    }

    [Fact]
    public void Dice_RoundTripsValue()
    {
        var expression = DiceExpression.D6 + 6;
        var value = new DiceCharacteristicValue(expression);

        value.Value.Should().Be(expression);
    }

    [Fact]
    public void Symbolic_RoundTripsSymbol()
    {
        var value = new SymbolicCharacteristicValue("-");

        value.Symbol.Should().Be("-");
    }

    [Fact]
    public void ImplicitIntConversion_ProducesNumericCharacteristicValue()
    {
        CharacteristicValue value = 4;

        value.Should().BeOfType<NumericCharacteristicValue>();
        ((NumericCharacteristicValue)value).Value.Should().Be(4);
    }

    [Fact]
    public void ImplicitDiceExpressionConversion_ProducesDiceCharacteristicValue()
    {
        CharacteristicValue value = DiceExpression.D6 + 6;

        value.Should().BeOfType<DiceCharacteristicValue>();
        ((DiceCharacteristicValue)value).Value.Should().Be(DiceExpression.D6 + 6);
    }

    [Fact]
    public void DifferentKinds_AreStructurallyDistinct_EvenWithEquivalentValue()
    {
        CharacteristicValue numeric = new NumericCharacteristicValue(6);
        CharacteristicValue dice = new DiceCharacteristicValue(DiceExpression.Fixed(6));

        numeric.Should().NotBe(dice);
    }

    [Theory]
    [InlineData(4, "4")]
    public void Numeric_ToString_RendersPlainValue(int value, string expected)
    {
        new NumericCharacteristicValue(value).ToString().Should().Be(expected);
    }

    [Fact]
    public void Dice_ToString_RendersDiceExpressionNotation()
    {
        var value = new DiceCharacteristicValue(DiceExpression.D6 + 6);

        value.ToString().Should().Be("D6+6");
    }

    [Fact]
    public void Symbolic_ToString_RendersBareSymbol()
    {
        var value = new SymbolicCharacteristicValue("-");

        value.ToString().Should().Be("-");
    }
}