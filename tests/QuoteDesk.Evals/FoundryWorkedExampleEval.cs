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

namespace QuoteDesk.Evals;

/// <summary>
/// The gate for task foundry-02 (see tasks/task-foundry-02-provider.md's acceptance criteria): does
/// the real Foundry resource endpoint — <c>https://pharshin29-2918-resource.services.ai.azure.com/openai/v1/</c>,
/// confirmed live in docs/FOUNDRY-PLAN.md Step 0 — accept the tool-call argument shapes
/// <c>AIFunctionFactory</c> produces, end to end against docs/DOMAIN.md's worked example, through real
/// multi-turn <c>FunctionInvokingChatClient</c> calls, not a hello-world completion? Copied from
/// <see cref="GeminiWorkedExampleEval"/>'s structure — same worked example, same repository wiring,
/// same no-op-without-a-key contract. Reads <c>Llm:ApiKey</c> from the same local
/// <c>dotnet user-secrets</c> store <c>QuoteDesk.Api</c> uses, never a command-line environment
/// variable, so the key is never typed anywhere a shell history or an approval prompt could echo it.
/// Runs against the real local dev database (already seeded), never a test database, and never
/// resumes to approval, so it only reads.
/// </summary>
public class FoundryWorkedExampleEval
{
    private const string DevConnectionString =
        "Server=localhost,1433;Database=QuoteDesk;User Id=sa;Password=QuoteDesk!Local1;TrustServerCertificate=True";

    [Fact]
    public async Task StartAsync_WorkedExampleAgainstRealFoundry_ResolvesBearingsAndBeltAndSuspendsAtApproval()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<FoundryWorkedExampleEval>()
            .Build();
        var apiKey = configuration["Llm:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return; // No key supplied — this eval is a deliberate no-op outside a manual run.
        }

        var options = new DbContextOptionsBuilder<QuoteDeskDbContext>().UseSqlServer(DevConnectionString).Options;
        await using var db = new QuoteDeskDbContext(options);

        var enquiries = new EnquiryRepository(db);
        var agentRuns = new AgentRunRepository(db);
        var customers = new CustomerRepository(db);
        var catalog = new CatalogRepository(db);
        var stock = new StockRepository(db);
        var orderHistory = new OrderHistoryRepository(db);
        var priceRules = new PriceRuleRepository(db);
        var quotes = new QuoteRepository(db);
        var timeProvider = TimeProvider.System;

        var enquiryId = await enquiries.CreateAsync(
            new NewEnquiry(
                "Paste",
                "kiran@shreejitextiles.com",
                """
                Hi Mehul bhai,
                Need urgent quote —
                250 nos of the 6203 bearings (same as last time)
                40 mtr of the 25mm PU timing belt
                12 pcs ring frame spindle tape, the thicker one

                Delivery at our Sachin unit, need by 5th. Last time you gave 8% on bearings, please keep same.

                Kiran — Shreeji Textiles
                """,
                timeProvider.GetUtcNow(),
                CustomerId: null,
                "pending"),
            CancellationToken.None);

        var customerTools = new CustomerTools(customers, orderHistory);
        var catalogTools = new CatalogTools(catalog);
        var stockTools = new StockTools(stock, timeProvider);
        var pricingTools = new PricingTools(customers, catalog, stock, priceRules, timeProvider);
        var readTools = new ReadToolRegistry(customerTools, catalogTools, stockTools, pricingTools);
        var writeTools = new QuoteWriteTools(quotes, enquiries, timeProvider);

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
        // Mirrors appsettings.json's "foundry" profile exactly — per-stage models and the light-stage
        // reasoning effort. An earlier version ran gpt-5-mini on every stage, which meant the reasoning
        // setting production sends to gpt-5-nano was never exercised by any live run (found in code
        // review, 2026-09-17). This eval is only worth anything if it runs what production runs.
        var chatClients = new ChatClientRegistry(llmOptions, model => ChatClientFactory.Create(llmOptions, model), loggerFactory: null);
        // A dedicated factory, not the scoped `db` above: the workflow engine writes checkpoints from
        // its own background execution task, concurrently with this method's own use of `db` — the
        // same reason WorkflowCheckpointRepository uses IDbContextFactory in production (docs/SPEC.md
        // §6). Sharing one instance throws EF Core's "a second operation was started on this context".
        var checkpointStore = new SqlCheckpointStore(new WorkflowCheckpointRepository(new DevDbContextFactory()), timeProvider);

        var pipeline = new EnquiryPipeline(
            enquiries, agentRuns, readTools, pricingTools, writeTools, quotes, catalog, customers,
            chatClients, new PromptLibrary(), llmOptions, checkpointStore, timeProvider,
            NullLogger<EnquiryPipeline>.Instance);

        var events = new List<AgentEvent>();
        await foreach (var evt in pipeline.StartAsync(enquiryId, CancellationToken.None))
        {
            events.Add(evt);
        }

        events.Should().NotContain(e => e is ErrorEvent, "a real Foundry call should complete cleanly against the worked example");
        events.OfType<StageEvent>().Select(e => e.Stage).Should().ContainInOrder("intake", "resolve", "price");
        events.OfType<ToolStartEvent>().Select(e => e.Name).Should().Contain("resolve_customer");
        var approval = events.OfType<ApprovalRequiredEvent>().Should().ContainSingle().Subject;

        // The worked example's two judgement calls (docs/DOMAIN.md) — the quality bar a cheaper model
        // or a lower reasoning effort has to clear, not just "it answered".
        var request = approval.Payload.Should().BeOfType<ApprovalRequest>().Subject;
        request.PricedQuote.Lines.Select(l => l.Sku).Should().Contain("BRG-6203-2RS", "order history resolves 'same as last time' to the 2RS");
        request.Unresolved.Should().Contain(
            u => u.OriginalDescription.Contains("spindle tape", StringComparison.OrdinalIgnoreCase),
            "'the thicker one' has no purchase history and must stay unresolved, never guessed");
    }

    private sealed class DevDbContextFactory : IDbContextFactory<QuoteDeskDbContext>
    {
        public QuoteDeskDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<QuoteDeskDbContext>().UseSqlServer(DevConnectionString).Options);

        public Task<QuoteDeskDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
