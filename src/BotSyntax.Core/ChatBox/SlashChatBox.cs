using BotSyntax.Core.Compiler;
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.ChatBox;

public sealed class SlashChatBox : IChatBox
{
    private readonly SlashCommandCompiler _compiler = new();

    public CompilerResult Parse(string input, string role, string? userId)
        => _compiler.Compile(input, role, userId);
}
