using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Inventory;

public sealed class StockHandler : IDomainHandler
{
    public string Domain => "stock";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "update" => ExecutionResult.Ok(new { sku = Str(command.Params, "sku"), quantity_change = Str(command.Params, "quantity"), new_quantity = 97, message = "Tồn kho đã được cập nhật." }),
            "import" => HandleImport(command.Params),
            "low"    => ExecutionResult.Ok(new { items = new[] { new { sku = "DT-MODEL-X", name = "Điện thoại Model X", quantity = 3, threshold = 10 } }, total = 1 }),
            _        => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'stock'")
        });

    private static ExecutionResult HandleImport(Dictionary<string, object> p) =>
        Str(p, "subaction", "") switch
        {
            "create"  => ExecutionResult.Ok(new { import_id = "NK001", supplier = Str(p, "supplier"), date = Str(p, "date"), status = "DRAFT", message = "Phiếu nhập đã được tạo." }),
            "add"     => ExecutionResult.Ok(new { import_id = Str(p, "import_id"), sku = Str(p, "sku"), quantity = Str(p, "quantity"), message = "Sản phẩm đã được thêm vào phiếu nhập." }),
            "confirm" => ExecutionResult.Ok(new { import_id = Str(p, "import_id"), status = "CONFIRMED", items_imported = 2, message = "Phiếu nhập đã được xác nhận." }),
            "list"    => ExecutionResult.Ok(new { imports = new[] { new { import_id = "NK001", supplier = "Công ty ABC", date = "2026-05-08", status = "CONFIRMED", items = 5 } }, total = 1 }),
            _         => ExecutionResult.Ok(new { import_id = Str(p, "import_id", "NK001"), status = "DRAFT", items = Array.Empty<object>() })
        };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
