public class BillingHistoryItemDto
{
    public string InvoiceId { get; set; }
    public DateTime PaidAt { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }
    // URL to download the invoice PDF
    public string InvoicePdfUrl { get; set; }
}