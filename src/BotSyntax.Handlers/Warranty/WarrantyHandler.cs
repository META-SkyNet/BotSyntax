using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Warranty;

public sealed class WarrantyHandler : IDomainHandler
{
    public string Domain => "warranty";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "status" => ExecutionResult.Ok(MockWarranty(command.Params)),
            "check"  => ExecutionResult.Ok(new { order_id = Str(command.Params, "order"), warranty_code = "BH00456", valid = true, expires_at = "2027-05-01", product = "Điện thoại Model X" }),
            "create" => ExecutionResult.Ok(new { warranty_code = "BH00457", order_id = Str(command.Params, "order_id"), issue = Str(command.Params, "issue"), status = "RECEIVED", message = "Phiếu bảo hành đã được tạo." }),
            "update" => ExecutionResult.Ok(new { warranty_code = Str(command.Params, "warranty_code"), status = Str(command.Params, "status"), message = "Trạng thái bảo hành đã được cập nhật." }),
            "list"   => ExecutionResult.Ok(new { warranties = new[] { MockWarranty(new()) }, total = 1 }),
            _        => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'warranty'")
        });

    private static object MockWarranty(Dictionary<string, object> p) => new
    {
        warranty_code = Str(p, "warranty_code", "BH00456"),
        product       = "Điện thoại Model X",
        sku           = "DT-MODEL-X",
        status        = "REPAIRING",
        received_at   = "2026-05-03",
        issue         = "Màn hình bị vỡ",
        note          = "Đang chờ linh kiện"
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
