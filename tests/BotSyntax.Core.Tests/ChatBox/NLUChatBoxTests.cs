using BotSyntax.Core.ChatBox;
using BotSyntax.Core.Compiler;
using BotSyntax.Core.Models;
using FluentAssertions;

namespace BotSyntax.Core.Tests.ChatBox;

public class NLUChatBoxTests
{
    private readonly NLUChatBox _box = new(AlgorithmNLUCompiler.CreateDefault());

    [Theory]
    [InlineData("đơn 10234 đang ở đâu")]
    [InlineData("kiểm tra đơn 10234")]
    [InlineData("don 10234 dang o dau")]
    public void Parse_OrderStatusPatterns_MatchCorrectly(string input)
    {
        var result = _box.Parse(input, "customer", "cus5501");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Domain.Should().Be("order");
        result.Value.Action.Should().Be("status");
    }

    [Fact]
    public void Parse_OrderStatusWithId_ExtractsOrderId()
    {
        var result = _box.Parse("đơn 10234 đang ở đâu", "customer", "cus5501");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Params.Should().ContainKey("order_id").WhoseValue.Should().Be(10234);
    }

    [Theory]
    [InlineData("tôi muốn trả hàng")]
    [InlineData("hàng bị lỗi muốn trả")]
    [InlineData("hoàn tiền đơn 10234")]
    public void Parse_ReturnPatterns_MatchCorrectly(string input)
    {
        var result = _box.Parse(input, "customer", "cus5501");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Domain.Should().Be("returns");
    }

    [Fact]
    public void Parse_UnknownInput_ReturnsUnknownIntent()
    {
        var result = _box.Parse("xyzzy không có ý nghĩa gì", "customer", "cus5501");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CompilerErrorCode.UnknownIntent);
    }

    [Fact]
    public void Parse_RoleAlwaysFromSession_NotFromInput()
    {
        var result = _box.Parse("đơn 10234 đang ở đâu", "customer", "cus5501");

        result.Value!.Role.Should().Be("customer");
    }
}
