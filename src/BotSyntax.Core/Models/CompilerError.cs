namespace BotSyntax.Core.Models;

public enum CompilerErrorCode
{
    UnknownIntent,
    MissingParam,
    InvalidParam,
    AmbiguousIntent,
    CompilerError,
    ParseError,
    ContextMismatch
}

public sealed record CompilerError(CompilerErrorCode Code, string Message);
