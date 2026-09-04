using System.Globalization;
using System.Text;

namespace TransactionManagement.Web.Extensions;

/// <summary>
/// Renders a monetary amount as words for the printed document, e.g. 3,625.70 becomes
/// "Three thousand six hundred twenty five and 70/100". Presentation only: nothing in the
/// application reads this value back.
/// </summary>
public static class AmountInWords
{
    private static readonly string[] Units =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen",
        "eighteen", "nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"
    ];

    private static readonly (long Value, string Name)[] Scales =
    [
        (1_000_000_000_000L, "trillion"),
        (1_000_000_000L, "billion"),
        (1_000_000L, "million"),
        (1_000L, "thousand")
    ];

    public static string Convert(decimal amount, string? currencyName = null)
    {
        var isNegative = amount < 0m;
        var absolute = Math.Abs(decimal.Round(amount, 2, MidpointRounding.AwayFromZero));

        var whole = (long)decimal.Truncate(absolute);
        var fraction = (int)((absolute - whole) * 100m);

        var builder = new StringBuilder();

        if (isNegative)
        {
            builder.Append("minus ");
        }

        if (!string.IsNullOrWhiteSpace(currencyName))
        {
            builder.Append(currencyName).Append(' ');
        }

        builder.Append(WholeNumberToWords(whole));
        builder.Append(" and ");
        builder.Append(fraction.ToString("00", CultureInfo.InvariantCulture));
        builder.Append("/100");

        var words = builder.ToString();

        // Sentence case. Both arguments are strings so this binds to Concat(string, string);
        // passing a char plus a span would box the char and then fail to match any overload.
        return string.Concat(words[..1].ToUpperInvariant(), words[1..]);
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

            return remainder == 0
                ? Tens[value / 10]
                : $"{Tens[value / 10]} {Units[remainder]}";
        }

        if (value < 1_000)
        {
            var remainder = value % 100;

            return remainder == 0
                ? $"{Units[value / 100]} hundred"
                : $"{Units[value / 100]} hundred {WholeNumberToWords(remainder)}";
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

        return value.ToString(CultureInfo.InvariantCulture);
    }
}
