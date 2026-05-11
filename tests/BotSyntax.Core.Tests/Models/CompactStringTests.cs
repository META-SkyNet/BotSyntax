using BotSyntax.Core.Models;
using FluentAssertions;

namespace BotSyntax.Core.Tests.Models;

public class CompactStringTests
{
    [Fact]
    public void CompactString_WithAuthenticatedUser_HoldsAllFields()
    {
        var cs = new CompactString("staff", "emp001", "order", "status",
            new Dictionary<string, object> { ["order_id"] = 10234 });

        cs.Role.Should().Be("staff");
        cs.UserId.Should().Be("emp001");
        cs.Domain.Should().Be("order");
        cs.Action.Should().Be("status");
        cs.Params["order_id"].Should().Be(10234);
    }

    [Fact]
    public void CompactString_GuestUser_HasNullUserId()
    {
        var cs = new CompactString("customer", null, "order", "status",
            new Dictionary<string, object> { ["order_id"] = 10234 });

        cs.UserId.Should().BeNull();
    }

    [Fact]
    public void CompactString_ToText_ReturnsCanonicalForm()
    {
        var cs = new CompactString("staff", "emp001", "order", "status",
            new Dictionary<string, object> { ["order_id"] = 10234 });

        cs.ToText().Should().Be("context('staff', 'emp001').order.status(order_id: 10234)");
    }

    [Fact]
    public void CompactString_GuestToText_UsesGuestKeyword()
    {
        var cs = new CompactString("customer", null, "order", "status",
            new Dictionary<string, object>());

        cs.ToText().Should().Be("context('customer', guest).order.status()");
    }
}
