using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Identity.Commands;
using ItemAuthoring.Application.Tests.TestDoubles;
using ItemAuthoring.Domain.Identity;
using NSubstitute;
using Shouldly;

namespace ItemAuthoring.Application.Tests.Identity;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedClock _clock = FixedClock.At(2026, 8, 24);

    private LoginCommandHandler CreateHandler()
        => new(_users, _passwordHasher, _tokens, _unitOfWork, _clock);

    private static User ActiveUser() => User.Create(
        EmailAddress.Create("author@itemauthoring.local"),
        DisplayName.Create("Test Author"),
        PasswordHash.FromHash("stored-hash"));

    private void ArrangeTokenIssuance()
    {
        _tokens.CreateAccessToken(
                Arg.Any<User>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new AccessToken("access", _clock.UtcNow.AddMinutes(15)));
        _tokens.CreateRefreshToken()
            .Returns(new RefreshTokenMaterial("refresh", "refresh-hash", _clock.UtcNow.AddDays(7)));
        _users.GetAuthorizationDataAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((new[] { RoleNames.Author }, new[] { Permissions.ItemsCreate }));
    }

    [Fact]
    public async Task Valid_credentials_issue_a_token_pair()
    {
        var user = ActiveUser();
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<PasswordHash>(), Arg.Any<string>()).Returns(true);
        ArrangeTokenIssuance();

        var result = await CreateHandler().HandleAsync(
            new LoginCommand("author@itemauthoring.local", "correct-password"),
            default);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access");
        result.Value.RefreshToken.ShouldBe("refresh");
        result.Value.User.Roles.ShouldContain(RoleNames.Author);
        user.RefreshTokens.ShouldHaveSingleItem().TokenHash.ShouldBe("refresh-hash");
    }

    [Fact]
    public async Task An_unknown_account_and_a_wrong_password_are_indistinguishable()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var unknown = await CreateHandler().HandleAsync(
            new LoginCommand("nobody@itemauthoring.local", "whatever"),
            default);

        var user = ActiveUser();
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<PasswordHash>(), Arg.Any<string>()).Returns(false);

        var wrongPassword = await CreateHandler().HandleAsync(
            new LoginCommand("author@itemauthoring.local", "wrong"),
            default);

        unknown.Error.ShouldBe(wrongPassword.Error);
        unknown.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task A_failed_attempt_advances_the_lockout_counter()
    {
        var user = ActiveUser();
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<PasswordHash>(), Arg.Any<string>()).Returns(false);

        await CreateHandler().HandleAsync(
            new LoginCommand("author@itemauthoring.local", "wrong"),
            default);

        user.FailedSignInAttempts.ShouldBe(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_locked_out_account_is_told_so_explicitly()
    {
        var user = ActiveUser();
        for (var attempt = 0; attempt < User.MaxFailedSignInAttempts; attempt++)
        {
            user.RecordFailedSignIn(_clock.UtcNow);
        }

        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().HandleAsync(
            new LoginCommand("author@itemauthoring.local", "correct-password"),
            default);

        result.Error.Code.ShouldBe("auth.locked_out");
    }

    [Fact]
    public async Task A_deactivated_account_cannot_sign_in()
    {
        var user = ActiveUser();
        user.Deactivate(_clock.UtcNow);
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<PasswordHash>(), Arg.Any<string>()).Returns(true);

        var result = await CreateHandler().HandleAsync(
            new LoginCommand("author@itemauthoring.local", "correct-password"),
            default);

        result.Error.Code.ShouldBe("auth.invalid_credentials");
    }

    [Fact]
    public async Task The_lookup_is_performed_with_the_normalized_address()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await CreateHandler().HandleAsync(
            new LoginCommand("  Author@ItemAuthoring.Local ", "whatever"),
            default);

        await _users.Received(1).GetByEmailAsync(
            "AUTHOR@ITEMAUTHORING.LOCAL",
            Arg.Any<CancellationToken>());
    }
}
