namespace ContractorDesk.Api.Models.Common;

public class DocumentTotals
{
    public decimal Subtotal { get; set; }
    public decimal GstPercent { get; set; }
    public decimal CgstPercent { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstPercent { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal GstAmount { get; set; }
    public decimal GrandTotal { get; set; }
}
