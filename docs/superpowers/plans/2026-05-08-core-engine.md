# BotSyntax Core Engine — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the core pipeline — IChatBox → Compiler → CompactString → RootCommand JSON → Executor with role-based permission check.

**Architecture:** Two IChatBox implementations (NLUChatBox for natural language, SlashChatBox for slash commands) compile user input into `context(role, userId).domain.action(params)` compact strings. A custom parser converts compact strings to RootCommand records. BotExecutor checks permissions via PermissionTable before dispatching to domain handlers.

**Tech Stack:** .NET 8, C# 12, xUnit 2.x, System.Text.Json, FluentAssertions

---

## Scope

**This plan (Plan 1):** Core engine only — interfaces, models, parser, compiler, permission system, executor scaffold.  
**Plan 2:** 11 domain handler implementations.  
**Plan 3:** AI Agent NLU integration for NLUChatBox.

---

## File Structure

```
BotSyntax.sln
src/
  BotSyntax.Core/
    BotSyntax.Core.csproj
    Interfaces/
      IChatBox.cs          ← Parse(input, role, userId) → CompactString
      ICompiler.cs         ← Compile(input) → CompilerResult
      IExecutor.cs         ← Execute(CompactString) → ExecutionResult
      IDomainHandler.cs    ← Handle(RootCommand) → HandlerResult
    Models/
      CompactString.cs     ← Parsed form: Role, UserId, Domain, Action, Params
      RootCommand.cs       ← JSON-serializable: root, domain, action, params, context, meta
      CompilerResult.cs    ← Ok(CompactString) | Error(CompilerError)
      CompilerError.cs     ← ErrorCode enum + message
      ExecutionResult.cs   ← Ok(payload) | Denied | Error
    ChatBox/
      SlashChatBox.cs      ← IChatBox for slash commands
      NLUChatBox.cs        ← IChatBox for natural language (algorithm mode)
    Compiler/
      SlashCommandCompiler.cs   ← /domain action param --key=val
      AlgorithmNLUCompiler.cs   ← pattern matching → CompactString
      IntentPattern.cs          ← pattern definition used by AlgorithmNLUCompiler
    Parser/
      CompactStringParser.cs    ← "context('staff','e1').order.status(10234)" → CompactString
    Permission/
      PermissionTable.cs        ← role → allowed domain.action set
      PermissionTableBuilder.cs ← fluent builder for permission rules
    Executor/
      BotExecutor.cs            ← check permission, dispatch to IDomainHandler

tests/
  BotSyntax.Core.Tests/
    BotSyntax.Core.Tests.csproj
    Parser/
      CompactStringParserTests.cs
    Compiler/
      SlashCommandCompilerTests.cs
      AlgorithmNLUCompilerTests.cs
    Permission/
      PermissionTableTests.cs
    ChatBox/
      SlashChatBoxTests.cs
      NLUChatBoxTests.cs
    Executor/
      BotExecutorTests.cs
    Helpers/
      TestPermissionTable.cs    ← shared minimal PermissionTable for tests
```

---

## Task 1: Solution & Project Setup

**Files:**
- Create: `BotSyntax.sln`
- Create: `src/BotSyntax.Core/BotSyntax.Core.csproj`
- Create: `tests/BotSyntax.Core.Tests/BotSyntax.Core.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

```bash
cd D:/META/Projects/BotSyntax
dotnet new sln -n BotSyntax
dotnet new classlib -n BotSyntax.Core -o src/BotSyntax.Core --framework net8.0
dotnet new xunit -n BotSyntax.Core.Tests -o tests/BotSyntax.Core.Tests --framework net8.0
dotnet sln add src/BotSyntax.Core/BotSyntax.Core.csproj
dotnet sln add tests/BotSyntax.Core.Tests/BotSyntax.Core.Tests.csproj
dotnet add tests/BotSyntax.Core.Tests/BotSyntax.Core.Tests.csproj reference src/BotSyntax.Core/BotSyntax.Core.csproj
```

- [ ] **Step 2: Add FluentAssertions to test project**

```bash
dotnet add tests/BotSyntax.Core.Tests package FluentAssertions --version 6.*
```

- [ ] **Step 3: Delete boilerplate files**

```bash
rm src/BotSyntax.Core/Class1.cs
rm tests/BotSyntax.Core.Tests/UnitTest1.cs
```

- [ ] **Step 4: Verify build**

```bash
dotnet build
```
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add BotSyntax.sln src/ tests/
git commit -m "chore: initialize solution with Core and Tests projects"
```

