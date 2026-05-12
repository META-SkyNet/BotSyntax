using System.Globalization;
using System.Text.RegularExpressions;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Parser;

public sealed class CompactStringParser
{
    // context('role', 'userId').domain.action(params)
    // context('role', guest).domain.action(params)
    private static readonly Regex _pattern = new(
        @"^context\(['""](?<role>[^'""]+)['""]\s*,\s*(?:guest|['""](?<userId>[^'""]*)['""])\)\." +
        @"(?<domain>[a-z]+)\.(?<action>[a-z][a-z-]*)\((?<params>[^)]*)\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CompilerResult Parse(string input)
    {
        var match = _pattern.Match(input.Trim());
        if (!match.Success)
            return CompilerResult.Fail(CompilerErrorCode.InvalidParam,
                $"Invalid compact string format: '{input}'");

        var role      = match.Groups["role"].Value;
        var userId    = match.Groups["userId"].Success ? match.Groups["userId"].Value : null;
        var domain    = match.Groups["domain"].Value;
        var action    = match.Groups["action"].Value;
        var rawParams = match.Groups["params"].Value.Trim();

        var parseResult = ParseParams(rawParams);
        if (parseResult.IsFailure)
            return CompilerResult.Fail(CompilerErrorCode.InvalidParam, parseResult.ErrorMessage!);

        return CompilerResult.Ok(new CompactString(role, userId, domain, action, parseResult.Params!));
    }

    // Returns CompactString? for callers that prefer null over CompilerResult
    public CompactString? TryParse(string input)
    {
        var result = Parse(input);
        return result.IsSuccess ? result.Value : null;
    }

    private static (bool IsFailure, string? ErrorMessage, Dictionary<string, object>? Params) ParseParams(string raw)
    {
        var dict = new Dictionary<string, object>();
        if (string.IsNullOrWhiteSpace(raw))
            return (false, null, dict);

        foreach (var token in SplitParams(raw))
        {
            var kv = token.Split(':', 2);
            if (kv.Length != 2)
                return (true, $"Invalid param token: '{token}'", null);

            var key   = kv[0].Trim();
            var value = kv[1].Trim();

            if ((value.StartsWith("'") && value.EndsWith("'")) ||
                (value.StartsWith("\"") && value.EndsWith("\"")))
                dict[key] = value[1..^1];
            else if (int.TryParse(value, out var intVal))
                dict[key] = intVal;
            else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblVal))
                dict[key] = dblVal;
            else
                dict[key] = value;
        }
        return (false, null, dict);
    }

    private static IEnumerable<string> SplitParams(string raw)
    {
        var tokens    = new List<string>();
        var inQuote   = false;
        char quoteChar = '\0';
        var current   = new System.Text.StringBuilder();

        foreach (var c in raw)
        {
            if (!inQuote && (c == '\'' || c == '"')) { inQuote = true; quoteChar = c; }
            else if (inQuote && c == quoteChar)      { inQuote = false; }
            else if (!inQuote && c == ',')           { tokens.Add(current.ToString().Trim()); current.Clear(); continue; }
            current.Append(c);
        }
        if (current.Length > 0) tokens.Add(current.ToString().Trim());
        return tokens;
    }
}
