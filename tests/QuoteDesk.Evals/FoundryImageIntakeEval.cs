using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using QuoteDesk.Agents.Checkpointing;
using QuoteDesk.Agents.Llm;
using QuoteDesk.Agents.Pipeline;
using QuoteDesk.Agents.Prompts;
using QuoteDesk.Agents.Tools;
using QuoteDesk.Data;
using QuoteDesk.Data.Repositories;
using Xunit.Abstractions;

namespace QuoteDesk.Evals;

/// <summary>
/// task foundry-04's early check: does Intake's model read a handwritten list
/// accurately enough to build on? Runs the real pipeline on Foundry with a photo-only enquiry — the
/// worked example written out by hand, with "PU" drawn so it reads as "PV" and "spindle" misspelt —
/// and records, per model call, which model answered, its tokens and its latency.
///
/// <c>Fixtures/handwritten-enquiry-synthetic.jpg</c> is a stand-in rendered in a handwriting font,
/// not a photograph of real handwriting: cleaner than the crafted demo photo will be, so a pass here
/// is necessary, not sufficient. Same no-op-without-a-key contract and dev-database wiring as
/// <see cref="FoundryWorkedExampleEval"/>. Run it with
/// <c>dotnet test tests/QuoteDesk.Evals --filter FoundryImageIntakeEval --logger "console;verbosity=detailed"</c>.
/// </summary>
public class FoundryImageIntakeEval(ITestOutputHelper output)
{
    private const string DevConnectionString =
        "Server=localhost,1433;Database=QuoteDesk;User Id=sa;Password=QuoteDesk!Local1;TrustServerCertificate=True";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task StartAsync_HandwrittenPhotoAgainstRealFoundry_ReadsEveryQuantityAndSuspendsAtApproval()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<FoundryImageIntakeEval>().Build();
        var apiKey = configuration["Llm:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return; // No key supplied — this eval is a deliberate no-op outside a manual run.
        }

        var imagePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "handwritten-enquiry-synthetic.jpg");
        var dataUrl = "data:image/jpeg;base64," + Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath));

        var options = new DbContextOptionsBuilder<QuoteDeskDbContext>().UseSqlServer(DevConnectionString).Options;
        await using var db = new QuoteDeskDbContext(options);

        var enquiries = new EnquiryRepository(db);
        var customers = new CustomerRepository(db);
        var catalog = new CatalogRepository(db);
        var stock = new StockRepository(db);
        var priceRules = new PriceRuleRepository(db);
        var quotes = new QuoteRepository(db);
        var timeProvider = TimeProvider.System;

        var enquiryId = await enquiries.CreateAsync(
            new NewEnquiry("Paste", "kiran@shreejitextiles.com", string.Empty, timeProvider.GetUtcNow(), CustomerId: null, "pending", dataUrl),
            CancellationToken.None);

        var pricingTools = new PricingTools(customers, catalog, stock, priceRules, timeProvider);
        var readTools = new ReadToolRegistry(
            new CustomerTools(customers, new OrderHistoryRepository(db)), new CatalogTools(catalog), new StockTools(stock, timeProvider), pricingTools);

        var llmOptions = new LlmOptions
        {
            Provider = "foundry",
            Endpoint = "https://pharshin29-2918-resource.services.ai.azure.com/openai/v1/",
            ApiKey = apiKey,
            Model = "gpt-5-mini",
            IntakeModel = "gpt-5-nano",
            IntakeImageModel = "gpt-5-mini",
            ResolveModel = "gpt-5-mini",
            NarrateModel = "gpt-5-nano",
            LightStageReasoningEffort = ReasoningEffort.None,
            MaxToolCalls = 10,
            TokenBudget = 60_000,
        };

        var calls = new List<string>();
        var chatClients = new ChatClientRegistry(
            llmOptions,
            model => new MeasuringChatClient(ChatClientFactory.Create(llmOptions, model), model, calls),
            loggerFactory: null);
        var checkpointStore = new SqlCheckpointStore(new WorkflowCheckpointRepository(new DevDbContextFactory()), timeProvider);

        var pipeline = new EnquiryPipeline(
            enquiries, new AgentRunRepository(db), readTools, pricingTools, new QuoteWriteTools(quotes, enquiries, timeProvider), quotes, catalog, customers,
            chatClients, new PromptLibrary(), llmOptions, checkpointStore, timeProvider,
            NullLogger<EnquiryPipeline>.Instance);

        var events = new List<AgentEvent>();
        await foreach (var evt in pipeline.StartAsync(enquiryId, CancellationToken.None))
        {
            events.Add(evt);
        }

        output.WriteLine($"Enquiry {enquiryId}, image {new FileInfo(imagePath).Length / 1024} KB");
        calls.ForEach(output.WriteLine);
        foreach (var evt in events.Where(e => e is StageEvent or ToolStartEvent or ToolEndEvent or ErrorEvent))
        {
            output.WriteLine(JsonSerializer.Serialize(evt, evt.GetType(), Json));
        }

        events.Should().NotContain(e => e is ErrorEvent);
        var request = events.OfType<ApprovalRequiredEvent>().Should().ContainSingle().Subject.Payload.Should().BeOfType<ApprovalRequest>().Subject;
        output.WriteLine(JsonSerializer.Serialize(request, Json));

        // Line-by-line reading accuracy: every quantity on the page, read exactly.
        var quantities = request.PricedQuote.Lines.Select(l => l.Quantity).Concat(request.Unresolved.Select(u => u.Quantity));
        quantities.Should().BeEquivalentTo([250, 40, 12], "Intake must read every handwritten quantity exactly");

        request.PricedQuote.Lines.Select(l => l.Sku).Should().Contain("BRG-6203-2RS");
        request.Unresolved.Should().Contain(u => u.OriginalDescription.Contains("spindl", StringComparison.OrdinalIgnoreCase)
            || u.OriginalDescription.Contains("spindel", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Records one line per model round trip: model, latency and token usage.</summary>
    private sealed class MeasuringChatClient(IChatClient inner, string model, List<string> calls) : DelegatingChatClient(inner)
    {
        public override async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var messageList = messages.ToList();
            var hasImage = messageList.SelectMany(m => m.Contents).OfType<DataContent>().Any();
            var stopwatch = Stopwatch.StartNew();
            var response = await base.GetResponseAsync(messageList, options, cancellationToken);
            calls.Add($"{model}: {stopwatch.ElapsedMilliseconds} ms, input {response.Usage?.InputTokenCount} tokens, "
                + $"output {response.Usage?.OutputTokenCount} tokens{(hasImage ? ", with image" : "")}");
            return response;
        }
    }

    private sealed class DevDbContextFactory : IDbContextFactory<QuoteDeskDbContext>
    {
        public QuoteDeskDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<QuoteDeskDbContext>().UseSqlServer(DevConnectionString).Options);

        public Task<QuoteDeskDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
