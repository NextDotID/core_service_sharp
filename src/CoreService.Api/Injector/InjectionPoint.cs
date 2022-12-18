namespace CoreService.Api.Injector;

using Open.Text;

public enum GenerateCategory
{
    AlphaNumeric,
    Base64,
}

public abstract record InjectionPoint
{
    public static InjectionPoint FromText(ReadOnlySpan<char> text)
    {
        var parts = text.Split(':', StringSplitOptions.TrimEntries);
        return parts[0] switch
        {
            "GENERATE" => ParseGeneratePoint(parts),
            "PROMPT" => new PromptPoint(parts[1]),
            "INTERNAL" => new InternalPoint(parts[1].ToString()),
            _ => throw new ArgumentException($"Invalid injection point type: {text}"),
        };
    }

    private static GeneratePoint ParseGeneratePoint(IReadOnlyList<string> parts) => parts[1] switch
    {
        "AN" => new GeneratePoint(GenerateCategory.AlphaNumeric, int.Parse(parts[2]), parts[3].ToString()),
        "BASE64" => new GeneratePoint(GenerateCategory.Base64, int.Parse(parts[2]), parts[3].ToString()),
        _ => throw new ArgumentException("Invalid generate category"),
    };
}

public record GeneratePoint(GenerateCategory Category, int Length, string Key) : InjectionPoint();

public record PromptPoint(string Key) : InjectionPoint();

public record InternalPoint(string Key) : InjectionPoint();
