namespace CoreService.Api.Injectors;

using System.Text.Json;
using System.Text.RegularExpressions;
using CoreService.Api.Logging;
using CoreService.Shared.Injectors;
using CoreService.Shared.Internals;
using Microsoft.Extensions.Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using SimpleBase;

public partial class Injector
{
    private readonly ILogger logger;

    public Injector(ILogger<Injector> logger)
    {
        this.logger = logger;
    }

    public List<InjectionPoint> Extract(string input)
    {
        // Hashset here to dedup by default.
        var result = new HashSet<InjectionPoint>();
        foreach (var match in InjectPattern().Matches(input).Cast<Match>())
        {
            if (!match.Success || !result.Add(InjectionPoint.FromText(match.Value[2..^2])))
            {
                logger.ExtractMatchFailed(match.Value);
            }
        }

        return result.ToList();
    }

    public string Inject(string input, Internal internals, IDictionary<string, string> prompts)
    {
        // Prevent GENERATE from being assigned different values.
        var genDict = new Dictionary<GeneratePoint, string>();
        using var doc = JsonSerializer.SerializeToDocument(internals);

        return InjectPattern().Replace(input, match => InjectionPoint.FromText(match.Value[2..^2]) switch
        {
            GeneratePoint gp => ResolveGeneratePoint(gp, genDict),
            PromptPoint { Key: var key } => prompts.TryGetValue(key, out var pValue) ? pValue : match.Value,
            InternalPoint { Key: var key, Transform: var transform } => ResolveInternal(doc, key, transform) ?? match.Value,
            _ => match.Value,
        });
    }

    /// <summary>
    /// Validate if all injection points in input are replaced.
    /// </summary>
    /// <param name="input">Text to validate.</param>
    /// <param name="point">If all replaced, return the first match.</param>
    /// <returns><c>True</c> if all replaced.</returns>
    public bool Validate(string input, out string point)
    {
        var firstMatch = InjectPattern().Matches(input).FirstOrDefault();
        point = firstMatch?.Value ?? string.Empty;
        return !(firstMatch?.Success ?? false);
    }

    /// <summary>
    /// General injection point. May contains these 3 types:
    /// <code>{{GENERATE:AN:64:DB_PASSWORD}}</code>
    /// <code>{{PROMPT:DB_PASSWORD}}</code>
    /// <code>{{INTERNAL:SUBKEY_PRIVATE(:HEX)}}</code>
    /// </summary>
    [GeneratedRegex("\\{\\{(.+?)}}")]
    private static partial Regex InjectPattern();

    private static string? ResolveInternal(JsonDocument doc, string path, InternalTransform transform)
    {
        var element = doc.RootElement;
        foreach (var part in path.Split('_'))
        {
            if (!element.TryGetProperty(part, out element))
            {
                return null;
            }
        }

        return transform switch
        {
            InternalTransform.None => element.GetString(),
            InternalTransform.Hex => element.GetString(),
            InternalTransform.Base58 => Base58.Bitcoin.Encode(element.GetString().HexToByteArray()),
            InternalTransform.Base64 => Convert.ToBase64String(element.GetString().HexToByteArray()),
            _ => throw new NotImplementedException(),
        };
    }

    private static string ResolveGeneratePoint(GeneratePoint gp, Dictionary<GeneratePoint, string> dict)
    {
        if (dict.TryGetValue(gp, out var value))
        {
            return value;
        }

        dict[gp] = gp.Category switch
        {
            GenerateCategory.AlphaNumeric => Generator.AlphaNumeric(gp.Length),
            GenerateCategory.Base64 => Generator.Base64(gp.Length),
            _ => string.Empty,
        };

        return dict[gp];
    }
}
