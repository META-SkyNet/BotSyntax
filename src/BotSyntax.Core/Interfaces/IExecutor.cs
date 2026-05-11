using BotSyntax.Core.Models;

namespace BotSyntax.Core.Interfaces;

public interface IExecutor
{
    Task<ExecutionResult> ExecuteAsync(CompactString command, string channel, string? sessionId);
}
