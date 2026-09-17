namespace QuoteDesk.Intake;

/// <summary>
/// Decides the initial status for a freshly ingested enquiry. Pure and shared by every adapter, so
/// the rule is written once rather than reimplemented per channel.
///
/// A blank body needs a human <b>unless</b> a readable image is attached (foundry-04): a photo of a
/// written list is something the Intake agent reads. An attachment with no content to read — a voice
/// note (audio is never transcribed, docs/SPEC.md §5) or one known only by its metadata — still needs
/// manual entry.
/// </summary>
public static class EnquiryStatusRule
{
    public static string Resolve(IncomingEnquiry enquiry)
    {
        ArgumentNullException.ThrowIfNull(enquiry);

        return string.IsNullOrWhiteSpace(enquiry.Body) && !enquiry.Attachments.Any(a => a.IsReadableImage)
            ? EnquiryStatus.NeedsManualEntry
            : EnquiryStatus.Pending;
    }
}
