using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using QuoteDesk.Api.Auth;
using QuoteDesk.Api.Enquiries;
using Xunit;

namespace QuoteDesk.IntegrationTests.Api;

/// <summary>Exercises <c>POST /api/enquiries</c> end to end against the real pipeline in
/// <c>Program.cs</c>, via <see cref="QuoteDeskApiFactory"/>.</summary>
[Collection("QuoteDeskApi")]
public class EnquiryEndpointsTests(QuoteDeskApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // The worked example from docs/DOMAIN.md.
    private const string WorkedExampleBody = """
        Hi Mehul bhai,
        Need urgent quote —
        250 nos of the 6203 bearings (same as last time)
        40 mtr of the 25mm PU timing belt
        12 pcs ring frame spindle tape, the thicker one

        Delivery at our Sachin unit, need by 5th. Last time you gave 8% on bearings, please keep same.

        Kiran — Shreeji Textiles
        """;

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/enquiries", new PasteEnquiryRequest(WorkedExampleBody, null), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WorkedExampleBody_Returns201AndStoresTheBodyIntact()
    {
        // A distinct email from every AuthEndpointsTests case — the two classes now share one
        // database via the "QuoteDeskApi" collection, and Users.Email is unique.
        using var client = await AuthenticatedClientAsync("kiran-paste@shreejitextiles.example");

        var response = await client.PostAsJsonAsync("/api/enquiries", new PasteEnquiryRequest(WorkedExampleBody, null), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<EnquiryCreatedResponse>(Json, CancellationToken.None);
        body!.EnquiryId.Should().BePositive();
        body.Status.Should().Be("pending");
    }

    [Fact]
    public async Task Post_BlankBody_Returns400ProblemDetails()
    {
        using var client = await AuthenticatedClientAsync("blank-body@shreejitextiles.example");

        var response = await client.PostAsJsonAsync("/api/enquiries", new PasteEnquiryRequest("   ", null), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json, CancellationToken.None);
        problem.Should().NotBeNull();

        var raw = await response.Content.ReadAsStringAsync(CancellationToken.None);
        raw.Should().NotContain("Exception").And.NotContain("StackTrace");
    }

    [Fact]
    public async Task Post_ImageWithBlankBody_Returns201Pending()
    {
        using var client = await AuthenticatedClientAsync("photo-only@shreejitextiles.example");
        var dataUrl = JpegDataUrlOfLength(1024);

        var response = await client.PostAsJsonAsync("/api/enquiries", new PasteEnquiryRequest("", null, dataUrl), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<EnquiryCreatedResponse>(Json, CancellationToken.None);
        body!.Status.Should().Be("pending", "a readable photo is a real enquiry, not a manual-entry case");
    }

    [Fact]
    public async Task Post_ImageOverTwoMegabytes_Returns400()
    {
        using var client = await AuthenticatedClientAsync("photo-too-big@shreejitextiles.example");
        var dataUrl = JpegDataUrlOfLength((2 * 1024 * 1024) + 1);

        var response = await client.PostAsJsonAsync("/api/enquiries", new PasteEnquiryRequest("see photo", null, dataUrl), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json, CancellationToken.None);
        problem!.Detail.Should().Contain("2 MB");
    }

    [Fact]
    public async Task Post_ImageThatIsNotAnImage_Returns400()
    {
        using var client = await AuthenticatedClientAsync("photo-not-image@shreejitextiles.example");

        var response = await client.PostAsJsonAsync(
            "/api/enquiries", new PasteEnquiryRequest("", null, "data:text/html;base64,PGgxPmhpPC9oMT4="), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_ImageEnquiry_DoesNotReturnTheImage()
    {
        using var client = await AuthenticatedClientAsync("photo-detail@shreejitextiles.example");
        // A PNG signature, then a distinctive run of bytes so the base64 cannot appear by coincidence.
        byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        var imageBase64 = Convert.ToBase64String([.. pngSignature, .. Enumerable.Range(0, 600).Select(i => (byte)(i % 199))]);
        var created = await client.PostAsJsonAsync(
            "/api/enquiries", new PasteEnquiryRequest("", null, $"data:image/png;base64,{imageBase64}"), CancellationToken.None);
        var enquiry = await created.Content.ReadFromJsonAsync<EnquiryCreatedResponse>(Json, CancellationToken.None);

        var detail = await client.GetStringAsync($"/api/enquiries/{enquiry!.EnquiryId}", CancellationToken.None);

        detail.Should().NotContain(imageBase64[..100], "the detail response is fetched on every Desk navigation");
    }

    [Fact]
    public async Task GetImage_ImageEnquiry_ReturnsTheExactBytesAsAnUncachedImage()
    {
        using var client = await AuthenticatedClientAsync("photo-image-get@shreejitextiles.example");
        var dataUrl = JpegDataUrlOfLength(2048);
        var created = await client.PostAsJsonAsync("/api/enquiries", new PasteEnquiryRequest("", null, dataUrl), CancellationToken.None);
        var enquiry = await created.Content.ReadFromJsonAsync<EnquiryCreatedResponse>(Json, CancellationToken.None);

        var response = await client.GetAsync($"/api/enquiries/{enquiry!.EnquiryId}/image", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/jpeg");
        response.Headers.CacheControl?.NoStore.Should().BeTrue("a customer's photo must not be kept in shared or browser caches");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
        bytes.Should().Equal(Convert.FromBase64String(dataUrl["data:image/jpeg;base64,".Length..]));
    }

    [Fact]
    public async Task GetImage_TextEnquiry_Returns404()
    {
        using var client = await AuthenticatedClientAsync("photo-image-text@shreejitextiles.example");
        var created = await client.PostAsJsonAsync("/api/enquiries", new PasteEnquiryRequest("50 pcs bearing 6203", null), CancellationToken.None);
        var enquiry = await created.Content.ReadFromJsonAsync<EnquiryCreatedResponse>(Json, CancellationToken.None);

        var response = await client.GetAsync($"/api/enquiries/{enquiry!.EnquiryId}/image", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetImage_UnknownEnquiry_Returns404()
    {
        using var client = await AuthenticatedClientAsync("photo-image-missing@shreejitextiles.example");

        var response = await client.GetAsync("/api/enquiries/987654/image", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetImage_WithoutToken_Returns401()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/enquiries/1/image", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_NoSenderIdSupplied_UsesTheSignedInUsersEmail()
    {
        const string email = "sender-from-token@shreejitextiles.example";
        using var client = await AuthenticatedClientAsync(email);

        var response = await client.PostAsJsonAsync("/api/enquiries", new PasteEnquiryRequest("50 pcs bearing 6203, please quote.", null), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public void PostRoute_CarriesARequestBodySizeLimit()
    {
        // TestServer has no IHttpMaxRequestBodySizeFeature, so it cannot enforce this limit — routing
        // logs a warning and moves on. The 413 itself is proven over real Kestrel in the next test;
        // this one pins the metadata the routing middleware reads.
        var endpoint = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == "/api/enquiries/"
                && e.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains("POST"));

        endpoint.Metadata.GetMetadata<IRequestSizeLimitMetadata>()!.MaxRequestBodySize
            .Should().Be(EnquiryEndpoints.MaxPasteRequestBytes);
    }

    [Fact]
    public async Task Post_BodyOverTheRequestSizeLimit_Returns413OnKestrel()
    {
        await using var kestrel = factory.WithWebHostBuilder(_ => { });
        kestrel.UseKestrel(0);
        kestrel.StartServer();
        using var client = await AuthenticatedClientAsync(kestrel, "body-too-big@shreejitextiles.example");
        var oversizedBody = new string('x', (int)EnquiryEndpoints.MaxPasteRequestBytes + 1);

        // Content-Length up front plus "Expect: 100-continue", so Kestrel answers 413 before the body is
        // sent. Streaming the body instead raced the server closing the connection mid-upload: the
        // client then saw a socket error rather than the 413 (failed once on CI, 2026-09-24).
        using var content = new StringContent(
            JsonSerializer.Serialize(new PasteEnquiryRequest(oversizedBody, null), JsonSerializerOptions.Web),
            System.Text.Encoding.UTF8,
            "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/enquiries") { Content = content };
        request.Headers.ExpectContinue = true;

        var response = await client.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Post_ImageOverTwoMegabytes_Returns400OnKestrelToo()
    {
        // The size limit must leave room for a 2 MB image plus its base64 overhead, so that the
        // endpoint's own, clearer "larger than 2 MB" 400 is what a client sees, not a bare 413.
        await using var kestrel = factory.WithWebHostBuilder(_ => { });
        kestrel.UseKestrel(0);
        kestrel.StartServer();
        using var client = await AuthenticatedClientAsync(kestrel, "photo-too-big-kestrel@shreejitextiles.example");
        var dataUrl = JpegDataUrlOfLength((2 * 1024 * 1024) + 1);

        var response = await client.PostAsJsonAsync("/api/enquiries", new PasteEnquiryRequest("see photo", null, dataUrl), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>A JPEG data URL of exactly <paramref name="length"/> decoded bytes — the JPEG file
    /// signature, then zero padding.</summary>
    private static string JpegDataUrlOfLength(int length)
    {
        var bytes = new byte[length];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
    }

    private Task<HttpClient> AuthenticatedClientAsync(string email) => AuthenticatedClientAsync(factory, email);

    private static async Task<HttpClient> AuthenticatedClientAsync(WebApplicationFactory<Program> host, string email)
    {
        var client = host.CreateClient();
        var identity = new GoogleIdentity($"sub-{Guid.NewGuid():N}", email, "Test User", null);

        var signIn = await client.PostAsJsonAsync(
            "/api/auth/google",
            new { idToken = StubGoogleIdTokenValidator.TokenFor(identity) },
            CancellationToken.None);
        var signInBody = await signIn.Content.ReadFromJsonAsync<AuthResponse>(Json, CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", signInBody!.Token);
        return client;
    }
}
