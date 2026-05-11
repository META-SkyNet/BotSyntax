namespace BotSyntax.Core.Models;

public sealed record CompactString(
    string Role,
    string? UserId,
    string Domain,
    string Action,
    Dictionary<string, object> Params)
{
    public bool IsGuest => UserId is null;

    public string ToText()
    {
        var identity = IsGuest ? "guest" : $"'{UserId}'";
        var paramStr = Params.Count == 0
            ? ""
            : string.Join(", ", Params.Select(p => $"{p.Key}: {FormatValue(p.Value)}"));
        return $"context('{Role}', {identity}).{Domain}.{Action}({paramStr})";
    }

    private static string FormatValue(object value) =>
        value is string s ? $"'{s}'" : value.ToString()!;
}