---

## Task 2: Core Models

**Files:**
- Create: `src/BotSyntax.Core/Models/CompactString.cs`
- Create: `src/BotSyntax.Core/Models/RootCommand.cs`
- Create: `src/BotSyntax.Core/Models/CompilerError.cs`
- Create: `src/BotSyntax.Core/Models/CompilerResult.cs`
- Create: `src/BotSyntax.Core/Models/ExecutionResult.cs`

- [ ] **Step 1: Write tests for CompactString**

Create `tests/BotSyntax.Core.Tests/Models/CompactStringTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test — verify it fails**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "CompactStringTests"
```
Expected: FAIL — `CompactString` type not found

- [ ] **Step 3: Implement CompactString**

Create `src/BotSyntax.Core/Models/CompactString.cs`:

```csharp
namespace BotSyntax.Core.Models;

public sealed record CompactString(
    string Role,
    string? UserId,
    string Domain,
    string Action,
    Dictionary<string, object> Params)
{
    public bool IsGuest => UserId is null;

    public string ToText()
    {
        var identity = IsGuest ? "guest" : $"'{UserId}'";
        var paramStr = Params.Count == 0
            ? ""
            : string.Join(", ", Params.Select(p => $"{p.Key}: {FormatValue(p.Value)}"));
        return $"context('{Role}', {identity}).{Domain}.{Action}({paramStr})";
    }

    private static string FormatValue(object value) =>
        value is string s ? $"'{s}'" : value.ToString()!;
}
```

- [ ] **Step 4: Run test — verify it passes**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "CompactStringTests"
```
Expected: PASS — 4 tests

- [ ] **Step 5: Implement remaining models**

Create `src/BotSyntax.Core/Models/CompilerError.cs`:

```csharp
namespace BotSyntax.Core.Models;

public enum CompilerErrorCode
{
    UnknownIntent,
    MissingParam,
    InvalidParam,
    AmbiguousIntent,
    CompilerError
}

public sealed record CompilerError(CompilerErrorCode Code, string Message);
```

Create `src/BotSyntax.Core/Models/CompilerResult.cs`:

```csharp
namespace BotSyntax.Core.Models;

public sealed class CompilerResult
{
    public bool IsSuccess { get; }
    public CompactString? Value { get; }
    public CompilerError? Error { get; }

    private CompilerResult(CompactString value) { IsSuccess = true; Value = value; }
    private CompilerResult(CompilerError error) { IsSuccess = false; Error = error; }

    public static CompilerResult Ok(CompactString value) => new(value);
    public static CompilerResult Fail(CompilerErrorCode code, string message) =>
        new(new CompilerError(code, message));
}
```

Create `src/BotSyntax.Core/Models/ExecutionResult.cs`:

```csharp
namespace BotSyntax.Core.Models;

public enum ExecutionStatus { Ok, PermissionDenied, NotFound, Error }

public sealed class ExecutionResult
{
    public ExecutionStatus Status { get; }
    public object? Payload { get; }
    public string? Message { get; }

    private ExecutionResult(ExecutionStatus status, object? payload, string? message)
    { Status = status; Payload = payload; Message = message; }

    public static ExecutionResult Ok(object payload) =>
        new(ExecutionStatus.Ok, payload, null);
    public static ExecutionResult Denied(string message) =>
        new(ExecutionStatus.PermissionDenied, null, message);
    public static ExecutionResult NotFound(string message) =>
        new(ExecutionStatus.NotFound, null, message);
    public static ExecutionResult Error(string message) =>
        new(ExecutionStatus.Error, null, message);
}
```

Create `src/BotSyntax.Core/Models/RootCommand.cs`:

```csharp
namespace BotSyntax.Core.Models;

public sealed record RootCommand(
    string Root,
    string Domain,
    string Action,
    Dictionary<string, object> Params,
    RootCommandContext Context,
    RootCommandMeta Meta);

