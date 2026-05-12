using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Customer;

public sealed class CustomerHandler : IDomainHandler
{
    public string Domain => "customer";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "info"      => ExecutionResult.Ok(MockCustomer(command.Params)),
            "note"      => ExecutionResult.Ok(new { customer_id = Str(command.Params, "customer_id"), note_added = true, message = "Ghi chú đã được thêm." }),
            "orders"    => ExecutionResult.Ok(new { customer_id = Str(command.Params, "customer_id"), orders = new[] { new { order_id = "10234", status = "DELIVERED", total = 299000, date = "2026-05-01" } }, total = 1 }),
            "search"    => ExecutionResult.Ok(new { keyword = Str(command.Params, "keyword"), results = new[] { MockCustomer(new()) }, total = 1 }),
            "complain"  => ExecutionResult.Ok(new { complaint_id = "KN001", customer_id = command.Context.Identity ?? "guest", status = "OPEN", message = "Khiếu nại đã được ghi nhận, nhân viên sẽ liên hệ sớm nhất." }),
            "complaint" => HandleComplaint(command.Params),
            _           => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'customer'")
        });

    private static ExecutionResult HandleComplaint(Dictionary<string, object> p) =>
        Str(p, "subaction", "") switch
        {
            "create" => ExecutionResult.Ok(new { complaint_id = "KN002", customer_id = Str(p, "customer_id"), status = "OPEN", message = "Khiếu nại đã được tạo." }),
            "status" => ExecutionResult.Ok(new { complaint_id = Str(p, "complaint_id", "KN001"), status = "PROCESSING", created_at = "2026-05-09", assigned_to = "emp002" }),
            "close"  => ExecutionResult.Ok(new { complaint_id = Str(p, "complaint_id", "KN001"), status = "CLOSED", resolution = Str(p, "resolution"), closed_at = "2026-05-11" }),
            _        => ExecutionResult.Ok(new { complaint_id = Str(p, "complaint_id", "KN001"), status = "OPEN" })
        };

    private static object MockCustomer(Dictionary<string, object> p) => new
    {
        customer_id  = Str(p, "customer_id", "KH001"),
        name         = "Nguyễn Văn A",
        phone        = "0912345678",
        email        = "nguyenvana@example.com",
        total_orders = 5,
        total_spent  = 1495000,
        tier         = "silver",
        note         = "Khách VIP, ưu tiên xử lý"
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
