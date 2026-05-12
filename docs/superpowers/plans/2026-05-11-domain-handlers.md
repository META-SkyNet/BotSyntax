# BotSyntax Domain Handlers — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement 10 domain handler stubs — each returns realistic mock `ExecutionResult.Ok(payload)` data, no real database required.

**Architecture:** Each handler is a sealed class implementing `IDomainHandler` from `BotSyntax.Core`. Handlers live in a new `BotSyntax.Handlers` project. Each handler switches on `RootCommand.Action` and returns a mock payload; unknown actions return `ExecutionResult.Error`. A `BotSyntax.Handlers.Tests` project verifies every handler with TDD. Task 1 also fixes two known issues in Plan 1 code: (1) `CompactStringParser` regex does not allow hyphens in action names (breaks `top-products`, `pending-orders`), and (2) `SlashCommandCompiler._positionalParams` is missing `stock.import` and `customer.complaint` entries.

**Tech Stack:** .NET 8, C# 12, xUnit 2.5.3, FluentAssertions 6.x, NSubstitute 5.3.0

---

## Scope

**Plan 1 (done):** Core engine — interfaces, models, parser, compiler, permission system, BotExecutor.  
**This plan (Plan 2):** 10 domain handler stubs + test project + two targeted fixes to Plan 1 code.  
**Plan 3:** AI Agent NLU integration for NLUChatBox.  
**Plan 4:** Real repository layer replacing mock payloads.

---

## File Structure

```
src/
  BotSyntax.Handlers/
    BotSyntax.Handlers.csproj
    Orders/       OrderHandler.cs
    Shipping/     ShipHandler.cs
    Returns/      ReturnsHandler.cs
    Products/     ProductHandler.cs
    Inventory/    StockHandler.cs
    Warranty/     WarrantyHandler.cs
    Debt/         DebtHandler.cs
    Customer/     CustomerHandler.cs
    Reports/      ReportHandler.cs
    Cart/         CartHandler.cs

tests/
  BotSyntax.Handlers.Tests/
    BotSyntax.Handlers.Tests.csproj
    Helpers/      TestRootCommand.cs
    Orders/       OrderHandlerTests.cs
    Shipping/     ShipHandlerTests.cs
    Returns/      ReturnsHandlerTests.cs
    Products/     ProductHandlerTests.cs
    Inventory/    StockHandlerTests.cs
    Warranty/     WarrantyHandlerTests.cs
    Debt/         DebtHandlerTests.cs
    Customer/     CustomerHandlerTests.cs
    Reports/      ReportHandlerTests.cs
    Cart/         CartHandlerTests.cs
```

---

## Task 1: Project Setup + Parser Fix + _positionalParams Update

**Files:**
- Create: `src/BotSyntax.Handlers/BotSyntax.Handlers.csproj`
- Create: `tests/BotSyntax.Handlers.Tests/BotSyntax.Handlers.Tests.csproj`
- Create: `tests/BotSyntax.Handlers.Tests/Helpers/TestRootCommand.cs`
- Modify: `src/BotSyntax.Core/Parser/CompactStringParser.cs` — allow hyphens in action regex
- Modify: `src/BotSyntax.Core/Compiler/SlashCommandCompiler.cs` — add `stock.import` and `customer.complaint` positional params

- [ ] **Step 1: Create handler project file**

Create `src/BotSyntax.Handlers/BotSyntax.Handlers.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\BotSyntax.Core\BotSyntax.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create test project file**

Create `tests/BotSyntax.Handlers.Tests/BotSyntax.Handlers.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\BotSyntax.Handlers\BotSyntax.Handlers.csproj" />
    <ProjectReference Include="..\..\src\BotSyntax.Core\BotSyntax.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Add projects to solution**

Run:
```bash
dotnet sln add src/BotSyntax.Handlers/BotSyntax.Handlers.csproj
dotnet sln add tests/BotSyntax.Handlers.Tests/BotSyntax.Handlers.Tests.csproj
```

Expected: `Project ... added to the solution.` (×2)

- [ ] **Step 4: Create TestRootCommand helper**

Create `tests/BotSyntax.Handlers.Tests/Helpers/TestRootCommand.cs`:

```csharp
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Tests.Helpers;

internal static class TestRootCommand
{
    public static RootCommand Build(
        string domain,
        string action,
        Dictionary<string, object>? @params = null,
        string role = "staff",
        string userId = "emp001") =>
        new(
            Root: $"context('{role}', '{userId}').{domain}.{action}()",
            Domain: domain,
            Action: action,
            Params: @params ?? new Dictionary<string, object>(),
            Context: new RootCommandContext(role, userId, "web", null, DateTimeOffset.UtcNow),
            Meta: new RootCommandMeta("slash", "algorithm", null, $"/{domain} {action}")
        );
}
```