public sealed record RootCommandContext(
    string Role,
    string? Identity,
    string Channel,
    string? SessionId,
    DateTimeOffset Timestamp);

public sealed record RootCommandMeta(
    string InputType,
    string Compiler,
    double? Confidence,
    string RawInput);
```

- [ ] **Step 6: Build**

```bash
dotnet build
```
Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add src/ tests/
git commit -m "feat: add core models — CompactString, RootCommand, CompilerResult, ExecutionResult"
```

---

## Task 3: Core Interfaces

**Files:**
- Create: `src/BotSyntax.Core/Interfaces/IChatBox.cs`
- Create: `src/BotSyntax.Core/Interfaces/ICompiler.cs`
- Create: `src/BotSyntax.Core/Interfaces/IExecutor.cs`
- Create: `src/BotSyntax.Core/Interfaces/IDomainHandler.cs`

- [ ] **Step 1: Create interfaces**

Create `src/BotSyntax.Core/Interfaces/IChatBox.cs`:

```csharp
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Interfaces;

public interface IChatBox
{
    CompilerResult Parse(string input, string role, string? userId);
}
```

Create `src/BotSyntax.Core/Interfaces/ICompiler.cs`:

```csharp
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Interfaces;

public interface ICompiler
{
    CompilerResult Compile(string input, string role, string? userId);
}
```

Create `src/BotSyntax.Core/Interfaces/IExecutor.cs`:

```csharp
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Interfaces;

public interface IExecutor
{
    Task<ExecutionResult> ExecuteAsync(CompactString command, string channel, string? sessionId);
}
```

Create `src/BotSyntax.Core/Interfaces/IDomainHandler.cs`:

```csharp
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Interfaces;

public interface IDomainHandler
{
    string Domain { get; }
    Task<ExecutionResult> HandleAsync(RootCommand command);
}
```

- [ ] **Step 2: Build**

```bash
dotnet build
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/BotSyntax.Core/Interfaces/
git commit -m "feat: add IChatBox, ICompiler, IExecutor, IDomainHandler interfaces"
```

---

## Task 4: CompactString Parser

**Files:**
- Create: `src/BotSyntax.Core/Parser/CompactStringParser.cs`
- Test: `tests/BotSyntax.Core.Tests/Parser/CompactStringParserTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/BotSyntax.Core.Tests/Parser/CompactStringParserTests.cs`:

```csharp
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
        var result = _parser.Parse("context('staff', 'emp001').order.status(10234)");

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
        var result = _parser.Parse("context('customer', guest).order.status(10234)");

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
        var result = _parser.Parse("context(\"staff\", \"emp001\").order.status(10234)");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be("staff");
    }
}
```

- [ ] **Step 2: Run — verify fails**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "CompactStringParserTests"
```
Expected: FAIL — `CompactStringParser` not found

- [ ] **Step 3: Implement CompactStringParser**

Create `src/BotSyntax.Core/Parser/CompactStringParser.cs`:

```csharp
using System.Text.RegularExpressions;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Parser;

