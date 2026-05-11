using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;
using BotSyntax.Core.Permission;

namespace BotSyntax.Core.Executor;

public sealed class BotExecutor : IExecutor
{
    private readonly PermissionTable _permissions;
    private readonly Dictionary<string, IDomainHandler> _handlers;

    public BotExecutor(PermissionTable permissions, IEnumerable<IDomainHandler> handlers)
    {
        _permissions = permissions;
        _handlers    = handlers.ToDictionary(h => h.Domain, h => h);
    }

    public async Task<ExecutionResult> ExecuteAsync(
        CompactString command, string channel, string? sessionId)
    {
        if (!_permissions.IsAllowed(command.Role, command.Domain, command.Action))
            return ExecutionResult.Denied(
                $"Role '{command.Role}' không có quyền thực hiện {command.Domain}.{command.Action}");

        if (!_handlers.TryGetValue(command.Domain, out var handler))
            return ExecutionResult.NotFound($"Domain handler '{command.Domain}' chưa được đăng ký");

        var root = new RootCommand(
            Root:    command.ToText(),
            Domain:  command.Domain,
            Action:  command.Action,
            Params:  command.Params,
            Context: new RootCommandContext(
                Role:      command.Role,
                Identity:  command.UserId,
                Channel:   channel,
                SessionId: sessionId,
                Timestamp: DateTimeOffset.UtcNow),
            Meta: new RootCommandMeta(
                InputType:  "compact",
                Compiler:   "algorithm",
                Confidence: null,
                RawInput:   command.ToText()));

        return await handler.HandleAsync(root);
    }
}
