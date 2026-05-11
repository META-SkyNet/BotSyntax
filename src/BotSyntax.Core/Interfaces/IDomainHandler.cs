using BotSyntax.Core.Models;

namespace BotSyntax.Core.Interfaces;

public interface IDomainHandler
{
    string Domain { get; }
    Task<ExecutionResult> HandleAsync(RootCommand command);
}
