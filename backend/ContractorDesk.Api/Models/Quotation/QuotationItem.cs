namespace ContractorDesk.Api.Models.Quotation;

public class QuotationItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public List<MeasurementRow>? Measurements { get; set; }

    /// <summary>Values for the document's custom columns, keyed by <c>CustomColumn.Id</c>.</summary>
    public Dictionary<string, string>? CustomValues { get; set; }

    /// <summary>
    /// Optional multiplier applied once to the sum of all measurement rows
    /// (e.g. unit weight in kg/m, to turn a running-meter total into a weight).
    /// Null means the measurement total is used as-is.
    /// </summary>
    public decimal? ConversionFactor { get; set; }

    /// <summary>
    /// Optional unit label for each measurement row's value and the running total
    /// (e.g. "R.Mt" for running-meter rows before a weight conversion is applied).
    /// </summary>
    public string? MeasurementUnit { get; set; }
}
