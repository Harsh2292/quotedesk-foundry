namespace QuoteDesk.Data.Entities;

public class Enquiry
{
    public int Id { get; set; }

    /// <summary>"Paste" / "Email" / "WhatsApp" — a plain string, not the <c>EnquiryChannel</c> enum.
    /// That enum is owned by QuoteDesk.Intake and must never appear outside it (docs/SPEC.md §5);
    /// the adapter that persists an enquiry converts to and from this column.</summary>
    public required string Channel { get; set; }

    public required string SenderId { get; set; }
    public required string RawBody { get; set; }
    public required DateTimeOffset ReceivedAt { get; set; }

    /// <summary>Null when the sender matched no known customer — see docs/DOMAIN.md, "Unknown sender".</summary>
    public int? CustomerId { get; set; }

    public required string Status { get; set; }

    /// <summary>A photographed enquiry, as a <c>data:image/...;base64,...</c> URL (foundry-04). Stored
    /// because the image arrives on <c>POST /api/enquiries</c> but is read on the separate
    /// <c>/process</c> request, and a Retry must be able to read it again. Null for text-only enquiries.</summary>
    public string? ImageDataUrl { get; set; }

    public Customer? Customer { get; set; }
}
