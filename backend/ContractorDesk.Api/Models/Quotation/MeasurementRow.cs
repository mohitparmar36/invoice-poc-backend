namespace ContractorDesk.Api.Models.Quotation;

public class MeasurementRow
{
    public string? Name { get; set; }
    public decimal No { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }

    /// <summary>
    /// Optional 4th multiplier (e.g. height/depth) for volume-style measurements.
    /// Null means the row is a plain No x Length x Width calculation.
    /// </summary>
    public decimal? Height { get; set; }
}