- [ ] **Step 5: Fix CompactStringParser — allow hyphens in action name**

In `src/BotSyntax.Core/Parser/CompactStringParser.cs`, line 12–13, change the regex so `(?<action>[a-z]+)` becomes `(?<action>[a-z][a-z-]*)`:

```csharp
    private static readonly Regex _pattern = new(
        @"^context\(['""](?<role>[^'""]+)['""]\s*,\s*(?:guest|['""](?<userId>[^'""]*)['""])\)\." +
        @"(?<domain>[a-z]+)\.(?<action>[a-z][a-z-]*)\((?<params>[^)]*)\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
```

- [ ] **Step 6: Fix SlashCommandCompiler — add stock.import and customer.complaint positional params**

In `src/BotSyntax.Core/Compiler/SlashCommandCompiler.cs`, append two entries to `_positionalParams` (after line 29):

```csharp
        ["stock.import"]       = "subaction",
        ["customer.complaint"] = "subaction",
```

Full updated dictionary:

```csharp
    private static readonly Dictionary<string, string> _positionalParams = new()
    {
        ["order.status"]       = "order_id",
        ["order.confirm"]      = "order_id",
        ["order.cancel"]       = "order_id",
        ["order.note"]         = "order_id",
        ["order.update"]       = "order_id",
        ["ship.status"]        = "order_id",
        ["ship.fail"]          = "order_id",
        ["ship.address"]       = "order_id",
        ["returns.status"]     = "return_id",
        ["returns.approve"]    = "return_id",
        ["returns.reject"]     = "return_id",
        ["product.info"]       = "sku",
        ["product.stock"]      = "sku",
        ["warranty.status"]    = "warranty_code",
        ["debt.confirm"]       = "debt_id",
        ["customer.info"]      = "customer_id",
        ["customer.orders"]    = "customer_id",
        ["customer.note"]      = "customer_id",
        ["customer.search"]    = "keyword",
        ["stock.import"]       = "subaction",
        ["customer.complaint"] = "subaction",
    };
```

- [ ] **Step 7: Verify existing tests still pass**

Run:
```bash
dotnet test tests/BotSyntax.Core.Tests -v minimal
```

Expected: all tests pass (no regressions from parser and compiler changes).

- [ ] **Step 8: Commit**

```bash
git add src/BotSyntax.Handlers/BotSyntax.Handlers.csproj
git add tests/BotSyntax.Handlers.Tests/BotSyntax.Handlers.Tests.csproj
git add tests/BotSyntax.Handlers.Tests/Helpers/TestRootCommand.cs
git add src/BotSyntax.Core/Parser/CompactStringParser.cs
git add src/BotSyntax.Core/Compiler/SlashCommandCompiler.cs
git commit -m "feat: add BotSyntax.Handlers project + fix parser hyphen support + add stock/customer positional params"
```

---

## Task 2: OrderHandler

**Files:**
- Create: `src/BotSyntax.Handlers/Orders/OrderHandler.cs`
- Create: `tests/BotSyntax.Handlers.Tests/Orders/OrderHandlerTests.cs`

Actions: `status`, `list`, `confirm`, `cancel`, `update`, `note`

- [ ] **Step 1: Write failing test**

Create `tests/BotSyntax.Handlers.Tests/Orders/OrderHandlerTests.cs`:

```csharp
using BotSyntax.Core.Models;
using BotSyntax.Handlers.Orders;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Orders;

public class OrderHandlerTests
{
    private readonly OrderHandler _handler = new();

    [Fact]
    public async Task Status_returns_Ok_with_order_payload()
    {
        var cmd = TestRootCommand.Build("order", "status",
            new() { ["order_id"] = 10234 });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("list")]
    [InlineData("confirm")]
    [InlineData("cancel")]
    [InlineData("update")]
    [InlineData("note")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("order", action,
            new() { ["order_id"] = 10234, ["status"] = "CONFIRMED" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("order", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: FAIL — `BotSyntax.Handlers.Orders.OrderHandler` not found.

- [ ] **Step 3: Implement OrderHandler**

Create `src/BotSyntax.Handlers/Orders/OrderHandler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Orders;

