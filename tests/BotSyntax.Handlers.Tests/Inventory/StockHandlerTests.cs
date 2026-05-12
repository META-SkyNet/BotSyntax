using BotSyntax.Core.Models;
using BotSyntax.Handlers.Inventory;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Inventory;

public class StockHandlerTests
{
    private readonly StockHandler _handler = new();

    [Fact]
    public async Task Update_returns_Ok_with_new_quantity()
    {
        var cmd = TestRootCommand.Build("stock", "update",
            new() { ["sku"] = "AO-HOODIE-L-DEN", ["quantity"] = "+50" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("create")]
    [InlineData("add")]
    [InlineData("confirm")]
    [InlineData("list")]
    public async Task Import_subactions_return_Ok(string subaction)
    {
        var cmd = TestRootCommand.Build("stock", "import",
            new() { ["subaction"] = subaction, ["import_id"] = "NK001", ["sku"] = "AO-HOODIE-L-DEN", ["supplier"] = "Công ty ABC", ["date"] = "08/05/2026", ["quantity"] = "100" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Low_returns_Ok_with_low_stock_list()
    {
        var cmd = TestRootCommand.Build("stock", "low");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("stock", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
