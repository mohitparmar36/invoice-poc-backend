using ContractorDesk.Api.Models.Common;

namespace ContractorDesk.Api.Services.Calculations;

public interface ITotalsCalculator
{
    List<LineItemView> BuildLineItems(IEnumerable<(string Description, decimal Quantity, string Unit, decimal Rate)> items);

    DocumentTotals Calculate(IEnumerable<LineItemView> items, decimal gstPercent);
}

public class TotalsCalculator : ITotalsCalculator
{
    public List<LineItemView> BuildLineItems(IEnumerable<(string Description, decimal Quantity, string Unit, decimal Rate)> items)
    {
        return items.Select(i => new LineItemView
        {
            Description = i.Description,
            Quantity = i.Quantity,
            Unit = i.Unit,
            Rate = i.Rate,
            Amount = Round(i.Quantity * i.Rate)
        }).ToList();
    }

    public DocumentTotals Calculate(IEnumerable<LineItemView> items, decimal gstPercent)
    {
        var subtotal = Round(items.Sum(i => i.Amount));

        var halfGstPercent = gstPercent / 2m;
        var cgstAmount = Round(subtotal * halfGstPercent / 100m);
        var sgstAmount = Round(subtotal * halfGstPercent / 100m);
        var gstAmount = Round(cgstAmount + sgstAmount);
        var grandTotal = Round(subtotal + gstAmount);

        return new DocumentTotals
        {
            Subtotal = subtotal,
            GstPercent = gstPercent,
            CgstPercent = halfGstPercent,
            CgstAmount = cgstAmount,
            SgstPercent = halfGstPercent,
            SgstAmount = sgstAmount,
            GstAmount = gstAmount,
            GrandTotal = grandTotal
        };
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