public sealed class CompactStringParser
{
    // context('role', 'userId').domain.action(params)
    // context('role', guest).domain.action(params)
    private static readonly Regex _pattern = new(
        @"^context\(['""](?<role>[^'""]+)['""]\s*,\s*(?:guest|['""](?<userId>[^'""]*)['""])\)\." +
        @"(?<domain>[a-z]+)\.(?<action>[a-z]+)\((?<params>[^)]*)\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CompilerResult Parse(string input)
    {
        var match = _pattern.Match(input.Trim());
        if (!match.Success)
            return CompilerResult.Fail(CompilerErrorCode.InvalidParam,
                $"Invalid compact string format: '{input}'");

        var role   = match.Groups["role"].Value;
        var userId = match.Groups["userId"].Success ? match.Groups["userId"].Value : null;
        var domain = match.Groups["domain"].Value;
        var action = match.Groups["action"].Value;
        var rawParams = match.Groups["params"].Value.Trim();

        var parseResult = ParseParams(rawParams);
        if (parseResult.IsFailure)
            return CompilerResult.Fail(CompilerErrorCode.InvalidParam, parseResult.ErrorMessage!);

        return CompilerResult.Ok(new CompactString(role, userId, domain, action, parseResult.Params!));
    }

    private static (bool IsFailure, string? ErrorMessage, Dictionary<string, object>? Params) ParseParams(string raw)
    {
        var dict = new Dictionary<string, object>();
        if (string.IsNullOrWhiteSpace(raw))
            return (false, null, dict);

        foreach (var token in SplitParams(raw))
        {
            var kv = token.Split(':', 2);
            if (kv.Length != 2)
                return (true, $"Invalid param token: '{token}'", null);

            var key   = kv[0].Trim();
            var value = kv[1].Trim();

            if ((value.StartsWith("'") && value.EndsWith("'")) ||
                (value.StartsWith("\"") && value.EndsWith("\"")))
                dict[key] = value[1..^1];
            else if (int.TryParse(value, out var intVal))
                dict[key] = intVal;
            else if (double.TryParse(value, out var dblVal))
                dict[key] = dblVal;
            else
                dict[key] = value;
        }
        return (false, null, dict);
    }

    private static IEnumerable<string> SplitParams(string raw)
    {
        // Split by comma but not inside quotes
        var tokens = new List<string>();
        var depth = 0; var inQuote = false; char quoteChar = '\0';
        var current = new System.Text.StringBuilder();

        foreach (var c in raw)
        {
            if (!inQuote && (c == '\'' || c == '"')) { inQuote = true; quoteChar = c; }
            else if (inQuote && c == quoteChar)      { inQuote = false; }
            else if (!inQuote && c == ',')           { tokens.Add(current.ToString().Trim()); current.Clear(); continue; }
            current.Append(c);
        }
        if (current.Length > 0) tokens.Add(current.ToString().Trim());
        return tokens;
    }
}
```

- [ ] **Step 4: Run — verify passes**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "CompactStringParserTests"
```
Expected: PASS — 7 tests

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Core/Parser/ tests/BotSyntax.Core.Tests/Parser/
git commit -m "feat: add CompactStringParser with single/double quote support and guest keyword"
```

---

## Task 5: Permission Table

**Files:**
- Create: `src/BotSyntax.Core/Permission/PermissionTable.cs`
- Create: `src/BotSyntax.Core/Permission/PermissionTableBuilder.cs`
- Create: `tests/BotSyntax.Core.Tests/Helpers/TestPermissionTable.cs`
- Test: `tests/BotSyntax.Core.Tests/Permission/PermissionTableTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/BotSyntax.Core.Tests/Permission/PermissionTableTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run — verify fails**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "PermissionTableTests"
```
Expected: FAIL

- [ ] **Step 3: Implement PermissionTable and builder**

Create `src/BotSyntax.Core/Permission/PermissionTable.cs`:

```csharp
namespace BotSyntax.Core.Permission;

public sealed class PermissionTable
{
    private readonly Dictionary<string, HashSet<string>> _rules;  // role → "domain.action"

    internal PermissionTable(Dictionary<string, HashSet<string>> rules)
        => _rules = rules;

    public bool IsAllowed(string role, string domain, string action)
    {
        if (!_rules.TryGetValue(role, out var allowed)) return false;
        return allowed.Contains("*.*")
            || allowed.Contains($"*.{action}")
            || allowed.Contains($"{domain}.*")
            || allowed.Contains($"{domain}.{action}");
    }
}
```

Create `src/BotSyntax.Core/Permission/PermissionTableBuilder.cs`:

```csharp
namespace BotSyntax.Core.Permission;

public sealed class PermissionTableBuilder
{
    private readonly Dictionary<string, HashSet<string>> _rules = new();
    private readonly List<(string child, string parent)> _inheritances = new();

    public PermissionTableBuilder Allow(string role, string domain, string action)
    {
        if (!_rules.ContainsKey(role)) _rules[role] = new HashSet<string>();
        _rules[role].Add($"{domain}.{action}");
        return this;
    }

    public PermissionTableBuilder Inherit(string childRole, string parentRole)
    {
        _inheritances.Add((childRole, parentRole));
        return this;
    }

