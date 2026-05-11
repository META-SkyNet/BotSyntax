using BotSyntax.Core.Models;
using BotSyntax.Core.Parser;
using FluentAssertions;

namespace BotSyntax.Core.Tests.Parser;

public class CompactStringParserTests
{
    private readonly CompactStringParser _parser = new();

    [Fact]
    public void Parse_AuthenticatedUser_ExtractsAllFields()
    {
        var result = _parser.Parse("context('staff', 'emp001').order.status(order_id: 10234)");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be("staff");
        result.Value.UserId.Should().Be("emp001");
        result.Value.Domain.Should().Be("order");
        result.Value.Action.Should().Be("status");
        result.Value.Params.Should().ContainKey("order_id").WhoseValue.Should().Be(10234);
    }

    [Fact]
    public void Parse_GuestUser_HasNullUserId()
    {
        var result = _parser.Parse("context('customer', guest).order.status(order_id: 10234)");

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().BeNull();
        result.Value.Role.Should().Be("customer");
    }

    [Fact]
    public void Parse_MultipleParams_ExtractsAll()
    {
        var result = _parser.Parse("context('staff', 'emp001').order.list(status: 'PENDING', date: '01/05/2026~08/05/2026')");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Params["status"].Should().Be("PENDING");
        result.Value.Params["date"].Should().Be("01/05/2026~08/05/2026");
    }

    [Fact]
    public void Parse_NoParams_ReturnsEmptyParams()
    {
        var result = _parser.Parse("context('manager', 'mgr01').report.today()");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Params.Should().BeEmpty();
    }

    [Fact]
    public void Parse_StringValueWithSingleQuotes_StripsQuotes()
    {
        var result = _parser.Parse("context('staff', 'emp001').ship.assign(order_id: 10234, carrier: 'GHN')");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Params["carrier"].Should().Be("GHN");
        result.Value.Params["order_id"].Should().Be(10234);
    }

    [Fact]
    public void Parse_InvalidFormat_ReturnsError()
    {
        var result = _parser.Parse("this is not a compact string");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CompilerErrorCode.InvalidParam);
    }

    [Fact]
    public void Parse_DoubleQuotesInsteadOfSingle_AlsoWorks()
    {
        var result = _parser.Parse("context(\"staff\", \"emp001\").order.status(order_id: 10234)");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be("staff");
    }
}
