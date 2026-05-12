using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Orders;

public sealed class OrderHandler : IDomainHandler
{
    public string Domain => "order";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "status"  => ExecutionResult.Ok(MockOrder(command.Params)),
            "list"    => ExecutionResult.Ok(new { orders = new[] { MockOrder(new()) }, total = 1 }),
            "confirm" => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), status = "CONFIRMED",  message = "Đơn hàng đã được xác nhận." }),
            "cancel"  => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), status = "CANCELLED",  message = "Đơn hàng đã bị hủy." }),
            "update"  => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), status = Str(command.Params, "status"), message = "Trạng thái đã được cập nhật." }),
            "note"    => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), note_added = true, message = "Ghi chú đã được thêm." }),
            _         => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'order'")
        });

    private static object MockOrder(Dictionary<string, object> p) => new
    {
        order_id   = Str(p, "order_id", "10234"),
        status     = "SHIPPING",
        created_at = "2026-05-01",
        items      = new[] { new { sku = "AO-HOODIE-L-DEN", name = "Áo Hoodie Đen L", qty = 1, price = 299000 } },
        total      = 299000,
        address    = "123 Nguyễn Huệ, Q1, HCM",
        carrier    = "GHN",
        tracking   = "GHN123456789"
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
