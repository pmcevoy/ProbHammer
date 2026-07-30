using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain;

public class DiceExpressionTests
{
    [Theory]
    [InlineData("1", 0, 0, 1)]
    [InlineData("3", 0, 0, 3)]
    [InlineData("D6", 1, 6, 0)]
    [InlineData("d6", 1, 6, 0)]
    [InlineData("2D6", 2, 6, 0)]
    [InlineData("D3", 1, 3, 0)]
    [InlineData("2D3+1", 2, 3, 1)]
    [InlineData("D6+2", 1, 6, 2)]
    public void Parse_ValidExpressions(string input, int count, int sides, int modifier)
    {
        var expr = DiceExpression.Parse(input);
        expr.Count.Should().Be(count);
        expr.Sides.Should().Be(sides);
        expr.Modifier.Should().Be(modifier);
    }

    [Fact]
    public void Parse_Invalid_Throws()
    {
        var act = () => DiceExpression.Parse("abc");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Fixed_CreatesFixedExpression()
    {
        var expr = DiceExpression.Fixed(4);
        expr.Count.Should().Be(0);
        expr.Modifier.Should().Be(4);
    }

    [Fact]
    public void Scale_Fixed_MultipliesModifier()
    {
        DiceExpression.Fixed(3).Scale(4).Should().Be(DiceExpression.Fixed(12));
    }

    [Fact]
    public void Scale_Dice_MultipliesCount()
    {
        var expr = DiceExpression.Parse("D6").Scale(3);
        expr.Count.Should().Be(3);
        expr.Sides.Should().Be(6);
        expr.Modifier.Should().Be(0);
    }

    [Fact]
    public void Scale_DiceWithModifier_ScalesCount()
    {
        // D6+1 × 2 → 2D6+2
        var expr = DiceExpression.Parse("D6+1").Scale(2);
        expr.Count.Should().Be(2);
        expr.Sides.Should().Be(6);
        expr.Modifier.Should().Be(2);
    }

    [Fact]
    public void Add_TwoFixed_SumsModifiers()
    {
        DiceExpression.Fixed(3).Add(DiceExpression.Fixed(5)).Should().Be(DiceExpression.Fixed(8));
    }

    [Fact]
    public void Add_FixedAndDice_PreservesCount()
    {
        var result = DiceExpression.Fixed(2).Add(DiceExpression.Parse("D6"));
        result.Count.Should().Be(1);
        result.Sides.Should().Be(6);
        result.Modifier.Should().Be(2);
    }

    [Fact]
    public void Add_TwoDiceWithSameSides_SumsCounts()
    {
        var result = DiceExpression.Parse("D6").Add(DiceExpression.Parse("2D6"));
        result.Count.Should().Be(3);
        result.Sides.Should().Be(6);
    }

    [Fact]
    public void Add_DiceWithDifferentSides_Throws()
    {
        var act = () => DiceExpression.Parse("D6").Add(DiceExpression.Parse("D3"));
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0, 0, 3, "3")]
    [InlineData(1, 6, 0, "D6")]
    [InlineData(2, 6, 0, "2D6")]
    [InlineData(1, 6, 2, "D6+2")]
    [InlineData(3, 3, 1, "3D3+1")]
    public void ToString_FormatsCorrectly(int count, int sides, int mod, string expected)
    {
        var expr = new DiceExpression { Count = count, Sides = sides, Modifier = mod };
        expr.ToString().Should().Be(expected);
    }

    // ─── new in this change ────────────────────────────────────────────────

    [Fact]
    public void ImplicitConversion_FromInt_ProducesFixedExpression()
    {
        DiceExpression expr = 5;
        expr.Should().Be(DiceExpression.Fixed(5));
    }

    [Fact]
    public void ImplicitConversion_AllowsPlainIntAtCallSite()
    {
        // Mirrors a fixed weapon stat construction site: no DiceExpression.Fixed(...) needed.
        DiceExpression Accepts(DiceExpression d) => d;
        Accepts(3).Should().Be(DiceExpression.Fixed(3));
    }

    [Fact]
    public void D6Preset_IsOneSixSidedDieNoModifier()
    {
        DiceExpression.D6.Count.Should().Be(1);
        DiceExpression.D6.Sides.Should().Be(6);
        DiceExpression.D6.Modifier.Should().Be(0);
    }

    [Fact]
    public void D3Preset_IsOneThreeSidedDieNoModifier()
    {
        DiceExpression.D3.Count.Should().Be(1);
        DiceExpression.D3.Sides.Should().Be(3);
        DiceExpression.D3.Modifier.Should().Be(0);
    }

    [Fact]
    public void PlusOperator_AddsModifier_MatchesParsedNotation()
    {
        (DiceExpression.D6 + 2).Should().Be(DiceExpression.Parse("D6+2"));
    }

    [Fact]
    public void PlusOperator_DoesNotMutateOriginal()
    {
        var original = DiceExpression.D6;
        _ = original + 2;
        original.Should().Be(DiceExpression.D6);
        original.Modifier.Should().Be(0);
    }
}