public sealed class OrderHandler : IDomainHandler
{
    public string Domain => "order";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "status"  => ExecutionResult.Ok(MockOrder(command.Params)),
            "list"    => ExecutionResult.Ok(new { orders = new[] { MockOrder(new()) }, total = 1 }),
            "confirm" => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), status = "CONFIRMED",  message = "Đơn hàng đã được xác nhận." }),
            "cancel"  => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), status = "CANCELLED",  message = "Đơn hàng đã bị hủy." }),
            "update"  => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), status = Str(command.Params, "status"), message = "Trạng thái đã được cập nhật." }),
            "note"    => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), note_added = true, message = "Ghi chú đã được thêm." }),
            _         => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'order'")
        });

    private static object MockOrder(Dictionary<string, object> p) => new
    {
        order_id   = Str(p, "order_id", "10234"),
        status     = "SHIPPING",
        created_at = "2026-05-01",
        items      = new[] { new { sku = "AO-HOODIE-L-DEN", name = "Áo Hoodie Đen L", qty = 1, price = 299000 } },
        total      = 299000,
        address    = "123 Nguyễn Huệ, Q1, HCM",
        carrier    = "GHN",
        tracking   = "GHN123456789"
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Handlers/Orders/OrderHandler.cs
git add tests/BotSyntax.Handlers.Tests/Orders/OrderHandlerTests.cs
git commit -m "feat: add OrderHandler stub with mock payloads"
```

---

## Task 3: ShipHandler

**Files:**
- Create: `src/BotSyntax.Handlers/Shipping/ShipHandler.cs`
- Create: `tests/BotSyntax.Handlers.Tests/Shipping/ShipHandlerTests.cs`

Actions: `status`, `assign`, `fail`, `address`, `list`

- [ ] **Step 1: Write failing test**

Create `tests/BotSyntax.Handlers.Tests/Shipping/ShipHandlerTests.cs`:

```csharp
using BotSyntax.Core.Models;
using BotSyntax.Handlers.Shipping;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Shipping;

public class ShipHandlerTests
{
    private readonly ShipHandler _handler = new();

    [Fact]
    public async Task Status_returns_Ok_with_tracking_payload()
    {
        var cmd = TestRootCommand.Build("ship", "status",
            new() { ["order_id"] = 10234 });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("fail")]
    [InlineData("address")]
    [InlineData("list")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("ship", action,
            new() { ["order_id"] = 10234, ["carrier"] = "GHN", ["reason"] = "khách không nhà" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("ship", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: FAIL — `BotSyntax.Handlers.Shipping.ShipHandler` not found.

- [ ] **Step 3: Implement ShipHandler**

Create `src/BotSyntax.Handlers/Shipping/ShipHandler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Shipping;

public sealed class ShipHandler : IDomainHandler
{
    public string Domain => "ship";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "status"  => ExecutionResult.Ok(MockTracking(command.Params)),
            "assign"  => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), carrier = Str(command.Params, "carrier"), message = "Đã gán đơn vị vận chuyển." }),
            "fail"    => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), reason = Str(command.Params, "reason"), action = Str(command.Params, "action", "retry"), message = "Đã ghi nhận giao thất bại." }),
            "address" => ExecutionResult.Ok(new { order_id = Str(command.Params, "order_id"), new_address = Str(command.Params, "address"), updated = true }),
            "list"    => ExecutionResult.Ok(new { shipments = new[] { MockTracking(new()) }, total = 1 }),
            _         => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'ship'")
        });

    private static object MockTracking(Dictionary<string, object> p) => new
    {
        order_id        = Str(p, "order_id", "10234"),
        carrier         = "GHN",
        tracking_number = "GHN123456789",
        status          = "SHIPPING",
        estimated_at    = "2026-05-12",
        last_event      = new { time = "2026-05-11T08:00:00", location = "Bưu cục Q1, HCM", note = "Đã lấy hàng" }
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Handlers/Shipping/ShipHandler.cs
git add tests/BotSyntax.Handlers.Tests/Shipping/ShipHandlerTests.cs
git commit -m "feat: add ShipHandler stub with mock payloads"
```

---

## Task 4: ReturnsHandler

**Files:**
- Create: `src/BotSyntax.Handlers/Returns/ReturnsHandler.cs`
- Create: `tests/BotSyntax.Handlers.Tests/Returns/ReturnsHandlerTests.cs`

Actions: `create`, `status`, `approve`, `reject`

- [ ] **Step 1: Write failing test**

Create `tests/BotSyntax.Handlers.Tests/Returns/ReturnsHandlerTests.cs`:

```csharp
using BotSyntax.Core.Models;
using BotSyntax.Handlers.Returns;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Returns;

public class ReturnsHandlerTests
{
    private readonly ReturnsHandler _handler = new();

    [Fact]
    public async Task Create_returns_Ok_with_return_id()
    {
        var cmd = TestRootCommand.Build("returns", "create",
            new() { ["order_id"] = 10234, ["type"] = "exchange" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("status")]
    [InlineData("approve")]
    [InlineData("reject")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("returns", action,
            new() { ["return_id"] = "TRA001", ["reason"] = "quá thời hạn" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("returns", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: FAIL — `BotSyntax.Handlers.Returns.ReturnsHandler` not found.

- [ ] **Step 3: Implement ReturnsHandler**

Create `src/BotSyntax.Handlers/Returns/ReturnsHandler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Returns;

public sealed class ReturnsHandler : IDomainHandler
{
    public string Domain => "returns";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "create"  => ExecutionResult.Ok(new { return_id = "TRA001", order_id = Str(command.Params, "order_id"), type = Str(command.Params, "type", "exchange"), status = "REQUESTED", message = "Yêu cầu đổi trả đã được tạo." }),
            "status"  => ExecutionResult.Ok(MockReturn(command.Params)),
            "approve" => ExecutionResult.Ok(new { return_id = Str(command.Params, "return_id"), status = "APPROVED", message = "Yêu cầu đổi trả đã được duyệt." }),
            "reject"  => ExecutionResult.Ok(new { return_id = Str(command.Params, "return_id"), status = "REJECTED", reason = Str(command.Params, "reason"), message = "Yêu cầu đã bị từ chối." }),
            _         => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'returns'")
        });

    private static object MockReturn(Dictionary<string, object> p) => new
    {
        return_id  = Str(p, "return_id", "TRA001"),
        order_id   = "10234",
        type       = "exchange",
        status     = "PROCESSING",
        created_at = "2026-05-05",
        sku        = "AO-HOODIE-L-DEN",
        reason     = "Sai size"
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Handlers/Returns/ReturnsHandler.cs
git add tests/BotSyntax.Handlers.Tests/Returns/ReturnsHandlerTests.cs
git commit -m "feat: add ReturnsHandler stub with mock payloads"
```

---

## Task 5: ProductHandler

**Files:**
- Create: `src/BotSyntax.Handlers/Products/ProductHandler.cs`
- Create: `tests/BotSyntax.Handlers.Tests/Products/ProductHandlerTests.cs`

Actions: `search`, `info`, `update`, `stock`

- [ ] **Step 1: Write failing test**

Create `tests/BotSyntax.Handlers.Tests/Products/ProductHandlerTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: FAIL — `BotSyntax.Handlers.Products.ProductHandler` not found.

- [ ] **Step 3: Implement ProductHandler**

Create `src/BotSyntax.Handlers/Products/ProductHandler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Products;

public sealed class ProductHandler : IDomainHandler
{
    public string Domain => "product";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "search" => ExecutionResult.Ok(new { keyword = Str(command.Params, "keyword"), results = new[] { MockProduct() }, total = 1 }),
            "info"   => ExecutionResult.Ok(MockProduct()),
            "update" => ExecutionResult.Ok(new { sku = Str(command.Params, "sku"), updated = true, message = "Sản phẩm đã được cập nhật." }),
            "stock"  => ExecutionResult.Ok(new { sku = Str(command.Params, "sku"), quantity = 47, reserved = 3, available = 44, status = "IN_STOCK" }),
            _        => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'product'")
        });

    private static object MockProduct() => new
    {
        sku      = "AO-HOODIE-L-DEN",
        name     = "Áo Hoodie Đen Size L",
        price    = 299000,
        status   = "active",
        category = "Áo",
        variants = new[] { new { size = "S", qty = 10 }, new { size = "M", qty = 15 }, new { size = "L", qty = 22 } }
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Handlers/Products/ProductHandler.cs
git add tests/BotSyntax.Handlers.Tests/Products/ProductHandlerTests.cs
git commit -m "feat: add ProductHandler stub with mock payloads"
```

---

## Task 6: StockHandler

**Files:**
- Create: `src/BotSyntax.Handlers/Inventory/StockHandler.cs`
- Create: `tests/BotSyntax.Handlers.Tests/Inventory/StockHandlerTests.cs`

Actions: `update`, `import` (routes internally via `subaction` param: create/add/confirm/list), `low`

> **Note:** `/stock import create` from the slash command gives `action="import"` and `params["subaction"]="create"` (via the `_positionalParams` fix in Task 1). The handler switches on `subaction` internally.

- [ ] **Step 1: Write failing test**

Create `tests/BotSyntax.Handlers.Tests/Inventory/StockHandlerTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: FAIL — `BotSyntax.Handlers.Inventory.StockHandler` not found.

- [ ] **Step 3: Implement StockHandler**

Create `src/BotSyntax.Handlers/Inventory/StockHandler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Inventory;

public sealed class StockHandler : IDomainHandler
{
    public string Domain => "stock";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "update" => ExecutionResult.Ok(new { sku = Str(command.Params, "sku"), quantity_change = Str(command.Params, "quantity"), new_quantity = 97, message = "Tồn kho đã được cập nhật." }),
            "import" => HandleImport(command.Params),
            "low"    => ExecutionResult.Ok(new { items = new[] { new { sku = "DT-MODEL-X", name = "Điện thoại Model X", quantity = 3, threshold = 10 } }, total = 1 }),
            _        => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'stock'")
        });

    private static ExecutionResult HandleImport(Dictionary<string, object> p) =>
        Str(p, "subaction", "") switch
        {
            "create"  => ExecutionResult.Ok(new { import_id = "NK001", supplier = Str(p, "supplier"), date = Str(p, "date"), status = "DRAFT", message = "Phiếu nhập đã được tạo." }),
            "add"     => ExecutionResult.Ok(new { import_id = Str(p, "import_id"), sku = Str(p, "sku"), quantity = Str(p, "quantity"), message = "Sản phẩm đã được thêm vào phiếu nhập." }),
            "confirm" => ExecutionResult.Ok(new { import_id = Str(p, "import_id"), status = "CONFIRMED", items_imported = 2, message = "Phiếu nhập đã được xác nhận." }),
            "list"    => ExecutionResult.Ok(new { imports = new[] { new { import_id = "NK001", supplier = "Công ty ABC", date = "2026-05-08", status = "CONFIRMED", items = 5 } }, total = 1 }),
            _         => ExecutionResult.Ok(new { import_id = Str(p, "import_id", "NK001"), status = "DRAFT", items = Array.Empty<object>() })
        };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Handlers/Inventory/StockHandler.cs
git add tests/BotSyntax.Handlers.Tests/Inventory/StockHandlerTests.cs
git commit -m "feat: add StockHandler stub with import subaction routing"
```

---

## Task 7: WarrantyHandler

**Files:**
- Create: `src/BotSyntax.Handlers/Warranty/WarrantyHandler.cs`
- Create: `tests/BotSyntax.Handlers.Tests/Warranty/WarrantyHandlerTests.cs`

Actions: `status`, `check`, `create`, `update`, `list`

- [ ] **Step 1: Write failing test**

Create `tests/BotSyntax.Handlers.Tests/Warranty/WarrantyHandlerTests.cs`:

```csharp
using BotSyntax.Core.Models;
using BotSyntax.Handlers.Warranty;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Warranty;

public class WarrantyHandlerTests
{
    private readonly WarrantyHandler _handler = new();

    [Fact]
    public async Task Status_returns_Ok_with_warranty_info()
    {
        var cmd = TestRootCommand.Build("warranty", "status",
            new() { ["warranty_code"] = "BH00456" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("check")]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("list")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("warranty", action,
            new() { ["warranty_code"] = "BH00456", ["order"] = "10234", ["order_id"] = 10234, ["issue"] = "màn hình vỡ", ["status"] = "REPAIRING" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("warranty", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: FAIL — `BotSyntax.Handlers.Warranty.WarrantyHandler` not found.

- [ ] **Step 3: Implement WarrantyHandler**

Create `src/BotSyntax.Handlers/Warranty/WarrantyHandler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Warranty;

public sealed class WarrantyHandler : IDomainHandler
{
    public string Domain => "warranty";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "status" => ExecutionResult.Ok(MockWarranty(command.Params)),
            "check"  => ExecutionResult.Ok(new { order_id = Str(command.Params, "order"), warranty_code = "BH00456", valid = true, expires_at = "2027-05-01", product = "Điện thoại Model X" }),
            "create" => ExecutionResult.Ok(new { warranty_code = "BH00457", order_id = Str(command.Params, "order_id"), issue = Str(command.Params, "issue"), status = "RECEIVED", message = "Phiếu bảo hành đã được tạo." }),
            "update" => ExecutionResult.Ok(new { warranty_code = Str(command.Params, "warranty_code"), status = Str(command.Params, "status"), message = "Trạng thái bảo hành đã được cập nhật." }),
            "list"   => ExecutionResult.Ok(new { warranties = new[] { MockWarranty(new()) }, total = 1 }),
            _        => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'warranty'")
        });

    private static object MockWarranty(Dictionary<string, object> p) => new
    {
        warranty_code = Str(p, "warranty_code", "BH00456"),
        product       = "Điện thoại Model X",
        sku           = "DT-MODEL-X",
        status        = "REPAIRING",
        received_at   = "2026-05-03",
        issue         = "Màn hình bị vỡ",
        note          = "Đang chờ linh kiện"
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Handlers/Warranty/WarrantyHandler.cs
git add tests/BotSyntax.Handlers.Tests/Warranty/WarrantyHandlerTests.cs
git commit -m "feat: add WarrantyHandler stub with mock payloads"
```

---

## Task 8: DebtHandler

**Files:**
- Create: `src/BotSyntax.Handlers/Debt/DebtHandler.cs`
- Create: `tests/BotSyntax.Handlers.Tests/Debt/DebtHandlerTests.cs`

Actions: `list`, `confirm`, `reconcile`, `create`

- [ ] **Step 1: Write failing test**

Create `tests/BotSyntax.Handlers.Tests/Debt/DebtHandlerTests.cs`:

```csharp
using BotSyntax.Core.Models;
using BotSyntax.Handlers.Debt;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Debt;

public class DebtHandlerTests
{
    private readonly DebtHandler _handler = new();

    [Fact]
    public async Task List_returns_Ok_with_debt_records()
    {
        var cmd = TestRootCommand.Build("debt", "list",
            new() { ["partner"] = "GHN", ["status"] = "unpaid" },
            role: "accountant", userId: "acc01");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("confirm")]
    [InlineData("reconcile")]
    [InlineData("create")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("debt", action,
            new() { ["debt_id"] = "CN001", ["amount"] = 5000000, ["carrier"] = "GHN", ["date"] = "01/05/2026~07/05/2026", ["partner"] = "GHN", ["due"] = "15/05/2026" },
            role: "accountant", userId: "acc01");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("debt", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: FAIL — `BotSyntax.Handlers.Debt.DebtHandler` not found.

- [ ] **Step 3: Implement DebtHandler**

Create `src/BotSyntax.Handlers/Debt/DebtHandler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Debt;

public sealed class DebtHandler : IDomainHandler
{
    public string Domain => "debt";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "list"      => ExecutionResult.Ok(new { debts = new[] { MockDebt() }, total = 1, total_unpaid = 5000000 }),
            "confirm"   => ExecutionResult.Ok(new { debt_id = Str(command.Params, "debt_id"), amount_confirmed = Str(command.Params, "amount"), status = "paid", confirmed_at = "2026-05-11", message = "Đã ghi nhận thanh toán." }),
            "reconcile" => ExecutionResult.Ok(new { carrier = Str(command.Params, "carrier"), date = Str(command.Params, "date"), total_cod = 8500000, collected = 8200000, difference = 300000, orders = 12 }),
            "create"    => ExecutionResult.Ok(new { debt_id = "CN002", partner = Str(command.Params, "partner"), amount = Str(command.Params, "amount"), due = Str(command.Params, "due"), status = "unpaid", message = "Phiếu công nợ đã được tạo." }),
            _           => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'debt'")
        });

    private static object MockDebt() => new
    {
        debt_id    = "CN001",
        partner    = "GHN",
        amount     = 5000000,
        due        = "2026-05-15",
        status     = "unpaid",
        created_at = "2026-05-01"
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Handlers/Debt/DebtHandler.cs
git add tests/BotSyntax.Handlers.Tests/Debt/DebtHandlerTests.cs
git commit -m "feat: add DebtHandler stub with mock payloads"
```

---

## Task 9: CustomerHandler

**Files:**
- Create: `src/BotSyntax.Handlers/Customer/CustomerHandler.cs`
- Create: `tests/BotSyntax.Handlers.Tests/Customer/CustomerHandlerTests.cs`

Actions: `info`, `note`, `orders`, `search`, `complain` (NLU), `complaint` (routes by `subaction`: create/status/close)

> **Note:** `complain` comes from NLU (`DefaultPatterns`). `complaint` comes from slash commands (`/customer complaint create|status|close`) — the `subaction` param distinguishes the sub-operation.

- [ ] **Step 1: Write failing test**

Create `tests/BotSyntax.Handlers.Tests/Customer/CustomerHandlerTests.cs`:

```csharp
using BotSyntax.Core.Models;
using BotSyntax.Handlers.Customer;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Customer;

public class CustomerHandlerTests
{
    private readonly CustomerHandler _handler = new();

    [Fact]
    public async Task Info_returns_Ok_with_customer_profile()
    {
        var cmd = TestRootCommand.Build("customer", "info",
            new() { ["customer_id"] = "KH001" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("note")]
    [InlineData("orders")]
    [InlineData("search")]
    [InlineData("complain")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("customer", action,
            new() { ["customer_id"] = "KH001", ["keyword"] = "Nguyen" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("create")]
    [InlineData("status")]
    [InlineData("close")]
    public async Task Complaint_subactions_return_Ok(string subaction)
    {
        var cmd = TestRootCommand.Build("customer", "complaint",
            new() { ["subaction"] = subaction, ["customer_id"] = "KH001", ["complaint_id"] = "KN001", ["resolution"] = "Đã hoàn tiền" });
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("customer", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: FAIL — `BotSyntax.Handlers.Customer.CustomerHandler` not found.

- [ ] **Step 3: Implement CustomerHandler**

Create `src/BotSyntax.Handlers/Customer/CustomerHandler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Customer;

public sealed class CustomerHandler : IDomainHandler
{
    public string Domain => "customer";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "info"      => ExecutionResult.Ok(MockCustomer(command.Params)),
            "note"      => ExecutionResult.Ok(new { customer_id = Str(command.Params, "customer_id"), note_added = true, message = "Ghi chú đã được thêm." }),
            "orders"    => ExecutionResult.Ok(new { customer_id = Str(command.Params, "customer_id"), orders = new[] { new { order_id = "10234", status = "DELIVERED", total = 299000, date = "2026-05-01" } }, total = 1 }),
            "search"    => ExecutionResult.Ok(new { keyword = Str(command.Params, "keyword"), results = new[] { MockCustomer(new()) }, total = 1 }),
            "complain"  => ExecutionResult.Ok(new { complaint_id = "KN001", customer_id = command.Context.Identity ?? "guest", status = "OPEN", message = "Khiếu nại đã được ghi nhận, nhân viên sẽ liên hệ sớm nhất." }),
            "complaint" => HandleComplaint(command.Params),
            _           => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'customer'")
        });

    private static ExecutionResult HandleComplaint(Dictionary<string, object> p) =>
        Str(p, "subaction", "") switch
        {
            "create" => ExecutionResult.Ok(new { complaint_id = "KN002", customer_id = Str(p, "customer_id"), status = "OPEN", message = "Khiếu nại đã được tạo." }),
            "status" => ExecutionResult.Ok(new { complaint_id = Str(p, "complaint_id", "KN001"), status = "PROCESSING", created_at = "2026-05-09", assigned_to = "emp002" }),
            "close"  => ExecutionResult.Ok(new { complaint_id = Str(p, "complaint_id", "KN001"), status = "CLOSED", resolution = Str(p, "resolution"), closed_at = "2026-05-11" }),
            _        => ExecutionResult.Ok(new { complaint_id = Str(p, "complaint_id", "KN001"), status = "OPEN" })
        };

    private static object MockCustomer(Dictionary<string, object> p) => new
    {
        customer_id  = Str(p, "customer_id", "KH001"),
        name         = "Nguyễn Văn A",
        phone        = "0912345678",
        email        = "nguyenvana@example.com",
        total_orders = 5,
        total_spent  = 1495000,
        tier         = "silver",
        note         = "Khách VIP, ưu tiên xử lý"
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Handlers/Customer/CustomerHandler.cs
git add tests/BotSyntax.Handlers.Tests/Customer/CustomerHandlerTests.cs
git commit -m "feat: add CustomerHandler stub with complaint subaction routing"
```

---

## Task 10: ReportHandler

**Files:**
- Create: `src/BotSyntax.Handlers/Reports/ReportHandler.cs`
- Create: `tests/BotSyntax.Handlers.Tests/Reports/ReportHandlerTests.cs`

Actions: `today`, `revenue`, `top-products`, `pending-orders`, `returns`, `daily`

> **Note:** `top-products` and `pending-orders` use hyphens in the action name — this is why the `CompactStringParser` regex was fixed in Task 1. Tests construct `RootCommand` directly with these hyphenated action strings.

- [ ] **Step 1: Write failing test**

Create `tests/BotSyntax.Handlers.Tests/Reports/ReportHandlerTests.cs`:

```csharp
using BotSyntax.Core.Models;
using BotSyntax.Handlers.Reports;
using BotSyntax.Handlers.Tests.Helpers;
using FluentAssertions;

namespace BotSyntax.Handlers.Tests.Reports;

public class ReportHandlerTests
{
    private readonly ReportHandler _handler = new();

    [Fact]
    public async Task Today_returns_Ok_with_dashboard_snapshot()
    {
        var cmd = TestRootCommand.Build("report", "today", role: "supervisor", userId: "sup01");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Theory]
    [InlineData("revenue")]
    [InlineData("top-products")]
    [InlineData("pending-orders")]
    [InlineData("returns")]
    [InlineData("daily")]
    public async Task All_known_actions_return_Ok(string action)
    {
        var cmd = TestRootCommand.Build("report", action,
            new() { ["date"] = "01/05/2026~11/05/2026", ["limit"] = 10 },
            role: "manager", userId: "mgr01");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Ok);
        result.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Unknown_action_returns_Error()
    {
        var cmd = TestRootCommand.Build("report", "fly");
        var result = await _handler.HandleAsync(cmd);
        result.Status.Should().Be(ExecutionStatus.Error);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: FAIL — `BotSyntax.Handlers.Reports.ReportHandler` not found.

- [ ] **Step 3: Implement ReportHandler**

Create `src/BotSyntax.Handlers/Reports/ReportHandler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Reports;

public sealed class ReportHandler : IDomainHandler
{
    public string Domain => "report";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "today"          => ExecutionResult.Ok(new { date = "2026-05-11", new_orders = 24, revenue = 7200000, pending = 3, low_stock = 2, returns_pending = 1 }),
            "revenue"        => ExecutionResult.Ok(new { date = Str(command.Params, "date", "11/05/2026~11/05/2026"), total = 7200000, orders = 24, avg_order_value = 300000 }),
            "top-products"   => ExecutionResult.Ok(new { date = Str(command.Params, "date"), products = new[] { new { sku = "AO-HOODIE-L-DEN", name = "Áo Hoodie Đen L", sold = 12, revenue = 3588000 } }, total = 1 }),
            "pending-orders" => ExecutionResult.Ok(new { orders = new[] { new { order_id = "10231", status = "PENDING", days_waiting = 4 } }, total = 1 }),
            "returns"        => ExecutionResult.Ok(new { date = Str(command.Params, "date"), total_returns = 3, exchange = 2, refund = 1, total_amount = 598000 }),
            "daily"          => ExecutionResult.Ok(new { date = "2026-05-11", generated_at = DateTimeOffset.UtcNow.ToString("o"), summary = "Báo cáo ngày 11/05/2026 đã được tạo.", orders = 24, revenue = 7200000 }),
            _                => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'report'")
        });

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Handlers/Reports/ReportHandler.cs
git add tests/BotSyntax.Handlers.Tests/Reports/ReportHandlerTests.cs
git commit -m "feat: add ReportHandler stub with hyphenated action support"
```

---

## Task 11: CartHandler

**Files:**
- Create: `src/BotSyntax.Handlers/Cart/CartHandler.cs`
- Create: `tests/BotSyntax.Handlers.Tests/Cart/CartHandlerTests.cs`

Actions: `add`, `view`, `voucher`, `payment`, `checkout`

- [ ] **Step 1: Write failing test**

Create `tests/BotSyntax.Handlers.Tests/Cart/CartHandlerTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: FAIL — `BotSyntax.Handlers.Cart.CartHandler` not found.

- [ ] **Step 3: Implement CartHandler**

Create `src/BotSyntax.Handlers/Cart/CartHandler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Handlers.Cart;

public sealed class CartHandler : IDomainHandler
{
    public string Domain => "cart";

    public Task<ExecutionResult> HandleAsync(RootCommand command) =>
        Task.FromResult(command.Action switch
        {
            "add"      => ExecutionResult.Ok(new { sku = Str(command.Params, "sku"), quantity = 1, cart_total = 299000, items_count = 1, message = "Đã thêm sản phẩm vào giỏ hàng." }),
            "view"     => ExecutionResult.Ok(MockCart()),
            "voucher"  => ExecutionResult.Ok(new { voucher_code = Str(command.Params, "voucher_code"), discount = 50000, cart_total_after = 249000, message = "Mã giảm giá đã được áp dụng." }),
            "payment"  => ExecutionResult.Ok(new { methods = new[] { new { code = "COD", name = "Thanh toán khi nhận hàng" }, new { code = "MOMO", name = "Ví MoMo" }, new { code = "ZALOPAY", name = "ZaloPay" }, new { code = "BANK_TRANSFER", name = "Chuyển khoản ngân hàng" }, new { code = "VNPAY", name = "VNPay" }, new { code = "CREDIT_CARD", name = "Thẻ tín dụng / ghi nợ" } } }),
            "checkout" => ExecutionResult.Ok(new { order_id = "10235", status = "PENDING", total = 299000, payment_method = Str(command.Params, "method", "COD"), message = "Đơn hàng đã được đặt thành công!" }),
            _          => ExecutionResult.Error($"Unknown action '{command.Action}' for domain 'cart'")
        });

    private static object MockCart() => new
    {
        items       = new[] { new { sku = "AO-HOODIE-L-DEN", name = "Áo Hoodie Đen L", qty = 1, price = 299000 } },
        items_count = 1,
        subtotal    = 299000,
        discount    = 0,
        total       = 299000,
        voucher     = (string?)null
    };

    private static string Str(Dictionary<string, object> p, string key, string fallback = "mock") =>
        p.TryGetValue(key, out var v) ? v.ToString()! : fallback;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```bash
dotnet test tests/BotSyntax.Handlers.Tests -v minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Handlers/Cart/CartHandler.cs
git add tests/BotSyntax.Handlers.Tests/Cart/CartHandlerTests.cs
git commit -m "feat: add CartHandler stub — all 10 domain handlers complete"
```

---

## Final Verification

After all tasks complete, run both test suites together:

```bash
dotnet test -v minimal
```

Expected: **all tests in both `BotSyntax.Core.Tests` and `BotSyntax.Handlers.Tests` PASS**.
