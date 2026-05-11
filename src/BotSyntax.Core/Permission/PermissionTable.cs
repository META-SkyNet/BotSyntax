namespace BotSyntax.Core.Permission;

public sealed class PermissionTable
{
    private readonly Dictionary<string, HashSet<string>> _rules;  // role → "domain.action"

    internal PermissionTable(Dictionary<string, HashSet<string>> rules)
        => _rules = rules;

    public bool IsAllowed(string role, string domain, string action)
    {
        if (!_rules.TryGetValue(role, out var allowed)) return false;
        return allowed.Contains("*.*")
            || allowed.Contains($"*.{action}")
            || allowed.Contains($"{domain}.*")
            || allowed.Contains($"{domain}.{action}");
    }
}
