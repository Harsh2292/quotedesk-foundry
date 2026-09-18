using System.Reflection;
using FluentAssertions;
using QuoteDesk.Agents.Tools.Results;
using QuoteDesk.Domain;

namespace QuoteDesk.UnitTests.Agents;

/// <summary>
/// "CostPrice and margin appear in no tool result type" and "no EF entity type escapes
/// QuoteDesk.Data" (tasks/task-05-tools.md) — checked by reflecting over the actual result types
/// rather than trusting a review to catch a future addition, in the style of
/// QuoteDesk.UnitTests.Domain.DomainPurityTests and DataLayerBoundaryTests.
/// </summary>
public class ToolResultBoundaryTests
{
    private static IEnumerable<Type> ResultTypes => typeof(CustomerMatch).Assembly.GetTypes()
        .Where(t => t.Namespace == "QuoteDesk.Agents.Tools.Results");

    [Fact]
    public void ToolResultTypes_NeverExposeCostOrMargin()
    {
        var offenders = new List<string>();

        foreach (var type in ResultTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name.Contains("Cost", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Contains("Margin", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add($"{type.Name}.{property.Name}");
                }
            }
        }

        offenders.Should().BeEmpty("no tool result may expose cost price or margin — docs/DOMAIN.md 'What the model is never allowed to do'");
    }

    [Fact]
    public void ToolResultTypes_NeverExposeAnEntityOrTheDomainPricedLine()
    {
        var offenders = new List<string>();

        foreach (var type in ResultTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyType = UnwrapCollection(property.PropertyType);
                if (propertyType.Namespace == "QuoteDesk.Data.Entities" || propertyType == typeof(PricedLine))
                {
                    offenders.Add($"{type.Name}.{property.Name}");
                }
            }
        }

        offenders.Should().BeEmpty("entities never leave QuoteDesk.Data, and PricedLine carries MarginPct which must not reach the model");
    }

    /// <summary>
    /// The tests above scope themselves to <c>QuoteDesk.Agents.Tools.Results</c>, which is what a tool
    /// hands back — but that is not the whole of what reaches a model. <c>PriceExecutor.NarrateAsync</c>
    /// serializes a <see cref="QuoteDesk.Agents.Pipeline.ResolutionResult"/>'s fields and a
    /// <see cref="PricedQuote"/> straight into Narrate's prompt, and the pipeline contracts live in a
    /// different namespace the reflection above never sees — a blind spot found in the foundry-05
    /// boundary review, which `ResolutionResult.CustomerTier` had just walked into unchecked.
    ///
    /// Closing it rather than noting it: the pipeline's own contract types get the same name rule, so
    /// a future field called something like <c>CostPrice</c> or <c>MarginPct</c> on a message that
    /// crosses into a prompt fails here instead of shipping.
    /// </summary>
    [Fact]
    public void PipelineContractTypes_NeverExposeCostOrMargin()
    {
        var contractTypes = typeof(QuoteDesk.Agents.Pipeline.ResolutionResult).Assembly.GetTypes()
            .Where(t => t.Namespace == "QuoteDesk.Agents.Pipeline" && t.IsClass && !t.IsAbstract);

        var offenders = new List<string>();

        foreach (var type in contractTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name.Contains("Cost", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Contains("Margin", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add($"{type.Name}.{property.Name}");
                }
            }
        }

        offenders.Should().BeEmpty("a pipeline contract is serialized into a prompt, so it is as model-facing as a tool result");
    }

    /// <summary>verify_catalogue_term is Intake's tool, and Intake perceives — it must not be able to
    /// identify a part or see a price. <see cref="CatalogCandidate"/> legitimately carries a SKU for
    /// Resolve, so this cannot be a rule over every result type; it is checked on this one directly.</summary>
    [Fact]
    public void CatalogueTermCheck_HasNoSkuPriceOrCostProperty()
    {
        var offenders = typeof(CatalogueTermCheck).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .Where(n => n.Contains("Sku", StringComparison.OrdinalIgnoreCase)
                || n.Contains("Price", StringComparison.OrdinalIgnoreCase)
                || n.Contains("Cost", StringComparison.OrdinalIgnoreCase))
            .ToList();

        offenders.Should().BeEmpty("Intake confirms a word exists in the catalogue; it never identifies or prices a part");
    }

    private static Type UnwrapCollection(Type type)
    {
        if (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return type.GetGenericArguments().Single();
        }

        return type;
    }
}
