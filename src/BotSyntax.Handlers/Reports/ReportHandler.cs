using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Reports;

public sealed class ReportHandler : IDomainHandler
{
    public string Domain => "report";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "today"          => ExecutionResult.Ok(new { date = "2026-05-11", new_orders = 24, revenue = 7200000, pending = 3, low_stock = 2, returns_pending = 1 }),
            "revenue"        => ExecutionResult.Ok(new { date = Str(command.Params, "date", "11/05/2026~11/05/2026"), total = 7200000, orders = 24, avg_order_value = 300000 }),
            "top-products"   => ExecutionResult.Ok(new { date = Str(command.Params, "date"), products = new[] { new { sku = "AO-HOODIE-L-DEN", name = "Áo Hoodie Đen L", sold = 12, revenue = 3588000 } }, total = 1 }),
            "pending-orders" => ExecutionResult.Ok(new { orders = new[] { new { order_id = "10231", status = "PENDING", days_waiting = 4 } }, total = 1 }),
            "returns"        => ExecutionResult.Ok(new { date = Str(command.Params, "date"), total_returns = 3, exchange = 2, refund = 1, total_amount = 598000 }),
            "daily"          => ExecutionResult.Ok(new { date = "2026-05-11", generated_at = DateTimeOffset.UtcNow.ToString("o"), summary = "Báo cáo ngày 11/05/2026 đã được tạo.", orders = 24, revenue = 7200000 }),
            _                => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'report'")
        });

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
