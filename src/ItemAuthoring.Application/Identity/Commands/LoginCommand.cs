using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Identity.Commands;

/// <summary>Exchanges an e-mail address and password for a token pair.</summary>
/// <param name="Email">The login identifier.</param>
/// <param name="Password">The plaintext password.</param>
[AllowAnonymousRequest]
public sealed record LoginCommand(string Email, string Password)
    : ICommand<Result<AuthenticationResultDto>>;

/// <summary>Validates <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Initializes a new instance of the <see cref="LoginCommandValidator"/> class.</summary>
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(EmailAddress.MaxLength);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(256);
    }
}

/// <summary>Handles <see cref="LoginCommand"/>.</summary>
/// <remarks>
/// Every failure path returns the same error code and message. Distinguishing "no such account" from
/// "wrong password" would turn the sign-in endpoint into an account enumeration oracle, which is the
/// authentication failure listed in the OWASP Top 10. The lockout counter is still advanced, so
/// repeated guessing against a real account is throttled.
/// </remarks>
/// <param name="users">The user repository.</param>
/// <param name="passwordHasher">The password hasher.</param>
/// <param name="tokens">The token service.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="clock">The clock supplying the sign-in instant.</param>
internal sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<LoginCommand, Result<AuthenticationResultDto>>
{
    private static readonly Error InvalidCredentials = Error.Unauthorized(
        "auth.invalid_credentials",
        "The e-mail address or password is incorrect.");

    /// <inheritdoc />
    public async Task<Result<AuthenticationResultDto>> HandleAsync(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await users.GetByEmailAsync(normalizedEmail, cancellationToken);
        var now = clock.UtcNow;

        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthenticationResultDto>(InvalidCredentials);
        }

        if (user.IsLockedOut(now))
        {
            return Result.Failure<AuthenticationResultDto>(Error.Unauthorized(
                "auth.locked_out",
                "The account is temporarily locked after too many failed sign-in attempts."));
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            user.RecordFailedSignIn(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthenticationResultDto>(InvalidCredentials);
        }

        user.RecordSuccessfulSignIn(now);
        user.PruneRefreshTokens(now);

        var authorization = await users.GetAuthorizationDataAsync(user.Id, cancellationToken);
        var accessToken = tokens.CreateAccessToken(user, authorization.Roles, authorization.Permissions);
        var refreshToken = tokens.CreateRefreshToken();
        user.IssueRefreshToken(refreshToken.Hash, now, refreshToken.ExpiresAtUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(AuthenticationResultFactory.Create(
            user,
            authorization.Roles,
            authorization.Permissions,
            accessToken,
            refreshToken));
    }
}
