namespace FluentPdf.Samples.Invoice;

/// <summary>The print DTO an application hands to the invoice template.</summary>
public sealed record InvoiceDto(
    string Number,
    string CustomerName,
    IReadOnlyList<InvoiceLine> Lines)
{
    /// <summary>The grand total of every line.</summary>
    public decimal Total => Lines.Sum(line => line.Total);
}

/// <summary>A single billed line.</summary>
public sealed record InvoiceLine(string Description, int Quantity, decimal UnitPrice)
{
    /// <summary>The line total (quantity × unit price).</summary>
    public decimal Total => Quantity * UnitPrice;
}
