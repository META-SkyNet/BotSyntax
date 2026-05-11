using BotSyntax.Core.ChatBox;
using BotSyntax.Core.Models;
using FluentAssertions;

namespace BotSyntax.Core.Tests.ChatBox;

public class SystemChatBoxTests
{
    private readonly SystemChatBox _box = new();

    [Fact]
    public void Parse_ValidCompactString_ReturnsOk()
    {
        var result = _box.Parse(
            "context('system', 'scheduler').report.daily()",
            "system", "scheduler");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be("system");
        result.Value.UserId.Should().Be("scheduler");
        result.Value.Domain.Should().Be("report");
        result.Value.Action.Should().Be("daily");
    }

    [Fact]
    public void Parse_RoleMismatch_ReturnsContextMismatchError()
    {
        var result = _box.Parse(
            "context('manager', 'emp001').report.daily()",
            "system", "scheduler");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CompilerErrorCode.ContextMismatch);
    }

    [Fact]
    public void Parse_UserIdMismatch_ReturnsContextMismatchError()
    {
        var result = _box.Parse(
            "context('system', 'webhook-ghn').ship.update(order_id: 10234, status: 'delivered')",
            "system", "scheduler");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CompilerErrorCode.ContextMismatch);
    }

    [Fact]
    public void Parse_MalformedCompactString_ReturnsParseError()
    {
        var result = _box.Parse("not a compact string", "system", "scheduler");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CompilerErrorCode.ParseError);
    }

    [Fact]
    public void Parse_WebhookCompactString_ParsesParams()
    {
        var result = _box.Parse(
            "context('system', 'webhook-ghn').ship.update(order_id: 10234, status: 'delivered')",
            "system", "webhook-ghn");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Domain.Should().Be("ship");
        result.Value.Action.Should().Be("update");
        result.Value.Params["order_id"].Should().Be(10234);
        result.Value.Params["status"].Should().Be("delivered");
    }
}
