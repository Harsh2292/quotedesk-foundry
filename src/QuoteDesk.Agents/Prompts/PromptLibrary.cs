using System.Reflection;

namespace QuoteDesk.Agents.Prompts;

/// <summary>
/// The three system prompts, embedded into the assembly (per CLAUDE.md: they must survive the task 09
/// Docker image, so they are compiled in rather than copied as loose content files) and loaded once
/// here, not inline strings and not read from disk on every request. Reading fails fast in the
/// constructor — a missing prompt is a startup error, not a first-request surprise.
/// </summary>
public sealed class PromptLibrary
{
    public string Intake { get; }
    public string Resolve { get; }
    public string Narrate { get; }

    /// <summary>
    /// The company's quotation policy — the one knowledge source in this system, as opposed to a
    /// tool (task foundry-05). It grounds the narration's <i>explanation</i> of a discount, a freight
    /// waiver or a validity date in a named, versioned rule. It is never a second source of the
    /// arithmetic: every number still comes from <see cref="QuoteDesk.Domain"/>, and the document
    /// says so in its own text so the instruction survives into the model's context.
    /// </summary>
    public string QuotationPolicy { get; }

    /// <summary>
    /// What the Narrate agent is actually instructed with: <see cref="Narrate"/> followed by
    /// <see cref="QuotationPolicy"/>. Composed once here rather than in the pipeline, so there is one
    /// answer to "what does Narrate know" and a test can assert on it directly.
    /// </summary>
    public string NarrateWithPolicy { get; }

    public PromptLibrary()
    {
        Intake = Load("intake.md");
        Resolve = Load("resolve.md");
        Narrate = Load("narrate.md");
        QuotationPolicy = Load("quotation-policy.md");

        NarrateWithPolicy = $"""
            {Narrate}

            ---

            The company's quotation policy follows. Cite it as described above; never compute from it.

            {QuotationPolicy}
            """;
    }

    private static string Load(string fileName)
    {
        var assembly = typeof(PromptLibrary).Assembly;
        var resourceName = $"{assembly.GetName().Name}.Prompts.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded prompt '{resourceName}' not found. Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
