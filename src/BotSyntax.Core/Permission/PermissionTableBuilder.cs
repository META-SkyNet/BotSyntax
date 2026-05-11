namespace BotSyntax.Core.Permission;

public sealed class PermissionTableBuilder
{
    private readonly Dictionary<string, HashSet<string>> _rules = new();
    private readonly List<(string child, string parent)> _inheritances = new();

    public PermissionTableBuilder Allow(string role, string domain, string action)
    {
        if (!_rules.ContainsKey(role)) _rules[role] = new HashSet<string>();
        _rules[role].Add($"{domain}.{action}");
        return this;
    }

    public PermissionTableBuilder Inherit(string childRole, string parentRole)
    {
        _inheritances.Add((childRole, parentRole));
        return this;
    }

    public PermissionTable Build()
    {
        // Resolve inheritances (simple single-pass — parent must be defined before child)
        foreach (var (child, parent) in _inheritances)
        {
            if (!_rules.ContainsKey(child))  _rules[child]  = new HashSet<string>();
            if (_rules.TryGetValue(parent, out var parentRules))
                foreach (var rule in parentRules) _rules[child].Add(rule);
        }
        return new PermissionTable(_rules);
    }
}
