using BotSyntax.Core.ChatBox;
using BotSyntax.Core.Models;
using FluentAssertions;

namespace BotSyntax.Core.Tests.ChatBox;

public class SlashChatBoxTests
{
    private readonly SlashChatBox _box = new();

    [Fact]
    public void Parse_SimpleStatusCommand_ReturnsCompactString()
    {
        var result = _box.Parse("/order status 10234", "staff", "emp001");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Domain.Should().Be("order");
        result.Value.Action.Should().Be("status");
        result.Value.Role.Should().Be("staff");
        result.Value.UserId.Should().Be("emp001");
        result.Value.Params.Should().ContainKey("order_id").WhoseValue.Should().Be(10234);
    }

    [Fact]
    public void Parse_CommandWithNamedParams_ExtractsParams()
    {
        var result = _box.Parse("/order list --status=PENDING --date=01/05/2026~08/05/2026", "staff", "emp001");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Action.Should().Be("list");
        result.Value.Params["status"].Should().Be("PENDING");
        result.Value.Params["date"].Should().Be("01/05/2026~08/05/2026");
    }

    [Fact]
    public void Parse_CommandWithFlag_SetsBooleanTrue()
    {
        var result = _box.Parse("/order list --help", "staff", "emp001");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Params["help"].Should().Be(true);
    }

    [Fact]
    public void Parse_NoSlashPrefix_ReturnsUnknownIntent()
    {
        var result = _box.Parse("order status 10234", "staff", "emp001");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CompilerErrorCode.UnknownIntent);
    }

    [Fact]
    public void Parse_MissingAction_ReturnsMissingParam()
    {
        var result = _box.Parse("/order", "staff", "emp001");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CompilerErrorCode.MissingParam);
    }

    [Fact]
    public void Parse_InjectsRoleFromSession_NotFromInput()
    {
        // Even if someone crafts input that looks like a context() call, it's treated as unknown
        var result = _box.Parse("context('manager', 'hack').report.today()", "staff", "emp001");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CompilerErrorCode.UnknownIntent);
    }
}
