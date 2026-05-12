using System.Globalization;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Compiler;

public sealed class SlashCommandCompiler
{
    // Positional param names per domain.action (first positional arg)
    private static readonly Dictionary<string, string> _positionalParams = new()
    {
        ["order.status"]     = "order_id",
        ["order.confirm"]    = "order_id",
        ["order.cancel"]     = "order_id",
        ["order.note"]       = "order_id",
        ["order.update"]     = "order_id",
        ["ship.status"]      = "order_id",
        ["ship.fail"]        = "order_id",
        ["ship.address"]     = "order_id",
        ["returns.status"]   = "return_id",
        ["returns.approve"]  = "return_id",
        ["returns.reject"]   = "return_id",
        ["product.info"]     = "sku",
        ["product.stock"]    = "sku",
        ["warranty.status"]  = "warranty_code",
        ["debt.confirm"]     = "debt_id",
        ["customer.info"]    = "customer_id",
        ["customer.orders"]  = "customer_id",
        ["customer.note"]    = "customer_id",
        ["customer.search"]    = "keyword",
        ["stock.import"]       = "subaction",
        ["customer.complaint"] = "subaction",
    };

    public CompilerResult Compile(string input, string role, string? userId)
    {
        if (!input.StartsWith("/"))
            return CompilerResult.Fail(CompilerErrorCode.UnknownIntent, "Slash commands must start with '/'");

        var tokens = input[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
            return CompilerResult.Fail(CompilerErrorCode.MissingParam, "Command requires at least domain and action");

        var domain = tokens[0].ToLower();
        var action = tokens[1].ToLower();
        var key    = $"{domain}.{action}";
        var @params = new Dictionary<string, object>();

        for (var i = 2; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.StartsWith("--"))
            {
                var kv = token[2..].Split('=', 2);
                if (kv.Length == 2)
                    @params[kv[0]] = TryParse(kv[1].Trim('"'));
                else
                    @params[kv[0]] = true;
            }
            else if (i == 2 && _positionalParams.TryGetValue(key, out var paramName))
            {
                @params[paramName] = TryParse(token);
            }
        }

        return CompilerResult.Ok(new CompactString(role, userId, domain, action, @params));
    }

    private static object TryParse(string value) =>
        int.TryParse(value, out var i) ? i :
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d :
        (object)value;
}
