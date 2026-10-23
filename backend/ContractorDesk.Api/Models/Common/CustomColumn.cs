namespace ContractorDesk.Api.Models.Common;

/// <summary>
/// An optional, user-named text column in the line items table (e.g. "HSN/SAC", "Brand").
/// Values live on each item's <c>CustomValues</c>, keyed by <see cref="Id"/>. Informational only — never affects amounts.
/// </summary>
public class CustomColumn
{
    public const int MaxPerDocument = 3;
    public const int MaxNameLength = 40;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
