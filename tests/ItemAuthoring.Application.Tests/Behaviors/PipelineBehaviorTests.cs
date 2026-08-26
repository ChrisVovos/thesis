using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Behaviors;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Tests.TestDoubles;
using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ItemAuthoring.Application.Tests.Behaviors;

public sealed class PipelineBehaviorTests
{
    [RequiresPermission(Permissions.ItemsCreate)]
    private sealed record GuardedCommand(string Name) : ICommand<Result<Guid>>;

    [AllowAnonymousRequest]
    private sealed record OpenCommand : ICommand<Result>;

    private sealed class GuardedCommandValidator : AbstractValidator<GuardedCommand>
    {
        public GuardedCommandValidator() => RuleFor(command => command.Name).NotEmpty();
    }

    private static Task<Result<Guid>> Succeed() => Task.FromResult(Result.Success(Guid.Empty));

    [Fact]
    public async Task Validation_short_circuits_with_per_field_details()
    {
        var behavior = new ValidationBehavior<GuardedCommand, Result<Guid>>(
            [new GuardedCommandValidator()]);

        var result = await behavior.HandleAsync(new GuardedCommand(string.Empty), Succeed, default);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("validation.failed");
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Details.ShouldNotBeNull().ShouldContainKey(nameof(GuardedCommand.Name));
    }

    [Fact]
    public async Task Validation_lets_a_valid_request_through()
    {
        var behavior = new ValidationBehavior<GuardedCommand, Result<Guid>>(
            [new GuardedCommandValidator()]);

        var result = await behavior.HandleAsync(new GuardedCommand("valid"), Succeed, default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task An_anonymous_caller_is_rejected_before_the_handler_runs()
    {
        var handlerRan = false;
        var behavior = new AuthorizationBehavior<GuardedCommand, Result<Guid>>(
            FakeCurrentUser.Anonymous());

        var result = await behavior.HandleAsync(
            new GuardedCommand("valid"),
            () =>
            {
                handlerRan = true;
                return Succeed();
            },
            default);

        handlerRan.ShouldBeFalse();
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
        result.Error.Code.ShouldBe("auth.required");
    }

    [Fact]
    public async Task A_caller_without_the_declared_permission_is_forbidden()
    {
        var behavior = new AuthorizationBehavior<GuardedCommand, Result<Guid>>(
            FakeCurrentUser.With(Permissions.ItemsRead));

        var result = await behavior.HandleAsync(new GuardedCommand("valid"), Succeed, default);

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        result.Error.Message.ShouldContain(Permissions.ItemsCreate);
    }

    [Fact]
    public async Task A_caller_holding_the_permission_reaches_the_handler()
    {
        var behavior = new AuthorizationBehavior<GuardedCommand, Result<Guid>>(
            FakeCurrentUser.With(Permissions.ItemsCreate));

        var result = await behavior.HandleAsync(new GuardedCommand("valid"), Succeed, default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task A_request_marked_anonymous_bypasses_authentication()
    {
        var behavior = new AuthorizationBehavior<OpenCommand, Result>(FakeCurrentUser.Anonymous());

        var result = await behavior.HandleAsync(
            new OpenCommand(),
            () => Task.FromResult(Result.Success()),
            default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task A_violated_domain_rule_becomes_a_conflict_carrying_its_code()
    {
        var behavior = new DomainExceptionBehavior<OpenCommand, Result>(
            NullLogger<DomainExceptionBehavior<OpenCommand, Result>>.Instance);

        var result = await behavior.HandleAsync(
            new OpenCommand(),
            () => throw new DomainException("item.not_editable", "Not editable."),
            default);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("item.not_editable");
    }

    [Fact]
    public async Task An_unexpected_exception_is_not_swallowed()
    {
        var behavior = new DomainExceptionBehavior<OpenCommand, Result>(
            NullLogger<DomainExceptionBehavior<OpenCommand, Result>>.Instance);

        await Should.ThrowAsync<InvalidOperationException>(() => behavior.HandleAsync(
            new OpenCommand(),
            () => throw new InvalidOperationException("boom"),
            default));
    }

    [Fact]
    public void The_result_factory_builds_a_failure_of_the_requested_shape()
    {
        var error = Error.NotFound("item.not_found", "Missing.");

        ResultFactory.Failure<Result>(error).Error.ShouldBe(error);
        ResultFactory.Failure<Result<Guid>>(error).Error.ShouldBe(error);
    }

    [Fact]
    public void The_result_factory_refuses_a_response_type_it_cannot_build()
        => Should.Throw<InvalidOperationException>(
            () => ResultFactory.Failure<string>(Error.NotFound("x", "y")));

    [Fact]
    public void Reading_the_value_of_a_failed_result_is_a_programming_error()
        => Should.Throw<InvalidOperationException>(
            () => Result.Failure<Guid>(Error.NotFound("x", "y")).Value);
}
