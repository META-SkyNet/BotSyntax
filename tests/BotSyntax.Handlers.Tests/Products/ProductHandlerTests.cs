using BotSyntax.Core.Models;
using BotSyntax.Handlers.Products;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Products;

public class ProductHandlerTests
{
    private readonly ProductHandler _handler = new();

    [Fact]
    public async Task Search_returns_Ok_with_results_list()
    {
        var cmd = TestRootCommand.Build("product", "search",
            new() { ["keyword"] = "hoodie" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("info")]
    [InlineData("update")]
    [InlineData("stock")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("product", action,
            new() { ["sku"] = "AO-HOODIE-L-DEN", ["price"] = 299000 });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("product", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
