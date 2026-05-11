using BotSyntax.Core.Permission;

namespace BotSyntax.Core.Tests.Helpers;

public static class TestPermissionTable
{
    public static PermissionTable Build() => new PermissionTableBuilder()
        // customer
        .Allow("customer", "order",    "status")
        .Allow("customer", "order",    "cancel")
        .Allow("customer", "ship",     "status")
        .Allow("customer", "returns",  "create")
        .Allow("customer", "returns",  "status")
        .Allow("customer", "product",  "search")
        .Allow("customer", "product",  "info")
        .Allow("customer", "product",  "stock")
        .Allow("customer", "warranty", "status")
        .Allow("customer", "warranty", "check")
        .Allow("customer", "cart",     "*")
        // staff inherits customer + adds more
        .Allow("staff",    "order",    "list")
        .Allow("staff",    "order",    "confirm")
        .Allow("staff",    "order",    "note")
        .Allow("staff",    "ship",     "assign")
        .Allow("staff",    "ship",     "list")
        .Allow("staff",    "ship",     "address")
        .Allow("staff",    "customer", "*")
        .Allow("staff",    "returns",  "status")
        .Inherit("staff",       "customer")
        // warehouse
        .Allow("warehouse", "stock", "*")
        .Inherit("warehouse", "staff")
        // supervisor
        .Allow("supervisor", "order",    "cancel")
        .Allow("supervisor", "order",    "update")
        .Allow("supervisor", "ship",     "fail")
        .Allow("supervisor", "returns",  "approve")
        .Allow("supervisor", "returns",  "reject")
        .Allow("supervisor", "warranty", "create")
        .Allow("supervisor", "warranty", "update")
        .Allow("supervisor", "report",   "pending-orders")
        .Allow("supervisor", "report",   "today")
        .Inherit("supervisor", "warehouse")
        // accountant
        .Allow("accountant", "debt", "*")
        .Inherit("accountant", "supervisor")
        // manager gets everything
        .Allow("manager", "*", "*")
        .Build();
}
