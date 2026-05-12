using BotSyntax.Core.Models;
using BotSyntax.Handlers.Reports;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Reports;

public class ReportHandlerTests
{
    private readonly ReportHandler _handler = new();

    [Fact]
    public async Task Today_returns_Ok_with_dashboard_snapshot()
    {
        var cmd = TestRootCommand.Build("report", "today", role: "supervisor", userId: "sup01");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("revenue")]
    [InlineData("top-products")]
    [InlineData("pending-orders")]
    [InlineData("returns")]
    [InlineData("daily")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("report", action,
            new() { ["date"] = "01/05/2026~11/05/2026", ["limit"] = 10 },
            role: "manager", userId: "mgr01");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("report", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
