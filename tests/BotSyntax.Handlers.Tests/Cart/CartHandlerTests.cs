using BotSyntax.Core.Models;
using BotSyntax.Handlers.Cart;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Cart;

public class CartHandlerTests
{
    private readonly CartHandler _handler = new();

    [Fact]
    public async Task View_returns_Ok_with_cart_contents()
    {
        var cmd = TestRootCommand.Build("cart", "view",
            role: "customer", userId: "cus5501");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("add")]
    [InlineData("voucher")]
    [InlineData("payment")]
    [InlineData("checkout")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("cart", action,
            new() { ["sku"] = "AO-HOODIE-L-DEN", ["voucher_code"] = "SALE50", ["method"] = "COD" },
            role: "customer", userId: "cus5501");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("cart", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
