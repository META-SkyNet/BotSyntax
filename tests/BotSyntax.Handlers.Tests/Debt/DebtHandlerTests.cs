using BotSyntax.Core.Models;
using BotSyntax.Handlers.Debt;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Debt;

public class DebtHandlerTests
{
    private readonly DebtHandler _handler = new();

    [Fact]
    public async Task List_returns_Ok_with_debt_records()
    {
        var cmd = TestRootCommand.Build("debt", "list",
            new() { ["partner"] = "GHN", ["status"] = "unpaid" },
            role: "accountant", userId: "acc01");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("confirm")]
    [InlineData("reconcile")]
    [InlineData("create")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("debt", action,
            new() { ["debt_id"] = "CN001", ["amount"] = 5000000, ["carrier"] = "GHN", ["date"] = "01/05/2026~07/05/2026", ["partner"] = "GHN", ["due"] = "15/05/2026" },
            role: "accountant", userId: "acc01");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("debt", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
