using BotSyntax.Core.Permission;
using FluentAssertions;

namespace BotSyntax.Core.Tests.Permission;

public class PermissionTableTests
{
    [Fact]
    public void IsAllowed_StaffOnAllowedAction_ReturnsTrue()
    {
        var table = new PermissionTableBuilder()
            .Allow("staff", "order", "status")
            .Build();

        table.IsAllowed("staff", "order", "status").Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_CustomerOnStaffOnlyAction_ReturnsFalse()
    {
        var table = new PermissionTableBuilder()
            .Allow("staff", "order", "cancel")
            .Build();

        table.IsAllowed("customer", "order", "cancel").Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_ManagerInheritsStaffPermissions()
    {
        var table = new PermissionTableBuilder()
            .Allow("staff", "order", "status")
            .Inherit("supervisor", "staff")
            .Inherit("manager", "supervisor")
            .Build();

        table.IsAllowed("manager", "order", "status").Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_UnknownRole_ReturnsFalse()
    {
        var table = new PermissionTableBuilder().Build();

        table.IsAllowed("hacker", "order", "cancel").Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_WildcardDomain_AllowsAnyDomain()
    {
        var table = new PermissionTableBuilder()
            .Allow("manager", "*", "*")
            .Build();

        table.IsAllowed("manager", "order", "cancel").Should().BeTrue();
        table.IsAllowed("manager", "report", "today").Should().BeTrue();
    }
}
