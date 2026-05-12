using BotSyntax.Core.Models;
using BotSyntax.Handlers.Orders;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Orders;

public class OrderHandlerTests
{
    private readonly OrderHandler _handler = new();

    [Fact]
    public async Task Status_returns_Ok_with_order_payload()
    {
        var cmd = TestRootCommand.Build("order", "status",
            new() { ["order_id"] = 10234 });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("list")]
    [InlineData("confirm")]
    [InlineData("cancel")]
    [InlineData("update")]
    [InlineData("note")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("order", action,
            new() { ["order_id"] = 10234, ["status"] = "CONFIRMED" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("order", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
