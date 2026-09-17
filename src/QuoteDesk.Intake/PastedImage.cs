using System.Diagnostics.CodeAnalysis;

namespace QuoteDesk.Intake;

/// <summary>
/// Parses and validates a photographed enquiry sent as a <c>data:image/...;base64,...</c> URL on the
/// paste endpoint (foundry-04). The image rides the existing JSON POST rather than a multipart upload,
/// so this is the one place its shape, type and size are checked — before anything touches the
/// database, and before the base64 is ever handed to a model.
/// </summary>
public static class PastedImage
{
    /// <summary>The largest decoded image accepted. The web client downscales to ~1280px JPEG first,
    /// which lands far below this; the cap exists for a client that does not.</summary>
    public const int MaxDecodedBytes = 2 * 1024 * 1024;

    private const string DataScheme = "data:";
    private const string Base64Marker = ";base64,";
    private const string TooLargeError = "The image is larger than 2 MB.";
    private const string ContentMismatchError = "The image content does not match its type.";

    // Raster formats a vision model reads. SVG is deliberately absent: it is markup, not a photo.
    private static readonly HashSet<string> AllowedMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
    };

    public static bool TryParse(
        string dataUrl,
        [NotNullWhen(true)] out EnquiryAttachment? attachment,
        [NotNullWhen(false)] out string? error) =>
        TryParseAndDecode(dataUrl, out attachment, out error, out _);

    /// <summary>
    /// Decodes a stored photo for serving it back (<c>GET /api/enquiries/{id}/image</c>). It runs the
    /// full <see cref="TryParse"/> validation, so only an allowlisted raster type whose bytes match its
    /// label is ever returned, whatever ended up in the column — and it returns the bytes that
    /// validation already decoded rather than decoding a second time.
    /// </summary>
    public static bool TryDecode(
        string dataUrl,
        [NotNullWhen(true)] out string? mediaType,
        [NotNullWhen(true)] out byte[]? content)
    {
        if (!TryParseAndDecode(dataUrl, out var attachment, out _, out content))
        {
            mediaType = null;
            return false;
        }

        mediaType = attachment.ContentType;
        return true;
    }

    private static bool TryParseAndDecode(
        string dataUrl,
        [NotNullWhen(true)] out EnquiryAttachment? attachment,
        [NotNullWhen(false)] out string? error,
        [NotNullWhen(true)] out byte[]? content)
    {
        ArgumentNullException.ThrowIfNull(dataUrl);
        attachment = null;
        content = null;

        if (!dataUrl.StartsWith(DataScheme, StringComparison.OrdinalIgnoreCase))
        {
            error = "The image must be a data URL (data:image/jpeg;base64,...).";
            return false;
        }

        var marker = dataUrl.IndexOf(Base64Marker, StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
        {
            error = "The image data URL must be base64-encoded.";
            return false;
        }

        var mediaType = dataUrl[DataScheme.Length..marker];
        if (!AllowedMediaTypes.Contains(mediaType))
        {
            error = "The image must be a JPEG, PNG or WebP.";
            return false;
        }

        var payload = dataUrl.AsSpan(marker + Base64Marker.Length);
        if (payload.IsEmpty)
        {
            error = "The image is empty.";
            return false;
        }

        // Reject on the encoded length first, so an oversized upload is refused without decoding it.
        // Base64 carries 3 bytes per 4 characters; the +3 allows for padding on an at-limit payload.
        if (payload.Length > ((MaxDecodedBytes + 2) / 3 * 4) + 3)
        {
            error = TooLargeError;
            return false;
        }

        var buffer = new byte[(payload.Length / 4 * 3) + 3];
        if (!Convert.TryFromBase64Chars(payload, buffer, out var decodedBytes))
        {
            error = "The image data is not valid base64.";
            return false;
        }

        if (decodedBytes > MaxDecodedBytes)
        {
            error = TooLargeError;
            return false;
        }

        // The label is the client's claim; the first bytes are the file's. Anything that is not
        // really the declared raster format is refused before it is stored or sent to a model.
        if (!HasSignatureOf(mediaType, buffer.AsSpan(0, decodedBytes)))
        {
            error = ContentMismatchError;
            return false;
        }

        attachment = new EnquiryAttachment
        {
            FileName = "pasted-image." + mediaType["image/".Length..].ToLowerInvariant(),
            ContentType = mediaType.ToLowerInvariant(),
            SizeBytes = decodedBytes,
            DataUrl = dataUrl,
        };
        content = buffer[..decodedBytes];
        error = null;
        return true;
    }

    /// <summary>Whether <paramref name="content"/> starts with the file signature of
    /// <paramref name="mediaType"/>: JPEG <c>FF D8 FF</c>; PNG <c>89 50 4E 47 0D 0A 1A 0A</c>;
    /// WebP <c>RIFF</c> at bytes 0–3 and <c>WEBP</c> at bytes 8–11.</summary>
    private static bool HasSignatureOf(string mediaType, ReadOnlySpan<byte> content) =>
        mediaType.ToLowerInvariant() switch
        {
            "image/jpeg" => content.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]),
            "image/png" => content.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            "image/webp" => content.Length >= 12 && content.StartsWith("RIFF"u8) && content[8..12].SequenceEqual("WEBP"u8),
            _ => false,
        };
}
