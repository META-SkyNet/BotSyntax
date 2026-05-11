namespace BotSyntax.Core.Models;

public sealed record RootCommand(
    string Root,
    string Domain,
    string Action,
    Dictionary<string, object> Params,
    RootCommandContext Context,
    RootCommandMeta Meta);

public sealed record RootCommandContext(
    string Role,
    string? Identity,
    string Channel,
    string? SessionId,
    DateTimeOffset Timestamp);

public sealed record RootCommandMeta(
    string InputType,
    string Compiler,
    double? Confidence,
    string RawInput);
