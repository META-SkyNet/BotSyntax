namespace BotSyntax.Core.Models;

public sealed class CompilerResult
{
    public bool IsSuccess { get; }
    public CompactString? Value { get; }
    public CompilerError? Error { get; }

    private CompilerResult(CompactString value) { IsSuccess = true; Value = value; }
    private CompilerResult(CompilerError error) { IsSuccess = false; Error = error; }

    public static CompilerResult Ok(CompactString value) => new(value);
    public static CompilerResult Fail(CompilerErrorCode code, string message) =>
        new(new CompilerError(code, message));
}
