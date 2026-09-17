using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using QuoteDesk.Agents.Pipeline;
using QuoteDesk.Api.Streaming;
using QuoteDesk.Data.Repositories;
using QuoteDesk.Intake;

namespace QuoteDesk.Api.Enquiries;

/// <summary>A pasted enquiry. <paramref name="ImageDataUrl"/> is an optional photo of it as a
/// <c>data:image/...;base64,...</c> URL (foundry-04) — riding this JSON POST rather than a multipart
/// upload; with a photo, <paramref name="Body"/> may be blank.</summary>
public sealed record PasteEnquiryRequest(string Body, string? SenderId, string? ImageDataUrl = null);

public sealed record EnquiryCreatedResponse(int EnquiryId, string Status);

/// <summary>What <c>GET /api/enquiries/{id}</c> returns — the enquiry plus its latest pipeline run,
/// if one exists. <see cref="Trace"/> is the persisted transcript (docs/SPEC.md §8's
/// <c>AgentEvent</c> stream), replayed after the live SSE stream that produced it has closed.</summary>
public sealed record EnquiryDetailResponse(
    int Id,
    string Channel,
    string SenderId,
    string RawBody,
    DateTimeOffset ReceivedAt,
    int? CustomerId,
    string Status,
    string? RunStatus,
    ApprovalRequest? PendingApproval,
    IReadOnlyList<AgentEvent>? Trace);

public static class EnquiryEndpoints
{
    /// <summary>The largest request body <c>POST /api/enquiries</c> accepts, in bytes: a 2 MB image is
    /// ~2.8 MB once base64-encoded, plus the pasted text and JSON framing. Kestrel answers anything
    /// bigger with 413 before the body is buffered, instead of its 30 MB server-wide default.</summary>
    public const long MaxPasteRequestBytes = 3_500_000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapEnquiryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/enquiries");

        // RequestSizeLimitAttribute is IRequestSizeLimitMetadata, which endpoint routing applies to
        // minimal APIs too (not only MVC) by setting IHttpMaxRequestBodySizeFeature for the request.
        group.MapPost("/", CreateFromPasteAsync).WithMetadata(new RequestSizeLimitAttribute(MaxPasteRequestBytes));
        // "pipeline" is a hard, demo-wide daily cap stacked on top of the app-wide GlobalLimiter
        // (Program.cs) — this is the one route that spends the shared Gemini key.
        group.MapPost("/{id:int}/process", ProcessAsync).RequireRateLimiting("pipeline");
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapGet("/{id:int}/image", GetImageAsync);

