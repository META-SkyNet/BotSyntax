using BotSyntax.Core.Models;

namespace BotSyntax.Core.Interfaces;

public interface ICompiler
{
    CompilerResult Compile(string input, string role, string? userId);
}
