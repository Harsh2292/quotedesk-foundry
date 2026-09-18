using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using QuoteDesk.Agents.Checkpointing;
using QuoteDesk.Agents.Llm;
using QuoteDesk.Agents.Pipeline;
using QuoteDesk.Agents.Prompts;
using QuoteDesk.Agents.Tools;
using QuoteDesk.IntegrationTests.Data;

namespace QuoteDesk.IntegrationTests.Agents;

/// <summary>
/// Builds a real <see cref="EnquiryPipeline"/> over the seeded test database and a stubbed
/// <see cref="IChatClient"/> — extracted from <see cref="EnquiryPipelineTests"/> when a second test
/// class (<see cref="AgentTelemetryTests"/>) needed the identical wiring. Two copies of this would
/// drift, and a telemetry test running against a pipeline assembled slightly differently from the one
/// under test would be worth very little.
/// </summary>
internal static class EnquiryPipelineFactory
{
    /// <summary>The fixed clock every pipeline test shares, so dates in assertions are stable.</summary>
    public static readonly DateTimeOffset Now = new(2026, 3, 26, 8, 41, 0, TimeSpan.FromHours(5.5));

    public static EnquiryPipeline Build(
        RepositoryFixture fixture,
        IChatClient chatClient,
        int tokenBudget = 20_000,
        int maxToolCalls = 8,
        int intakeMaxToolCalls = 2,
        string? intakeModel = null,
        string? intakeImageModel = null,
        bool traceSensitiveData = false)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var timeProvider = new FixedTimeProvider(Now);
        var customerTools = new CustomerTools(fixture.Customers, fixture.OrderHistory);
        var catalogTools = new CatalogTools(fixture.Catalog);
        var stockTools = new StockTools(fixture.Stock, timeProvider);
        var pricingTools = new PricingTools(fixture.Customers, fixture.Catalog, fixture.Stock, fixture.PriceRules, timeProvider);
        var readTools = new ReadToolRegistry(customerTools, catalogTools, stockTools, pricingTools);
        var writeTools = new QuoteWriteTools(fixture.Quotes, fixture.Enquiries, timeProvider);
        var options = new LlmOptions
        {
            Endpoint = "https://example.test/",
            ApiKey = "unused",
            Model = "stub",
            MaxToolCalls = maxToolCalls,
            IntakeMaxToolCalls = intakeMaxToolCalls,
            TokenBudget = tokenBudget,
            IntakeModel = intakeModel,
            IntakeImageModel = intakeImageModel,
            TraceSensitiveData = traceSensitiveData,
        };
        var checkpointStore = new SqlCheckpointStore(fixture.Checkpoints, timeProvider);

        // One shared stub instance for every stage — its ordered turns assume Intake, Resolve and
        // Narrate all draw from the same script, exactly as they did before per-stage model routing.
        var chatClients = new ChatClientRegistry(options, _ => chatClient, loggerFactory: null);

        return new EnquiryPipeline(
            fixture.Enquiries,
            fixture.AgentRuns,
            readTools,
            pricingTools,
            writeTools,
            fixture.Quotes,
            fixture.Catalog,
            fixture.Customers,
            chatClients,
            new PromptLibrary(),
            options,
            checkpointStore,
            timeProvider,
            NullLogger<EnquiryPipeline>.Instance);
    }
}
