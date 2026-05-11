using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Compiler;

public sealed class AlgorithmNLUCompiler : ICompiler
{
    private readonly List<IntentPattern> _patterns;

    public AlgorithmNLUCompiler(IEnumerable<IntentPattern> patterns)
        => _patterns = patterns.ToList();

    public CompilerResult Compile(string input, string role, string? userId)
    {
        var normalized = input.Trim().ToLower();

        foreach (var pattern in _patterns)
        {
            var result = pattern.TryMatch(normalized, role, userId);
            if (result is not null) return result;
        }

        return CompilerResult.Fail(CompilerErrorCode.UnknownIntent,
            "Không hiểu yêu cầu. Bạn có thể mô tả rõ hơn không?");
    }

    public static AlgorithmNLUCompiler CreateDefault() => new(DefaultPatterns.All);
}
