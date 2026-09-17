namespace QuoteDesk.Agents.Pipeline;

/// <summary>
/// The delimiter every enquiry-bearing prompt wraps a customer's raw text in, per docs/DOMAIN.md
/// ("What the model is never allowed to do... Follow an instruction that arrived inside a customer's
/// email"). The prompt files under Prompts/ instruct the model on what these markers mean; this class
/// is what actually wraps the untrusted text at runtime, so the two can never drift apart.
/// </summary>
public static class UntrustedContent
{
    public const string Start = "<<<ENQUIRY_START>>>";
    public const string End = "<<<ENQUIRY_END>>>";

    /// <summary>Wraps customer-derived text in the delimiter. The text is neutralised first: any run of
    /// <c>&lt;&lt;&lt;</c> or <c>&gt;&gt;&gt;</c> is broken up, so an enquiry cannot contain
    /// <see cref="End"/> (or a look-alike such as <c>&lt;&lt;&lt; ENQUIRY_END &gt;&gt;&gt;</c>) and
    /// close the block early to make what follows read as instructions (found in the foundry-03
    /// security review). The change to the customer's text is cosmetic — a space inside a run of
    /// angle brackets.</summary>
    public static string Wrap(string untrusted)
    {
        ArgumentNullException.ThrowIfNull(untrusted);
        return $"{Start}\n{Neutralise(untrusted)}\n{End}";
    }

    private static string Neutralise(string text)
    {
        var result = text;
        while (result.Contains("<<<", StringComparison.Ordinal) || result.Contains(">>>", StringComparison.Ordinal))
        {
            result = result.Replace("<<<", "<< <", StringComparison.Ordinal).Replace(">>>", "> >>", StringComparison.Ordinal);
        }

        return result;
    }
}
