using BotSyntax.Core.Models;
using BotSyntax.Handlers.Warranty;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Warranty;

public class WarrantyHandlerTests
{
    private readonly WarrantyHandler _handler = new();

    [Fact]
    public async Task Status_returns_Ok_with_warranty_info()
    {
        var cmd = TestRootCommand.Build("warranty", "status",
            new() { ["warranty_code"] = "BH00456" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("check")]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("list")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("warranty", action,
            new() { ["warranty_code"] = "BH00456", ["order"] = "10234", ["order_id"] = 10234, ["issue"] = "màn hình vỡ", ["status"] = "REPAIRING" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("warranty", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
