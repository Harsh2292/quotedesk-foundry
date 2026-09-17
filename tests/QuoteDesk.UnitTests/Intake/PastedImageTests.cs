using FluentAssertions;
using QuoteDesk.Intake;

namespace QuoteDesk.UnitTests.Intake;

public class PastedImageTests
{
    // A 1x1 transparent PNG, the smallest real image there is.
    private const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF, 0xE0];
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] WebpHeader = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBPVP8 "u8];

    [Fact]
    public void TryParse_ValidPngDataUrl_ReturnsAttachmentCarryingTheData()
    {
        var dataUrl = SmallPngDataUrl;

        var ok = PastedImage.TryParse(dataUrl, out var attachment, out var error);

        ok.Should().BeTrue();
        error.Should().BeNull();
        attachment!.ContentType.Should().Be("image/png");
        attachment.DataUrl.Should().Be(dataUrl);
        attachment.SizeBytes.Should().Be(Convert.FromBase64String(OnePixelPngBase64).Length);
    }

    [Theory]
    [InlineData("image/png", 64)]
    [InlineData("image/jpeg", 64)]
    [InlineData("image/webp", 64)]
    [InlineData("image/jpeg", 4)]
    [InlineData("image/webp", 12)]
    public void TryParse_ContentStartsWithItsTypesSignature_IsAccepted(string mediaType, int length)
    {
        var dataUrl = DataUrlOf(mediaType, length);

        var ok = PastedImage.TryParse(dataUrl, out var attachment, out var error);

        ok.Should().BeTrue();
        error.Should().BeNull();
        attachment!.ContentType.Should().Be(mediaType);
        attachment.SizeBytes.Should().Be(length);
    }

    [Theory]
    [InlineData("image/png", "image/jpeg")]
    [InlineData("image/png", "image/webp")]
    [InlineData("image/jpeg", "image/png")]
    [InlineData("image/webp", "image/jpeg")]
    public void TryParse_ContentOfAnotherImageType_IsRejected(string declared, string actual)
    {
        var dataUrl = $"data:{declared};base64," + Convert.ToBase64String(BytesOf(actual, 64));

        var ok = PastedImage.TryParse(dataUrl, out var attachment, out var error);

        ok.Should().BeFalse();
        attachment.Should().BeNull();
        error.Should().Be("The image content does not match its type.");
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/webp")]
    public void TryParse_ContentThatIsNotAnImage_IsRejected(string declared)
    {
        // Plain text wearing an image label — "hello, this is not a picture".
        var dataUrl = $"data:{declared};base64," + Convert.ToBase64String("hello, this is not a picture"u8.ToArray());

        var ok = PastedImage.TryParse(dataUrl, out _, out var error);

        ok.Should().BeFalse();
        error.Should().Be("The image content does not match its type.");
    }

    [Fact]
    public void TryParse_WebpLabelWithRiffButNoWebpMarker_IsRejected()
    {
        // A RIFF container that is not WebP (a WAV file starts "RIFF....WAVE").
        byte[] wav = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WAVEfmt "u8];
        var dataUrl = "data:image/webp;base64," + Convert.ToBase64String(wav);

        PastedImage.TryParse(dataUrl, out _, out var error).Should().BeFalse();
        error.Should().Be("The image content does not match its type.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a data url")]
    [InlineData("data:image/png;base64,")]
    [InlineData("data:image/png,rawbytes")]
    [InlineData("data:image/png;base64,@@@not-base64@@@")]
    [InlineData("data:text/plain;base64,aGVsbG8=")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=")]
    [InlineData("https://example.com/list.jpg")]
    public void TryParse_MalformedOrUnsupported_ReturnsError(string dataUrl)
    {
        var ok = PastedImage.TryParse(dataUrl, out var attachment, out var error);

        ok.Should().BeFalse();
        attachment.Should().BeNull();
        error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TryParse_ExactlyAtTheSizeLimit_IsAccepted()
    {
        var dataUrl = DataUrlOf("image/jpeg", PastedImage.MaxDecodedBytes);

        PastedImage.TryParse(dataUrl, out var attachment, out _).Should().BeTrue();
        attachment!.SizeBytes.Should().Be(PastedImage.MaxDecodedBytes);
    }

    [Fact]
    public void TryParse_OneByteOverTheSizeLimit_IsRejected()
    {
        var dataUrl = DataUrlOf("image/jpeg", PastedImage.MaxDecodedBytes + 1);

        var ok = PastedImage.TryParse(dataUrl, out _, out var error);

        ok.Should().BeFalse();
        error.Should().Contain("2 MB");
    }

    [Fact]
    public void TryDecode_ValidDataUrl_ReturnsItsMediaTypeAndExactBytes()
    {
        var bytes = BytesOf("image/jpeg", 300);

        var ok = PastedImage.TryDecode("data:image/jpeg;base64," + Convert.ToBase64String(bytes), out var mediaType, out var content);

        ok.Should().BeTrue();
        mediaType.Should().Be("image/jpeg");
        content.Should().Equal(bytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("data:text/html;base64,PGgxPmhpPC9oMT4=")]
    [InlineData("data:image/png;base64,aGVsbG8sIHRoaXMgaXMgbm90IGEgcGljdHVyZQ==")]
    public void TryDecode_InvalidOrMismatchedDataUrl_ReturnsFalse(string dataUrl)
    {
        var ok = PastedImage.TryDecode(dataUrl, out var mediaType, out var content);

        ok.Should().BeFalse();
        mediaType.Should().BeNull();
        content.Should().BeNull();
    }

    [Fact]
    public void TryParse_Null_Throws()
    {
        var act = () => PastedImage.TryParse(null!, out _, out _);

        act.Should().Throw<ArgumentNullException>();
    }

    internal static string SmallPngDataUrl => $"data:image/png;base64,{OnePixelPngBase64}";

    /// <summary>A data URL of exactly <paramref name="length"/> decoded bytes: the declared type's
    /// file signature, then zero padding.</summary>
    private static string DataUrlOf(string mediaType, int length) =>
        $"data:{mediaType};base64," + Convert.ToBase64String(BytesOf(mediaType, length));

    private static byte[] BytesOf(string mediaType, int length)
    {
        var header = mediaType switch
        {
            "image/jpeg" => JpegHeader,
            "image/png" => PngHeader,
            "image/webp" => WebpHeader,
            _ => throw new ArgumentOutOfRangeException(nameof(mediaType), mediaType, "No signature for this type."),
        };

        var bytes = new byte[length];
        header.AsSpan(0, Math.Min(header.Length, length)).CopyTo(bytes);
        return bytes;
    }
}
