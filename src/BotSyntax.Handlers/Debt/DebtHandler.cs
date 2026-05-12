using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Debt;

public sealed class DebtHandler : IDomainHandler
{
    public string Domain => "debt";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "list"      => ExecutionResult.Ok(new { debts = new[] { MockDebt() }, total = 1, total_unpaid = 5000000 }),
            "confirm"   => ExecutionResult.Ok(new { debt_id = Str(command.Params, "debt_id"), amount_confirmed = Str(command.Params, "amount"), status = "paid", confirmed_at = "2026-05-11", message = "Đã ghi nhận thanh toán." }),
            "reconcile" => ExecutionResult.Ok(new { carrier = Str(command.Params, "carrier"), date = Str(command.Params, "date"), total_cod = 8500000, collected = 8200000, difference = 300000, orders = 12 }),
            "create"    => ExecutionResult.Ok(new { debt_id = "CN002", partner = Str(command.Params, "partner"), amount = Str(command.Params, "amount"), due = Str(command.Params, "due"), status = "unpaid", message = "Phiếu công nợ đã được tạo." }),
            _           => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'debt'")
        });

    private static object MockDebt() => new
    {
        debt_id    = "CN001",
        partner    = "GHN",
        amount     = 5000000,
        due        = "2026-05-15",
        status     = "unpaid",
        created_at = "2026-05-01"
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
