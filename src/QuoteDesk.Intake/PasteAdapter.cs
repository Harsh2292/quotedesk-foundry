using QuoteDesk.Data;
using QuoteDesk.Data.Repositories;

namespace QuoteDesk.Intake;

/// <summary>
/// The one adapter that always works — a UI textarea, no external system involved. Every other
/// channel is built and risk-managed against this one (docs/SPEC.md §5).
/// </summary>
public sealed class PasteAdapter(IEnquiryRepository enquiries) : IEnquiryIntakeAdapter
{
    public EnquiryChannel Channel => EnquiryChannel.Paste;

    /// <summary>Builds the channel-agnostic shape from raw pasted text — trims surrounding
    /// whitespace and normalises line endings, but otherwise preserves the text verbatim so
    /// downstream extraction sees exactly what the customer sent.</summary>
    public static IncomingEnquiry FromPastedText(string senderId, string body, DateTimeOffset receivedAt) =>
        new()
        {
            Channel = EnquiryChannel.Paste,
            SenderId = senderId,
            Body = NormalizeLineEndings(body).Trim(),
            ReceivedAt = receivedAt,
        };

    /// <summary>A pasted enquiry with a photo — the body may be blank when the photo is the whole
    /// enquiry. The caller validates the image first with <see cref="PastedImage.TryParse"/> to shape a
    /// 400; an invalid one reaching here is a programming error and throws.</summary>
    public static IncomingEnquiry FromPastedTextAndImage(
        string senderId, string body, string imageDataUrl, DateTimeOffset receivedAt)
    {
        if (!PastedImage.TryParse(imageDataUrl, out var image, out var error))
        {
            throw new ArgumentException(error, nameof(imageDataUrl));
        }

        return FromPastedText(senderId, body, receivedAt) with { Attachments = [image] };
    }

    public async Task<EnquiryIntakeResult> IngestAsync(IncomingEnquiry enquiry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(enquiry);

        var status = EnquiryStatusRule.Resolve(enquiry);
        var image = enquiry.Attachments.FirstOrDefault(a => a.IsReadableImage);

        var id = await enquiries.CreateAsync(
            new NewEnquiry(Channel.ToString(), enquiry.SenderId, enquiry.Body, enquiry.ReceivedAt, CustomerId: null, status, image?.DataUrl),
            cancellationToken);

        return new EnquiryIntakeResult(id, status);
    }

    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
}