        return app;
    }

    private static async Task<Results<Created<EnquiryCreatedResponse>, ProblemHttpResult>> CreateFromPasteAsync(
        PasteEnquiryRequest request,
        ClaimsPrincipal principal,
        PasteAdapter adapter,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        // A blank body with no photo is a client bug (an empty textarea submit), not a business case —
        // reject it outright. A photo on its own is a real enquiry: the Intake agent reads it.
        var imageDataUrl = string.IsNullOrWhiteSpace(request.ImageDataUrl) ? null : request.ImageDataUrl;
        if (string.IsNullOrWhiteSpace(request.Body) && imageDataUrl is null)
        {
            return TypedResults.Problem("Body must not be empty.", statusCode: StatusCodes.Status400BadRequest);
        }

        // Checked before anything touches the database: type, base64 shape and the ~2 MB cap.
        if (imageDataUrl is not null && !PastedImage.TryParse(imageDataUrl, out _, out var imageError))
        {
            return TypedResults.Problem(imageError, statusCode: StatusCodes.Status400BadRequest);
        }

        // MapInboundClaims is disabled in Program.cs, so "email" comes through exactly as JwtIssuer
        // wrote it, not remapped to the legacy XML claim type.
        var senderId = request.SenderId ?? principal.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(senderId))
        {
            return TypedResults.Problem("SenderId was not supplied and the token carries no email claim.", statusCode: StatusCodes.Status400BadRequest);
        }

        var body = request.Body ?? string.Empty;
        var enquiry = imageDataUrl is not null
            ? PasteAdapter.FromPastedTextAndImage(senderId, body, imageDataUrl, timeProvider.GetUtcNow())
            : PasteAdapter.FromPastedText(senderId, body, timeProvider.GetUtcNow());
        var result = await adapter.IngestAsync(enquiry, cancellationToken);

        return TypedResults.Created(
            $"/api/enquiries/{result.EnquiryId}",
            new EnquiryCreatedResponse(result.EnquiryId, result.Status));
    }

    /// <summary>Streams the pipeline over SSE. Returns <c>Task</c>, not an <c>IResult</c> — see
    /// <c>ApprovalEndpoints.DecideAsync</c>'s remarks for why a streaming endpoint takes this shape.
    /// A missing enquiry is not checked here: <see cref="EnquiryPipeline.ProcessAsync"/> already
    /// reports it as an <c>ErrorEvent</c> on the stream itself, which is the one error channel a
    /// client reading SSE is already watching. Calls <c>ProcessAsync</c>, not <c>StartAsync</c>
    /// directly — <c>ProcessAsync</c> transparently resumes a failed run past Resolve when Resolve
    /// already succeeded, rather than always restarting from Intake; the existing "Retry" button on
    /// the Desk needed no change to gain this.</summary>
    private static async Task ProcessAsync(
        int id,
        HttpContext context,
        EnquiryPipeline pipeline,
        IAgentRunRepository agentRuns,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await AgentEventStreamWriter.WriteAsync(
            context,
            pipeline.ProcessAsync(id, cancellationToken),
            async ct =>
            {
                // Whether this call started fresh or resumed in place, the run it acted on — if any —
                // is the latest for this enquiry by the time the stream has ended. Null when the
                // enquiry did not exist and no run was ever created.
                var run = await agentRuns.GetLatestByEnquiryIdAsync(id, ct);
                return run?.Id;
            },
            agentRuns,
            timeProvider,
            cancellationToken);
    }

    /// <summary>The enquiry's photo (foundry-04), so the approval card can show a human what the customer
    /// actually wrote next to what Intake read from it. Kept out of <see cref="EnquiryDetailResponse"/>,
    /// which is fetched on every Desk navigation. Behind the same fallback authorization policy as every
    /// other route; never cached, and served with <c>nosniff</c> so a browser cannot reinterpret it.</summary>
    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> GetImageAsync(
        int id,
        HttpContext context,
        IEnquiryRepository enquiries,
        CancellationToken cancellationToken)
    {
        var dataUrl = await enquiries.GetImageDataUrlAsync(id, cancellationToken);
        if (dataUrl is null || !PastedImage.TryDecode(dataUrl, out var mediaType, out var content))
        {
            return TypedResults.Problem($"Enquiry {id} has no photo.", statusCode: StatusCodes.Status404NotFound);
        }

        context.Response.Headers.CacheControl = "no-store, private";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        return TypedResults.File(content, mediaType);
    }

    private static async Task<Results<Ok<EnquiryDetailResponse>, ProblemHttpResult>> GetByIdAsync(
        int id,
        IEnquiryRepository enquiries,
        IAgentRunRepository agentRuns,
        CancellationToken cancellationToken)
    {
        var enquiry = await enquiries.GetByIdAsync(id, cancellationToken);
        if (enquiry is null)
        {
            return TypedResults.Problem($"Enquiry {id} does not exist.", statusCode: StatusCodes.Status404NotFound);
        }

        var run = await agentRuns.GetLatestByEnquiryIdAsync(id, cancellationToken);
        var pendingApproval = run?.ApprovalRequestJson is { } approvalJson
            ? JsonSerializer.Deserialize<EnquiryPipeline.StoredApproval>(approvalJson, JsonOptions)?.Request
            : null;
        var trace = run?.TraceJson is { } traceJson
            ? JsonSerializer.Deserialize<List<AgentEvent>>(traceJson, JsonOptions)
            : null;

        return TypedResults.Ok(new EnquiryDetailResponse(
            enquiry.Id,
            enquiry.Channel,
            enquiry.SenderId,
            enquiry.RawBody,
            enquiry.ReceivedAt,
            enquiry.CustomerId,
            enquiry.Status,
            run?.Status,
            pendingApproval,
            trace));
    }
}
