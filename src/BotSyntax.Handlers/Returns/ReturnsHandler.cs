using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Returns;

public sealed class ReturnsHandler : IDomainHandler
{
    public string Domain => "returns";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "create"  => ExecutionResult.Ok(new { return_id = "TRA001", order_id = Str(command.Params, "order_id"), type = Str(command.Params, "type", "exchange"), status = "REQUESTED", message = "Yêu cầu đổi trả đã được tạo." }),
            "status"  => ExecutionResult.Ok(MockReturn(command.Params)),
            "approve" => ExecutionResult.Ok(new { return_id = Str(command.Params, "return_id"), status = "APPROVED", message = "Yêu cầu đổi trả đã được duyệt." }),
            "reject"  => ExecutionResult.Ok(new { return_id = Str(command.Params, "return_id"), status = "REJECTED", reason = Str(command.Params, "reason"), message = "Yêu cầu đã bị từ chối." }),
            _         => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'returns'")
        });

    private static object MockReturn(Dictionary<string, object> p) => new
    {
        return_id  = Str(p, "return_id", "TRA001"),
        order_id   = "10234",
        type       = "exchange",
        status     = "PROCESSING",
        created_at = "2026-05-05",
        sku        = "AO-HOODIE-L-DEN",
        reason     = "Sai size"
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
