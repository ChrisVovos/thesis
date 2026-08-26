using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Identity.Commands;

/// <summary>Creates a user account and assigns their initial roles.</summary>
/// <param name="Email">The login identifier.</param>
/// <param name="DisplayName">The human readable name.</param>
/// <param name="Password">The initial plaintext password.</param>
/// <param name="RoleIds">The roles to assign.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record CreateUserCommand(
    string Email,
    string DisplayName,
    string Password,
    IReadOnlyList<Guid> RoleIds) : ICommand<Result<Guid>>;

/// <summary>Replaces the profile and role assignment of a user.</summary>
/// <param name="UserId">The user to update.</param>
/// <param name="Email">The new login identifier.</param>
/// <param name="DisplayName">The new human readable name.</param>
/// <param name="RoleIds">The roles the user should hold afterwards.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record UpdateUserCommand(
    Guid UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<Guid> RoleIds) : ICommand<Result>;

/// <summary>Activates or deactivates a user account.</summary>
/// <param name="UserId">The user to change.</param>
/// <param name="IsActive">Whether the user may sign in.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : ICommand<Result>;

/// <summary>Replaces a user's password and revokes their outstanding sessions.</summary>
/// <param name="UserId">The user to change.</param>
/// <param name="NewPassword">The new plaintext password.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword) : ICommand<Result>;

/// <summary>Validates <see cref="CreateUserCommand"/>.</summary>
public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateUserCommandValidator"/> class.</summary>
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress()
            .MaximumLength(EmailAddress.MaxLength);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(DisplayName.MaxLength);
        RuleFor(command => command.Password).ApplyPasswordPolicy();
        RuleFor(command => command.RoleIds).NotEmpty();
    }
}

/// <summary>Validates <see cref="UpdateUserCommand"/>.</summary>
public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateUserCommandValidator"/> class.</summary>
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Email).NotEmpty().EmailAddress()
            .MaximumLength(EmailAddress.MaxLength);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(DisplayName.MaxLength);
        RuleFor(command => command.RoleIds).NotEmpty();
    }
}

/// <summary>Validates <see cref="ResetUserPasswordCommand"/>.</summary>
public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ResetUserPasswordCommandValidator"/> class.</summary>
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.NewPassword).ApplyPasswordPolicy();
    }
}

/// <summary>Handles <see cref="CreateUserCommand"/>.</summary>
/// <param name="users">The user repository.</param>
/// <param name="roles">The role repository.</param>
/// <param name="passwordHasher">The password hasher.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class CreateUserCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        if (await users.EmailExistsAsync(email.Normalized, null, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict(
                "user.email_taken",
                "An account with that e-mail address already exists."));
        }

        var roleIds = request.RoleIds.Select(id => new RoleId(id)).Distinct().ToList();
        if ((await roles.FindMissingAsync(roleIds, cancellationToken)).Count > 0)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "role.not_found",
                "One or more of the supplied roles do not exist."));
        }

        var user = User.Create(
            email,
            DisplayName.Create(request.DisplayName),
            passwordHasher.Hash(request.Password));
        user.ReplaceRoles(roleIds);

        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(user.Id.Value);
    }
}

/// <summary>Handles <see cref="UpdateUserCommand"/>.</summary>
/// <param name="users">The user repository.</param>
/// <param name="roles">The role repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class UpdateUserCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUserCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var user = await users.GetAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("user.not_found", "The user does not exist."));
        }

        var email = EmailAddress.Create(request.Email);
        if (await users.EmailExistsAsync(email.Normalized, userId, cancellationToken))
        {
            return Result.Failure(Error.Conflict(
                "user.email_taken",
                "An account with that e-mail address already exists."));
        }

        var roleIds = request.RoleIds.Select(id => new RoleId(id)).Distinct().ToList();
        if ((await roles.FindMissingAsync(roleIds, cancellationToken)).Count > 0)
        {
            return Result.Failure(Error.NotFound(
                "role.not_found",
                "One or more of the supplied roles do not exist."));
        }

        user.UpdateProfile(email, DisplayName.Create(request.DisplayName));
        user.ReplaceRoles(roleIds);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="SetUserActiveCommand"/>.</summary>
/// <param name="users">The user repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
/// <param name="clock">The clock supplying the change instant.</param>
internal sealed class SetUserActiveCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<SetUserActiveCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        SetUserActiveCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        if (!request.IsActive && currentUser.UserId == userId)
        {
            return Result.Failure(Error.Conflict(
                "user.cannot_deactivate_self",
                "An administrator cannot deactivate their own account."));
        }

        var user = await users.GetAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("user.not_found", "The user does not exist."));
        }

        if (request.IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate(clock.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="ResetUserPasswordCommand"/>.</summary>
/// <param name="users">The user repository.</param>
/// <param name="passwordHasher">The password hasher.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="clock">The clock supplying the change instant.</param>
internal sealed class ResetUserPasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<ResetUserPasswordCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        ResetUserPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(new UserId(request.UserId), cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("user.not_found", "The user does not exist."));
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword), clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
