using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Products;

public sealed class ProductHandler : IDomainHandler
{
    public string Domain => "product";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "search" => ExecutionResult.Ok(new { keyword = Str(command.Params, "keyword"), results = new[] { MockProduct() }, total = 1 }),
            "info"   => ExecutionResult.Ok(MockProduct()),
            "update" => ExecutionResult.Ok(new { sku = Str(command.Params, "sku"), updated = true, message = "Sản phẩm đã được cập nhật." }),
            "stock"  => ExecutionResult.Ok(new { sku = Str(command.Params, "sku"), quantity = 47, reserved = 3, available = 44, status = "IN_STOCK" }),
            _        => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'product'")
        });

    private static object MockProduct() => new
    {
        sku      = "AO-HOODIE-L-DEN",
        name     = "Áo Hoodie Đen Size L",
        price    = 299000,
        status   = "active",
        category = "Áo",
        variants = new[] { new { size = "S", qty = 10 }, new { size = "M", qty = 15 }, new { size = "L", qty = 22 } }
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
