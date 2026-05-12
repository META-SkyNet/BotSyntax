using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Cart;

public sealed class CartHandler : IDomainHandler
{
    public string Domain => "cart";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "add"      => ExecutionResult.Ok(new { sku = Str(command.Params, "sku"), quantity = 1, cart_total = 299000, items_count = 1, message = "Đã thêm sản phẩm vào giỏ hàng." }),
            "view"     => ExecutionResult.Ok(MockCart()),
            "voucher"  => ExecutionResult.Ok(new { voucher_code = Str(command.Params, "voucher_code"), discount = 50000, cart_total_after = 249000, message = "Mã giảm giá đã được áp dụng." }),
            "payment"  => ExecutionResult.Ok(new { methods = new[] { new { code = "COD", name = "Thanh toán khi nhận hàng" }, new { code = "MOMO", name = "Ví MoMo" }, new { code = "ZALOPAY", name = "ZaloPay" }, new { code = "BANK_TRANSFER", name = "Chuyển khoản ngân hàng" }, new { code = "VNPAY", name = "VNPay" }, new { code = "CREDIT_CARD", name = "Thẻ tín dụng / ghi nợ" } } }),
            "checkout" => ExecutionResult.Ok(new { order_id = "10235", status = "PENDING", total = 299000, payment_method = Str(command.Params, "method", "COD"), message = "Đơn hàng đã được đặt thành công!" }),
            _          => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'cart'")
        });

    private static object MockCart() => new
    {
        items       = new[] { new { sku = "AO-HOODIE-L-DEN", name = "Áo Hoodie Đen L", qty = 1, price = 299000 } },
        items_count = 1,
        subtotal    = 299000,
        discount    = 0,
        total       = 299000,
        voucher     = (string?)null
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
