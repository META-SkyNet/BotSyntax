using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;
using BotSyntax.Core.Parser;

namespace BotSyntax.Core.ChatBox;

public sealed class SystemChatBox : IChatBox
{
    private readonly CompactStringParser _parser = new();

    public CompilerResult Parse(string input, string role, string? userId)
    {
        var parsed = _parser.TryParse(input);
        if (parsed is null)
            return CompilerResult.Fail(CompilerErrorCode.ParseError,
                $"Input is not a valid compact string: {input}");

        if (!string.Equals(parsed.Role, role, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(parsed.UserId, userId, StringComparison.OrdinalIgnoreCase))
            return CompilerResult.Fail(CompilerErrorCode.ContextMismatch,
                $"Role/userId in compact string ('{parsed.Role}', '{parsed.UserId}') " +
                $"does not match session ('{role}', '{userId}')");

        return CompilerResult.Ok(parsed);
    }
}
