using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Shipping;

public sealed class ShipHandler : IDomainHandler
{
    public string Domain => "ship";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "status"  => ExecutionResult.Ok(MockTracking(command.Params)),
            "assign"  => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), carrier = Str(command.Params, "carrier"), message = "Đã gán đơn vị vận chuyển." }),
            "fail"    => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), reason = Str(command.Params, "reason"), action = Str(command.Params, "action", "retry"), message = "Đã ghi nhận giao thất bại." }),
            "address" => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), new_address = Str(command.Params, "address"), updated = true }),
            "list"    => ExecutionResult.Ok(new { shipments = new[] { MockTracking(new()) }, total = 1 }),
            _         => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'ship'")
        });

    private static object MockTracking(Dictionary<string, object> p) => new
    {
        order_id        = Str(p, "order_id", "10234"),
        carrier         = "GHN",
        tracking_number = "GHN123456789",
        status          = "SHIPPING",
        estimated_at    = "2026-05-12",
        last_event      = new { time = "2026-05-11T08:00:00", location = "Bưu cục Q1, HCM", note = "Đã lấy hàng" }
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