    public PermissionTable Build()
    {
        // Resolve inheritances (simple single-pass — parent must be defined before child)
        foreach (var (child, parent) in _inheritances)
        {
            if (!_rules.ContainsKey(child))  _rules[child]  = new HashSet<string>();
            if (_rules.TryGetValue(parent, out var parentRules))
                foreach (var rule in parentRules) _rules[child].Add(rule);
        }
        return new PermissionTable(_rules);
    }
}
```

Create `tests/BotSyntax.Core.Tests/Helpers/TestPermissionTable.cs`:

```csharp
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
```

- [ ] **Step 4: Run — verify passes**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "PermissionTableTests"
```
Expected: PASS — 5 tests

- [ ] **Step 5: Commit**

```bash
git add src/BotSyntax.Core/Permission/ tests/BotSyntax.Core.Tests/
git commit -m "feat: add PermissionTable with inheritance and wildcard support"
```

---

## Task 6: SlashChatBox

**Files:**
- Create: `src/BotSyntax.Core/Compiler/SlashCommandCompiler.cs`
- Create: `src/BotSyntax.Core/ChatBox/SlashChatBox.cs`
- Test: `tests/BotSyntax.Core.Tests/ChatBox/SlashChatBoxTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/BotSyntax.Core.Tests/ChatBox/SlashChatBoxTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run — verify fails**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "SlashChatBoxTests"
```
Expected: FAIL

- [ ] **Step 3: Implement SlashCommandCompiler**

Create `src/BotSyntax.Core/Compiler/SlashCommandCompiler.cs`:

```csharp
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Compiler;

public sealed class SlashCommandCompiler
{
    // Positional param names per domain.action (first positional arg)
    private static readonly Dictionary<string, string> _positionalParams = new()
    {
        ["order.status"]     = "order_id",
        ["order.confirm"]    = "order_id",
        ["order.cancel"]     = "order_id",
        ["order.note"]       = "order_id",
        ["order.update"]     = "order_id",
        ["ship.status"]      = "order_id",
        ["ship.fail"]        = "order_id",
        ["ship.address"]     = "order_id",
        ["returns.status"]   = "return_id",
        ["returns.approve"]  = "return_id",
        ["returns.reject"]   = "return_id",
        ["product.info"]     = "sku",
        ["product.stock"]    = "sku",
        ["warranty.status"]  = "warranty_code",
        ["debt.confirm"]     = "debt_id",
        ["stock.import.confirm"] = "import_id",
        ["customer.info"]    = "customer_id",
        ["customer.orders"]  = "customer_id",
        ["customer.note"]    = "customer_id",
        ["customer.search"]  = "keyword",
    };

    public CompilerResult Compile(string input, string role, string? userId)
    {
        if (!input.StartsWith("/"))
            return CompilerResult.Fail(CompilerErrorCode.UnknownIntent, "Slash commands must start with '/'");

        var tokens = input[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
            return CompilerResult.Fail(CompilerErrorCode.MissingParam, "Command requires at least domain and action");

        var domain = tokens[0].ToLower();
        var action = tokens[1].ToLower();
        var key    = $"{domain}.{action}";
        var @params = new Dictionary<string, object>();

        for (var i = 2; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.StartsWith("--"))
            {
                var kv = token[2..].Split('=', 2);
                if (kv.Length == 2)
                    @params[kv[0]] = TryParse(kv[1].Trim('"'));
                else
                    @params[kv[0]] = true;
            }
            else if (i == 2 && _positionalParams.TryGetValue(key, out var paramName))
            {
                @params[paramName] = TryParse(token);
            }
        }

        return CompilerResult.Ok(new CompactString(role, userId, domain, action, @params));
    }

    private static object TryParse(string value) =>
        int.TryParse(value, out var i)    ? i :
        double.TryParse(value, out var d) ? d :
        (object)value;
}
```

- [ ] **Step 4: Implement SlashChatBox**

Create `src/BotSyntax.Core/ChatBox/SlashChatBox.cs`:

```csharp
using BotSyntax.Core.Compiler;
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.ChatBox;

public sealed class SlashChatBox : IChatBox
{
    private readonly SlashCommandCompiler _compiler = new();

    public CompilerResult Parse(string input, string role, string? userId)
        => _compiler.Compile(input, role, userId);
}
```

- [ ] **Step 5: Run — verify passes**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "SlashChatBoxTests"
```
Expected: PASS — 6 tests

- [ ] **Step 6: Commit**

```bash
git add src/BotSyntax.Core/Compiler/ src/BotSyntax.Core/ChatBox/SlashChatBox.cs tests/
git commit -m "feat: add SlashChatBox with SlashCommandCompiler — role injected from session"
```

---

## Task 7: NLUChatBox (Algorithm Mode)

**Files:**
- Create: `src/BotSyntax.Core/Compiler/IntentPattern.cs`
- Create: `src/BotSyntax.Core/Compiler/AlgorithmNLUCompiler.cs`
- Create: `src/BotSyntax.Core/ChatBox/NLUChatBox.cs`
- Test: `tests/BotSyntax.Core.Tests/ChatBox/NLUChatBoxTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/BotSyntax.Core.Tests/ChatBox/NLUChatBoxTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run — verify fails**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "NLUChatBoxTests"
```
Expected: FAIL

- [ ] **Step 3: Implement IntentPattern**

Create `src/BotSyntax.Core/Compiler/IntentPattern.cs`:

```csharp
using System.Text.RegularExpressions;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Compiler;

public sealed class IntentPattern
{
    private readonly Regex _regex;
    private readonly string _domain;
    private readonly string _action;
    private readonly Func<Match, Dictionary<string, object>>? _extractParams;

