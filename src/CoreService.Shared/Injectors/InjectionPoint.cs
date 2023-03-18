namespace CoreService.Shared.Injectors;

using System.Text.Json.Serialization;

public enum GenerateCategory
{
    AlphaNumeric,
    Base64,
    Hex,
}

public enum InternalTransform
{
    None,
    Hex,
    Base64,
    Base58,
}

[JsonDerivedType(typeof(GeneratePoint), nameof(GeneratePoint))]
[JsonDerivedType(typeof(PromptPoint), nameof(PromptPoint))]
[JsonDerivedType(typeof(InternalPoint), nameof(InternalPoint))]
public abstract record InjectionPoint
{
    public static InjectionPoint FromText(string text)
    {
        var parts = text.Split(':', StringSplitOptions.TrimEntries);
        return parts[0] switch
        {
            "GENERATE" => ParseGeneratePoint(parts),
            "PROMPT" => new PromptPoint(parts[1]),
            "INTERNAL" => ParseInternalPoint(parts),
            _ => throw new ArgumentException($"Invalid injection point type: {text}"),
        };
    }

    private static GeneratePoint ParseGeneratePoint(IReadOnlyList<string> parts) => parts[1] switch
    {
        "AN" => new GeneratePoint(GenerateCategory.AlphaNumeric, int.Parse(parts[2]), parts[3].ToString()),
        "BASE64" => new GeneratePoint(GenerateCategory.Base64, int.Parse(parts[2]), parts[3].ToString()),
        "HEX" => new GeneratePoint(GenerateCategory.Hex, int.Parse(parts[2]), parts[3].ToString()),
        _ => throw new ArgumentException("Invalid generate category"),
    };

    private static InternalPoint ParseInternalPoint(IReadOnlyList<string> parts) => parts.ElementAtOrDefault(2) switch
    {
        "HEX" => new InternalPoint(parts[1].ToString(), InternalTransform.Hex),
        "BASE64" => new InternalPoint(parts[1].ToString(), InternalTransform.Base64),
        "BASE58" => new InternalPoint(parts[1].ToString(), InternalTransform.Base58),
        _ => new InternalPoint(parts[1].ToString(), InternalTransform.None),
    };
}

public record GeneratePoint(GenerateCategory Category, int Length, string Key) : InjectionPoint();

public record PromptPoint(string Key) : InjectionPoint();

public record InternalPoint(string Key, InternalTransform Transform) : InjectionPoint();
