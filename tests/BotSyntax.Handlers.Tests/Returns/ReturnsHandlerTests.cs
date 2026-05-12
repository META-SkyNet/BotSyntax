using BotSyntax.Core.Models;
using BotSyntax.Handlers.Returns;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Returns;

public class ReturnsHandlerTests
{
    private readonly ReturnsHandler _handler = new();

    [Fact]
    public async Task Create_returns_Ok_with_return_id()
    {
        var cmd = TestRootCommand.Build("returns", "create",
            new() { ["order_id"] = 10234, ["type"] = "exchange" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("status")]
    [InlineData("approve")]
    [InlineData("reject")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("returns", action,
            new() { ["return_id"] = "TRA001", ["reason"] = "quá thời hạn" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("returns", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
