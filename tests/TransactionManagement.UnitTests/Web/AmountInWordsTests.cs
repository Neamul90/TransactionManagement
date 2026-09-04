using FluentAssertions;
using TransactionManagement.Web.Extensions;
using Xunit;

namespace TransactionManagement.UnitTests.Web;

/// <summary>
/// The amount in words is printed on the transaction document and on the RDLC report, where a
/// wrong or oddly worded figure is the kind of defect a customer notices before anyone else does.
/// </summary>
public sealed class AmountInWordsTests
{
    private const string Taka = "Taka";
    private const string Poysha = "Poysha";

    [Theory]
    [InlineData(2345.50, "Two Thousand Three Hundred Forty-Five Taka and Fifty Poysha")]
    [InlineData(150.00, "One Hundred Fifty Taka")]
    [InlineData(20.85, "Twenty Taka and Eighty-Five Poysha")]
    [InlineData(0.75, "Zero Taka and Seventy-Five Poysha")]
    [InlineData(0.00, "Zero Taka")]
    [InlineData(1.01, "One Taka and One Poysha")]
    public void Convert_SpellsTheAmountInTheExpectedForm(decimal amount, string expected) =>
        AmountInWords.Convert(amount, Taka, Poysha).Should().Be(expected);

    [Theory]
    [InlineData(45, "Forty-Five Taka")]
    [InlineData(21, "Twenty-One Taka")]
    [InlineData(70, "Seventy Taka")]
    public void Convert_HyphenatesCompoundTens(decimal amount, string expected) =>
        AmountInWords.Convert(amount, Taka, Poysha).Should().Be(expected);

    [Theory]
    [InlineData(100, "One Hundred Taka")]
    [InlineData(345, "Three Hundred Forty-Five Taka")]
    [InlineData(1_000, "One Thousand Taka")]
    [InlineData(1_000_000, "One Million Taka")]
    [InlineData(1_234_567, "One Million Two Hundred Thirty-Four Thousand Five Hundred Sixty-Seven Taka")]
    public void Convert_HandlesEachScale(decimal amount, string expected) =>
        AmountInWords.Convert(amount, Taka, Poysha).Should().Be(expected);

    [Fact]
    public void Convert_OmitsTheFractionEntirelyWhenThereIsNone() =>
        AmountInWords.Convert(150.00m, Taka, Poysha)
            .Should().NotContain("and")
            .And.NotContain("Zero Poysha");

    [Fact]
    public void Convert_RoundsToTwoDecimalsAwayFromZero() =>
        AmountInWords.Convert(2345.505m, Taka, Poysha)
            .Should().Be("Two Thousand Three Hundred Forty-Five Taka and Fifty-One Poysha");

    [Fact]
    public void Convert_PrefixesNegativeAmounts() =>
        AmountInWords.Convert(-5.25m, Taka, Poysha)
            .Should().Be("Minus Five Taka and Twenty-Five Poysha");

    [Fact]
    public void Convert_WithoutCurrencyNames_SpellsOnlyTheNumber() =>
        AmountInWords.Convert(45.50m).Should().Be("Forty-Five and Fifty");
}
