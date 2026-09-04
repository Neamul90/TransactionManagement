using System.Text;

namespace TransactionManagement.Web.Extensions;

/// <summary>
/// Spells a monetary amount for the printed document, in the convention used on Bangladeshi
/// invoices: the whole part, the currency name, and the fractional part only when there is one.
/// <list type="bullet">
///   <item><description>2,345.50 → "Two Thousand Three Hundred Forty-Five Taka and Fifty Poysha"</description></item>
///   <item><description>150.00 → "One Hundred Fifty Taka"</description></item>
///   <item><description>0.75 → "Zero Taka and Seventy-Five Poysha"</description></item>
/// </list>
/// Presentation only: nothing in the application reads this value back.
/// </summary>
public static class AmountInWords
{
    private static readonly string[] Units =
    [
        "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen",
        "Eighteen", "Nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    ];

    private static readonly (long Value, string Name)[] Scales =
    [
        (1_000_000_000_000L, "Trillion"),
        (1_000_000_000L, "Billion"),
        (1_000_000L, "Million"),
        (1_000L, "Thousand")
    ];

    /// <param name="amount">The amount to spell. Rounded to two decimals away from zero.</param>
    /// <param name="currencyName">For example "Taka". Omitted when null or blank.</param>
    /// <param name="fractionName">For example "Poysha". Omitted when null or blank.</param>
    public static string Convert(decimal amount, string? currencyName = null, string? fractionName = null)
    {
        var isNegative = amount < 0m;
        var absolute = Math.Abs(decimal.Round(amount, 2, MidpointRounding.AwayFromZero));

        var whole = (long)decimal.Truncate(absolute);
        var fraction = (int)decimal.Round((absolute - whole) * 100m, MidpointRounding.AwayFromZero);

        var builder = new StringBuilder();

        if (isNegative)
        {
            builder.Append("Minus ");
        }

        builder.Append(WholeNumberToWords(whole));
        AppendName(builder, currencyName);

        // A zero fractional part is simply not mentioned -- "One Hundred Fifty Taka", never
        // "... and Zero Poysha".
        if (fraction > 0)
        {
            builder.Append(" and ").Append(WholeNumberToWords(fraction));
            AppendName(builder, fractionName);
        }

        return builder.ToString();
    }

    private static void AppendName(StringBuilder builder, string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            builder.Append(' ').Append(name.Trim());
        }
    }

    private static string WholeNumberToWords(long value)
    {
        if (value < 20)
        {
            return Units[value];
        }

        if (value < 100)
        {
            var remainder = value % 10;

            // Compound tens are hyphenated: "Forty-Five", not "Forty Five".
            return remainder == 0
                ? Tens[value / 10]
                : $"{Tens[value / 10]}-{Units[remainder]}";
        }

        if (value < 1_000)
        {
            var remainder = value % 100;

            return remainder == 0
                ? $"{Units[value / 100]} Hundred"
                : $"{Units[value / 100]} Hundred {WholeNumberToWords(remainder)}";
        }

        foreach (var (scaleValue, scaleName) in Scales)
        {
            if (value < scaleValue)
            {
                continue;
            }

            var count = value / scaleValue;
            var remainder = value % scaleValue;

            return remainder == 0
                ? $"{WholeNumberToWords(count)} {scaleName}"
                : $"{WholeNumberToWords(count)} {scaleName} {WholeNumberToWords(remainder)}";
        }

        return Units[0];
    }
}
