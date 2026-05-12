using BotSyntax.Core.Models;
using BotSyntax.Handlers.Customer;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Customer;

public class CustomerHandlerTests
{
    private readonly CustomerHandler _handler = new();

    [Fact]
    public async Task Info_returns_Ok_with_customer_profile()
    {
        var cmd = TestRootCommand.Build("customer", "info",
            new() { ["customer_id"] = "KH001" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("note")]
    [InlineData("orders")]
    [InlineData("search")]
    [InlineData("complain")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("customer", action,
            new() { ["customer_id"] = "KH001", ["keyword"] = "Nguyen" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("create")]
    [InlineData("status")]
    [InlineData("close")]
    public async Task Complaint_subactions_return_Ok(string subaction)
    {
        var cmd = TestRootCommand.Build("customer", "complaint",
            new() { ["subaction"] = subaction, ["customer_id"] = "KH001", ["complaint_id"] = "KN001", ["resolution"] = "Đã hoàn tiền" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("customer", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
