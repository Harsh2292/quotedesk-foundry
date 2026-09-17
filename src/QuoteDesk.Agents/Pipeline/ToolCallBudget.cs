namespace QuoteDesk.Agents.Pipeline;

/// <summary>Enforces "max N tool calls per run, then a forced summary" (tasks/task-06). One instance
/// per run, shared by every tool <see cref="TracedAIFunction"/> wraps for that run.
///
/// A budget can have a <paramref name="parent"/>: a call must then fit within <b>both</b> this
/// budget's own cap and the parent's. That is how the Intake agent is capped (task foundry-03 review):
/// Intake gets a small child budget of the run's shared budget, so its calls still count toward the
/// run's total, but it can never spend more than its own few — leaving Resolve, the stage that needs
/// the lookups, a guaranteed share instead of whatever Intake happened to leave behind.</summary>
public sealed class ToolCallBudget(int max, ToolCallBudget? parent = null)
{
    private int _count;

    public int Max { get; } = max;

    public int Count => _count;

    /// <summary>True if this call is within budget — this budget's own cap and, when present, the
    /// parent's — and may proceed. A call refused by its own cap never touches the parent.</summary>
    public bool TryReserve() => Interlocked.Increment(ref _count) <= Max && (parent?.TryReserve() ?? true);
}
