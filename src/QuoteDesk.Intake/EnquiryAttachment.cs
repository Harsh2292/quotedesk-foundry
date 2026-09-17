namespace QuoteDesk.Intake;

/// <summary>
/// One file attached to an enquiry — a photo of a written list, a voice note. The paste channel's
/// photo (foundry-04, via <see cref="PastedImage"/>) is the first producer, and it carries its
/// content in <see cref="DataUrl"/>; an attachment known only by its metadata has none.
/// </summary>
public sealed record EnquiryAttachment
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }

    /// <summary>The content as a <c>data:</c> URL, when it was received and can be read. Null for an
    /// attachment known only by name and type — which is why the status rule looks at this, not at
    /// the attachment's mere presence.</summary>
    public string? DataUrl { get; init; }

    /// <summary>True for an image whose content is here — something the Intake agent can read.</summary>
    public bool IsReadableImage =>
        DataUrl is not null && ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