    public IntentPattern(string domain, string action, string pattern,
        Func<Match, Dictionary<string, object>>? extractParams = null)
    {
        _domain = domain;
        _action = action;
        _regex  = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        _extractParams = extractParams;
    }

    public CompilerResult? TryMatch(string input, string role, string? userId)
    {
        var match = _regex.Match(input);
        if (!match.Success) return null;

        var @params = _extractParams?.Invoke(match) ?? new Dictionary<string, object>();
        return CompilerResult.Ok(new CompactString(role, userId, _domain, _action, @params));
    }
}
```

- [ ] **Step 4: Implement AlgorithmNLUCompiler**

Create `src/BotSyntax.Core/Compiler/AlgorithmNLUCompiler.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.Compiler;

public sealed class AlgorithmNLUCompiler : ICompiler
{
    private readonly List<IntentPattern> _patterns;

    public AlgorithmNLUCompiler(IEnumerable<IntentPattern> patterns)
        => _patterns = patterns.ToList();

    public CompilerResult Compile(string input, string role, string? userId)
    {
        // Normalize: strip diacritics fallback handled by pattern alternation
        var normalized = input.Trim().ToLower();

        foreach (var pattern in _patterns)
        {
            var result = pattern.TryMatch(normalized, role, userId);
            if (result is not null) return result;
        }

        return CompilerResult.Fail(CompilerErrorCode.UnknownIntent,
            "Không hiểu yêu cầu. Bạn có thể mô tả rõ hơn không?");
    }

