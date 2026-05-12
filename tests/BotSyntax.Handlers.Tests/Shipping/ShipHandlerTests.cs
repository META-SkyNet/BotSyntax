using BotSyntax.Core.Models;
using BotSyntax.Handlers.Shipping;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Shipping;

public class ShipHandlerTests
{
    private readonly ShipHandler _handler = new();

    [Fact]
    public async Task Status_returns_Ok_with_tracking_payload()
    {
        var cmd = TestRootCommand.Build("ship", "status",
            new() { ["order_id"] = 10234 });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("fail")]
    [InlineData("address")]
    [InlineData("list")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("ship", action,
            new() { ["order_id"] = 10234, ["carrier"] = "GHN", ["reason"] = "khách không nhà" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("ship", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
