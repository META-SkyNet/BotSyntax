namespace BotSyntax.Core.Models;

public enum ExecutionStatus { Ok, PermissionDenied, NotFound, Error }

public sealed class ExecutionResult
{
    public ExecutionStatus Status { get; }
    public object? Payload { get; }
    public string? Message { get; }

    private ExecutionResult(ExecutionStatus status, object? payload, string? message)
    { Status = status; Payload = payload; Message = message; }

    public static ExecutionResult Ok(object payload) =>
        new(ExecutionStatus.Ok, payload, null);
    public static ExecutionResult Denied(string message) =>
        new(ExecutionStatus.PermissionDenied, null, message);
    public static ExecutionResult NotFound(string message) =>
        new(ExecutionStatus.NotFound, null, message);
    public static ExecutionResult Error(string message) =>
        new(ExecutionStatus.Error, null, message);
}
