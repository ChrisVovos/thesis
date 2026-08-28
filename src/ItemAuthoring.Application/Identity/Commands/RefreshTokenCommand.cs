using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Identity.Dtos;

namespace ItemAuthoring.Application.Identity.Commands;

/// <summary>Exchanges a valid refresh token for a new token pair.</summary>
/// <param name="RefreshToken">The opaque refresh token held by the client.</param>
[AllowAnonymousRequest]
public sealed record RefreshTokenCommand(string RefreshToken)
    : ICommand<Result<AuthenticationResultDto>>;

/// <summary>Revokes a refresh token, ending the session it belongs to.</summary>
/// <param name="RefreshToken">The opaque refresh token held by the client.</param>
[AllowAnonymousRequest]
public sealed record LogoutCommand(string RefreshToken) : ICommand<Result>;

/// <summary>Validates <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>Initializes a new instance of the <see cref="RefreshTokenCommandValidator"/> class.</summary>
    public RefreshTokenCommandValidator()
        => RuleFor(command => command.RefreshToken).NotEmpty().MaximumLength(512);
}

/// <summary>Validates <see cref="LogoutCommand"/>.</summary>
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    /// <summary>Initializes a new instance of the <see cref="LogoutCommandValidator"/> class.</summary>
    public LogoutCommandValidator()
        => RuleFor(command => command.RefreshToken).NotEmpty().MaximumLength(512);
}

/// <summary>Handles <see cref="RefreshTokenCommand"/>.</summary>
/// <remarks>
/// Refresh tokens are rotated on every use and the old token is marked as replaced. Presenting a
/// token that has already been rotated is treated as theft by the aggregate, which revokes the entire
/// token family — the standard mitigation for refresh token replay.
/// </remarks>
/// <param name="users">The user repository.</param>
/// <param name="tokens">The token service.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="clock">The clock supplying the rotation instant.</param>
internal sealed class RefreshTokenCommandHandler(
    IUserRepository users,
    ITokenService tokens,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<RefreshTokenCommand, Result<AuthenticationResultDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthenticationResultDto>> HandleAsync(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var presentedHash = tokens.HashRefreshToken(request.RefreshToken);
        var user = await users.GetByRefreshTokenAsync(presentedHash, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthenticationResultDto>(Error.Unauthorized(
                "auth.refresh_token_invalid",
                "The refresh token is not valid."));
        }

        return await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var now = clock.UtcNow;
                var replacement = tokens.CreateRefreshToken();
                user.RotateRefreshToken(presentedHash, replacement.Hash, now, replacement.ExpiresAtUtc);
                user.PruneRefreshTokens(now);

                var authorization = await users.GetAuthorizationDataAsync(user.Id, token);
                var accessToken = tokens.CreateAccessToken(
                    user,
                    authorization.Roles,
                    authorization.Permissions);

                await unitOfWork.SaveChangesAsync(token);

                return Result.Success(AuthenticationResultFactory.Create(
                    user,
                    authorization.Roles,
                    authorization.Permissions,
                    accessToken,
                    replacement));
            },
            cancellationToken);
    }
}

/// <summary>Handles <see cref="LogoutCommand"/>.</summary>
/// <param name="users">The user repository.</param>
/// <param name="tokens">The token service.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="clock">The clock supplying the revocation instant.</param>
internal sealed class LogoutCommandHandler(
    IUserRepository users,
    ITokenService tokens,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<LogoutCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(LogoutCommand request, CancellationToken cancellationToken)
    {
        var presentedHash = tokens.HashRefreshToken(request.RefreshToken);
        var user = await users.GetByRefreshTokenAsync(presentedHash, cancellationToken);

        if (user is not null && user.RevokeRefreshToken(presentedHash, clock.UtcNow))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
