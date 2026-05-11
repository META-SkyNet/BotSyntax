namespace BotSyntax.Core.Compiler;

internal static class DefaultPatterns
{
    public static readonly IEnumerable<IntentPattern> All = new[]
    {
        // Returns — must come BEFORE order-status so "hoàn tiền đơn \d+" hits returns, not order
        // Also matches "muốn trả" (word "trả" preceded by muốn, not necessarily followed by hàng)
        new IntentPattern("returns", "create",
            @"(?:muốn|muon)\s+(?:trả|tra)|(?:trả|tra)\s+(?:hàng|hang)|(?:hoàn tiền|hoan tien)|(?:đổi|doi)\s+(?:hàng|hang)"),

        // Order status — with numeric ID
        new IntentPattern("order", "status",
            @"(?:đơn|don|dh)\s+(\d+)|(?:kiểm tra|kiem tra)\s+(?:đơn|don)\s+(\d+)",
            m =>
            {
                var id = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                return new() { ["order_id"] = int.Parse(id) };
            }),

        // Order status — without ID (general inquiry)
        new IntentPattern("order", "status",
            @"(?:đơn|don)\s+(?:hàng|hang)?\s*(?:của tôi|cua toi|đang ở đâu|dang o dau|ở đâu|o dau|rồi|roi|như thế nào|nhu the nao)"),

        // Warranty
        new IntentPattern("warranty", "status",
            @"(?:bảo hành|bao hanh|bh)\s*(\w+)?",
            m => m.Groups[1].Success
                ? new() { ["warranty_code"] = m.Groups[1].Value }
                : new()),

        // Product search
        new IntentPattern("product", "search",
            @"(?:tìm|tim|tìm kiếm|tim kiem)\s+(.+)",
            m => new() { ["keyword"] = m.Groups[1].Value.Trim() }),

        new IntentPattern("product", "search",
            @"(?:gợi ý|goi y|recommend)\s+(.+)",
            m => new() { ["keyword"] = m.Groups[1].Value.Trim() }),

        // Shipping
        new IntentPattern("ship", "status",
            @"(?:vận đơn|van don|giao hàng|giao hang)\s*(\d+)?|(?:shipper|hàng tôi|hang toi)",
            m => m.Groups[1].Success
                ? new() { ["order_id"] = int.Parse(m.Groups[1].Value) }
                : new()),

        // Cart
        new IntentPattern("cart", "view",
            @"(?:giỏ hàng|gio hang|giỏ của tôi|gio cua toi)"),

        // Support
        new IntentPattern("customer", "complain",
            @"(?:khiếu nại|khieu nai|phản ánh|phan anh|không hài lòng|khong hai long|liên hệ|lien he)"),
    };
}
