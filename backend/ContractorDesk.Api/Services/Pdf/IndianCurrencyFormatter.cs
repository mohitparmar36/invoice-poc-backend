using System.Globalization;
using System.Text;

namespace ContractorDesk.Api.Services.Pdf;

public static class IndianCurrencyFormatter
{
    public static string Format(decimal amount)
    {
        var negative = amount < 0;
        amount = Math.Abs(amount);

        var fixedValue = amount.ToString("F2", CultureInfo.InvariantCulture);
        var parts = fixedValue.Split('.');
        var integerPart = parts[0];
        var decimalPart = parts[1];

        string formattedInteger;
        if (integerPart.Length <= 3)
        {
            formattedInteger = integerPart;
        }
        else
        {
            var lastThree = integerPart[^3..];
            var remaining = integerPart[..^3];
            var groups = new List<string>();

            while (remaining.Length > 2)
            {
                groups.Insert(0, remaining[^2..]);
                remaining = remaining[..^2];
            }

            if (remaining.Length > 0)
            {
                groups.Insert(0, remaining);
            }

            formattedInteger = string.Join(",", groups) + "," + lastThree;
        }

        var sb = new StringBuilder();
        if (negative) sb.Append('-');
        sb.Append('₹');
        sb.Append(formattedInteger);
        sb.Append('.');
        sb.Append(decimalPart);
        return sb.ToString();
    }
}
