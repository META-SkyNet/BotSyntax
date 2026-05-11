using System.Text.RegularExpressions;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Compiler;

public sealed class IntentPattern
{
    private readonly Regex _regex;
    private readonly string _domain;
    private readonly string _action;
    private readonly Func<Match, Dictionary<string, object>>? _extractParams;

    public IntentPattern(string domain, string action, string pattern,
        Func<Match, Dictionary<string, object>>? extractParams = null)
    {
        _domain = domain;
        _action = action;
        _regex  = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        _extractParams = extractParams;
    }

    public CompilerResult? TryMatch(string input, string role, string? userId)
    {
        var match = _regex.Match(input);
        if (!match.Success) return null;

        var @params = _extractParams?.Invoke(match) ?? new Dictionary<string, object>();
        return CompilerResult.Ok(new CompactString(role, userId, _domain, _action, @params));
    }
}