    public static AlgorithmNLUCompiler CreateDefault() => new(DefaultPatterns.All);
}
```

Create `src/BotSyntax.Core/Compiler/DefaultPatterns.cs`:

```csharp
using System.Text.RegularExpressions;

namespace BotSyntax.Core.Compiler;

internal static class DefaultPatterns
{
    public static readonly IEnumerable<IntentPattern> All = new[]
    {
        // Order status
        new IntentPattern("order", "status",
            @"(?:đơn|don|dh)\s+(\d+)|(?:kiểm tra|kiem tra)\s+(?:đơn|don)\s+(\d+)",
            m =>
            {
                var id = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                return new() { ["order_id"] = int.Parse(id) };
            }),

        new IntentPattern("order", "status",
            @"(?:đơn|don)\s+(?:hàng|hang)?\s*(?:của tôi|cua toi|đang ở đâu|dang o dau|ở đâu|o dau|rồi|roi|như thế nào|nhu the nao)"),

        // Returns
        new IntentPattern("returns", "create",
            @"(?:trả|tra)\s+(?:hàng|hang)|(?:hoàn tiền|hoan tien)|(?:đổi|doi)\s+(?:hàng|hang)"),

        // Warranty
        new IntentPattern("warranty", "status",
            @"(?:bảo hành|bao hanh|bh)\s*(\w+)?",
            m => m.Groups[1].Success
                ? new() { ["warranty_code"] = m.Groups[1].Value }
                : new()),

        // Product search
        new IntentPattern("product", "search",
            @"(?:tìm|tim|tìm kiếm|tim kiem)\s+(.+)",
            m => new() { ["keyword"] = m.Groups[1].Value.Trim() }),

        new IntentPattern("product", "search",
            @"(?:gợi ý|goi y|recommend)\s+(.+)",
            m => new() { ["keyword"] = m.Groups[1].Value.Trim() }),

        // Shipping
        new IntentPattern("ship", "status",
            @"(?:vận đơn|van don|giao hàng|giao hang)\s*(\d+)?|(?:shipper|hàng tôi|hang toi)",
            m => m.Groups[1].Success
                ? new() { ["order_id"] = int.Parse(m.Groups[1].Value) }
                : new()),

        // Cart
        new IntentPattern("cart", "view",
            @"(?:giỏ hàng|gio hang|giỏ của tôi|gio cua toi)"),

        // Support
        new IntentPattern("customer", "complaint.create",
            @"(?:khiếu nại|khieu nai|phản ánh|phan anh|không hài lòng|khong hai long|liên hệ|lien he)"),
    };
}
```

- [ ] **Step 5: Implement NLUChatBox**

Create `src/BotSyntax.Core/ChatBox/NLUChatBox.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;

namespace BotSyntax.Core.ChatBox;

public sealed class NLUChatBox : IChatBox
{
    private readonly ICompiler _compiler;

    public NLUChatBox(ICompiler compiler)
        => _compiler = compiler;

    public CompilerResult Parse(string input, string role, string? userId)
        => _compiler.Compile(input, role, userId);
}
```

- [ ] **Step 6: Run — verify passes**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "NLUChatBoxTests"
```
Expected: PASS — 8 tests

- [ ] **Step 7: Commit**

```bash
git add src/BotSyntax.Core/Compiler/ src/BotSyntax.Core/ChatBox/NLUChatBox.cs tests/
git commit -m "feat: add NLUChatBox with AlgorithmNLUCompiler and default intent patterns"
```

---

## Task 8: BotExecutor

**Files:**
- Create: `src/BotSyntax.Core/Executor/BotExecutor.cs`
- Test: `tests/BotSyntax.Core.Tests/Executor/BotExecutorTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/BotSyntax.Core.Tests/Executor/BotExecutorTests.cs`:

```csharp
using BotSyntax.Core.Executor;
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;
using BotSyntax.Core.Tests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace BotSyntax.Core.Tests.Executor;

public class BotExecutorTests
{
    private readonly IDomainHandler _handler = Substitute.For<IDomainHandler>();
    private readonly BotExecutor _executor;

    public BotExecutorTests()
    {
        _handler.Domain.Returns("order");
        _handler.HandleAsync(Arg.Any<RootCommand>())
            .Returns(ExecutionResult.Ok(new { status = "PENDING" }));
        _executor = new BotExecutor(TestPermissionTable.Build(), new[] { _handler });
    }

    [Fact]
    public async Task Execute_AllowedCommand_CallsHandler()
    {
        var cmd = new CompactString("staff", "emp001", "order", "status",
            new() { ["order_id"] = 10234 });

        var result = await _executor.ExecuteAsync(cmd, "web", "sess01");

        result.Status.Should().Be(ExecutionStatus.Ok);
        await _handler.Received(1).HandleAsync(Arg.Any<RootCommand>());
    }

    [Fact]
    public async Task Execute_DeniedCommand_ReturnsDenied()
    {
        var cmd = new CompactString("customer", "cus01", "report", "revenue",
            new());

        var result = await _executor.ExecuteAsync(cmd, "web", "sess01");

        result.Status.Should().Be(ExecutionStatus.PermissionDenied);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<RootCommand>());
    }

    [Fact]
    public async Task Execute_PassesRootCommandWithCorrectFields()
    {
        RootCommand? captured = null;
        _handler.HandleAsync(Arg.Do<RootCommand>(r => captured = r))
            .Returns(ExecutionResult.Ok(new { }));

        var cmd = new CompactString("staff", "emp001", "order", "status",
            new() { ["order_id"] = 10234 });

        await _executor.ExecuteAsync(cmd, "web", "sess01");

        captured!.Root.Should().Be(cmd.ToText());
        captured.Domain.Should().Be("order");
        captured.Params["order_id"].Should().Be(10234);
        captured.Context.Role.Should().Be("staff");
    }
}
```

- [ ] **Step 2: Add NSubstitute**

```bash
dotnet add tests/BotSyntax.Core.Tests package NSubstitute
```

- [ ] **Step 3: Run — verify fails**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "BotExecutorTests"
```
Expected: FAIL

- [ ] **Step 4: Implement BotExecutor**

Create `src/BotSyntax.Core/Executor/BotExecutor.cs`:

```csharp
using BotSyntax.Core.Interfaces;
using BotSyntax.Core.Models;
using BotSyntax.Core.Permission;

namespace BotSyntax.Core.Executor;

public sealed class BotExecutor : IExecutor
{
    private readonly PermissionTable _permissions;
    private readonly Dictionary<string, IDomainHandler> _handlers;

