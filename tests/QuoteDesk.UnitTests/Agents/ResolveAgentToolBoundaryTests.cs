using System.Reflection;
using FluentAssertions;
using QuoteDesk.Agents.Pipeline;
using QuoteDesk.Agents.Tools;

namespace QuoteDesk.UnitTests.Agents;

/// <summary>
/// "A test proves the Resolve agent cannot invoke create_quote_draft" (tasks/task-06-agents-workflow.md),
/// widened in task foundry-03 to cover both agents — checked statically here, the same
/// reflection-over-the-actual-boundary style as ToolRegistryTests/ToolResultBoundaryTests: each
/// tool-using executor is constructed from a bare list of read tools, never from
/// <see cref="WriteToolRegistry"/> or <see cref="QuoteWriteTools"/>, so there is no path from either
/// agent to a write tool for any prompt or model behaviour to exploit. The complementary runtime check
/// — that the tools actually handed to the model exclude the write tool names — lives in the
/// integration tests against a stubbed IChatClient.
/// </summary>
public class ResolveAgentToolBoundaryTests
{
    [Theory]
    [InlineData(typeof(IntakeExecutor))]
    [InlineData(typeof(ResolveExecutor))]
    public void AgentExecutor_HasNoConstructorOrFieldDependencyOnWriteTools(Type executorType)
    {
        var ctorParameterTypes = executorType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        var fieldTypes = executorType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(f => f.FieldType);

        var offending = ctorParameterTypes.Concat(fieldTypes)
            .Where(t => t == typeof(WriteToolRegistry) || t == typeof(QuoteWriteTools))
            .Select(t => t.Name)
            .ToList();

        offending.Should().BeEmpty("no agent may be constructed with a path to a write tool — {0}", executorType.Name);
    }
}
