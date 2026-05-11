using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.ChatBox;

public sealed class NLUChatBox : IChatBox
{
    private readonly ICompiler _compiler;

    public NLUChatBox(ICompiler compiler)
        => _compiler = compiler;

    public CompilerResult Parse(string input, string role, string? userId)
        => _compiler.Compile(input, role, userId);
}