    public BotExecutor(PermissionTable permissions, IEnumerable<IDomainHandler> handlers)
    {
        _permissions = permissions;
        _handlers    = handlers.ToDictionary(h => h.Domain, h => h);
    }

    public async Task<ExecutionResult> ExecuteAsync(
        CompactString command, string channel, string? sessionId)
    {
        if (!_permissions.IsAllowed(command.Role, command.Domain, command.Action))
            return ExecutionResult.Denied(
                $"Role '{command.Role}' không có quyền thực hiện {command.Domain}.{command.Action}");

        if (!_handlers.TryGetValue(command.Domain, out var handler))
            return ExecutionResult.NotFound($"Domain handler '{command.Domain}' chưa được đăng ký");

        var root = new RootCommand(
            Root:    command.ToText(),
            Domain:  command.Domain,
            Action:  command.Action,
            Params:  command.Params,
            Context: new RootCommandContext(
                Role:      command.Role,
                Identity:  command.UserId,
                Channel:   channel,
                SessionId: sessionId,
                Timestamp: DateTimeOffset.UtcNow),
            Meta: new RootCommandMeta(
                InputType:  "compact",
                Compiler:   "algorithm",
                Confidence: null,
                RawInput:   command.ToText()));

        return await handler.HandleAsync(root);
    }
}
```

- [ ] **Step 5: Run — verify passes**

```bash
dotnet test tests/BotSyntax.Core.Tests --filter "BotExecutorTests"
```
Expected: PASS — 3 tests

- [ ] **Step 6: Run all tests**

```bash
dotnet test
```
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/BotSyntax.Core/Executor/ tests/BotSyntax.Core.Tests/Executor/
git commit -m "feat: add BotExecutor with permission check and domain handler dispatch"
```

---

## Self-Review

**Spec coverage check:**
- ✅ IChatBox interface — Task 3
- ✅ NLUChatBox (algorithm mode) — Task 7
- ✅ SlashChatBox — Task 6
- ✅ CompactString parser — Task 4
- ✅ context(role, userId) format — Task 2, 4
- ✅ guest keyword — Task 2, 4
- ✅ JSON RootCommand — Task 2
- ✅ Permission system (5 roles + inheritance) — Task 5
- ✅ BotExecutor dispatch — Task 8
- ⏭ AI Agent NLU mode — Plan 3
- ⏭ 11 domain handlers — Plan 2

**Placeholder scan:** No TBD, TODO, or incomplete steps found.

**Type consistency:**
- `CompilerResult.Ok(CompactString)` / `CompilerResult.Fail(code, message)` — consistent Tasks 2→4→6→7→8
- `IDomainHandler.Domain` + `IDomainHandler.HandleAsync(RootCommand)` — consistent Tasks 3→8
- `PermissionTable.IsAllowed(role, domain, action)` — consistent Tasks 5→8
