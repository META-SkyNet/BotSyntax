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
