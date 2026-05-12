using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Tests.Helpers;

internal static class TestRootCommand
{
    public static RootCommand Build(
        string domain,
        string action,
        Dictionary<string, object>? @params = null,
        string role = "staff",
        string userId = "emp001") =>
        new(
            Root: $"context('{role}', '{userId}').{domain}.{action}()",
            Domain: domain,
            Action: action,
            Params: @params ?? new Dictionary<string, object>(),
            Context: new RootCommandContext(role, userId, "web", null, DateTimeOffset.UtcNow),
            Meta: new RootCommandMeta("slash", "algorithm", null, $"/{domain} {action}")
        );
}
