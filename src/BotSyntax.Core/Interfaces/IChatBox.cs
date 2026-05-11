using BotSyntax.Core.Models;

namespace BotSyntax.Core.Interfaces;

public interface IChatBox
{
    CompilerResult Parse(string input, string role, string? userId);
}
