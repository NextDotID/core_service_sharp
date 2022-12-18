namespace CoreService.Shared.Injector;

using System.Text.Json;
using System.Text.RegularExpressions;
using CoreService.Api.Injector;
using CoreService.Api.Logging;
using CoreService.Shared.Internals;
using FluentResults;
using Microsoft.Extensions.Logging;

public partial class Injector
{
    private readonly ILogger logger;

    public Injector(ILogger<Injector> logger)
    {
        this.logger = logger;
    }

    public Result<List<InjectionPoint>> Extract(string input)
    {
        // Hashset here to dedup by default.
        var result = new HashSet<InjectionPoint>();
        foreach (var match in InjectPattern().Matches(input).Cast<Match>())
        {
            if (!match.Success || !result.Add(InjectionPoint.FromText(match.ValueSpan[2..^2])))
            {
                logger.ExtractMatchFailed(match.Value);
            }
        }

        return result.ToList();
    }

    public Result<string> Inject(string input, Internal internals, IDictionary<string, string> prompts)
    {
        using var doc = JsonSerializer.SerializeToDocument(internals);

        // Prevent GENERATE from being assigned different values.
        var genDict = new Dictionary<GeneratePoint, string>();

        return InjectPattern().Replace(input, match => InjectionPoint.FromText(match.ValueSpan[2..^2]) switch
        {
            GeneratePoint gp => ResolveGeneratePoint(gp, genDict),
            PromptPoint { Key: var key } => prompts.TryGetValue(key, out var pValue) ? pValue : match.Value,
            InternalPoint { Key: var key } => ResolvePath(doc, key) ?? match.Value,
            _ => match.Value,
        });
    }

    /// <summary>
    /// Validate if all injection points in input are replaced.
    /// </summary>
    /// <param name="input">Text to validate.</param>
    /// <returns><c>True</c> if all replaced.</returns>
    public Result<bool> Validate(string input) => !InjectPattern().Matches(input).Any();

    /// <summary>
    /// General injection point. May contains these 3 types:
    /// <code>{{GENERATE:AN:64:DB_PASSWORD}}</code>
    /// <code>{{PROMPT:DB_PASSWORD}}</code>
    /// <code>{{INTERNAL:SUBKEY_PUB}}</code>
    /// </summary>
    [GeneratedRegex("\\{\\{(.+?)}}")]
    private static partial Regex InjectPattern();

    private static string? ResolvePath(JsonDocument doc, string path)
    {
        var element = doc.RootElement;
        foreach (var part in path.Split('_'))
        {
            if (!element.TryGetProperty(part, out element))
            {
                return null;
            }
        }

        return element.GetString();
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
