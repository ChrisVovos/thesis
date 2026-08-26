using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Identity.Commands;
using ItemAuthoring.Application.Tests.TestDoubles;
using ItemAuthoring.Domain.Identity;
using NSubstitute;
using Shouldly;

namespace ItemAuthoring.Application.Tests.Identity;

public sealed class AdministrationCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly IPermissionRepository _permissions = Substitute.For<IPermissionRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedClock _clock = FixedClock.At(2026, 8, 24);

    public AdministrationCommandHandlerTests()
        => _passwordHasher.Hash(Arg.Any<string>()).Returns(PasswordHash.FromHash("hashed"));

    private static User ExistingUser() => User.Create(
        EmailAddress.Create("author@itemauthoring.local"),
        DisplayName.Create("Test Author"),
        PasswordHash.FromHash("hashed"));

    [Fact]
    public async Task A_duplicate_e_mail_address_is_refused()
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await new CreateUserCommandHandler(
                _users, _roles, _passwordHasher, _unitOfWork)
            .HandleAsync(
                new CreateUserCommand(
                    "author@itemauthoring.local",
                    "Test Author",
                    "Aa1!aaaaaaaaaa",
                    [Guid.CreateVersion7()]),
                default);

        result.Error.Code.ShouldBe("user.email_taken");
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public async Task An_unknown_role_prevents_account_creation()
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _roles.FindMissingAsync(Arg.Any<IReadOnlyCollection<RoleId>>(), Arg.Any<CancellationToken>())
            .Returns([RoleId.New()]);

        var result = await new CreateUserCommandHandler(
                _users, _roles, _passwordHasher, _unitOfWork)
            .HandleAsync(
                new CreateUserCommand(
                    "new@itemauthoring.local",
                    "New User",
                    "Aa1!aaaaaaaaaa",
                    [Guid.CreateVersion7()]),
                default);

        result.Error.Code.ShouldBe("role.not_found");
    }

    [Fact]
    public async Task A_valid_account_is_created_with_its_roles()
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _roles.FindMissingAsync(Arg.Any<IReadOnlyCollection<RoleId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await new CreateUserCommandHandler(
                _users, _roles, _passwordHasher, _unitOfWork)
            .HandleAsync(
                new CreateUserCommand(
                    "new@itemauthoring.local",
                    "New User",
                    "Aa1!aaaaaaaaaa",
                    [Guid.CreateVersion7()]),
                default);

        result.IsSuccess.ShouldBeTrue();
        _users.Received(1).Add(Arg.Is<User>(user => user.Roles.Count == 1));
    }

    [Fact]
    public async Task An_administrator_cannot_deactivate_their_own_account()
    {
        var administrator = FakeCurrentUser.Administrator();

        var result = await new SetUserActiveCommandHandler(
                _users, _unitOfWork, administrator, _clock)
            .HandleAsync(
                new SetUserActiveCommand(administrator.UserId!.Value.Value, false),
                default);

        result.Error.Code.ShouldBe("user.cannot_deactivate_self");
        await _users.DidNotReceive().GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivating_a_user_revokes_their_sessions()
    {
        var user = ExistingUser();
        user.IssueRefreshToken("token", _clock.UtcNow, _clock.UtcNow.AddDays(7));
        _users.GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await new SetUserActiveCommandHandler(
                _users, _unitOfWork, FakeCurrentUser.Administrator(), _clock)
            .HandleAsync(new SetUserActiveCommand(user.Id.Value, false), default);

        result.IsSuccess.ShouldBeTrue();
        user.IsActive.ShouldBeFalse();
        user.RefreshTokens.ShouldAllBe(token => !token.IsActive(_clock.UtcNow));
    }

    [Fact]
    public async Task Resetting_a_password_replaces_the_hash_and_ends_the_sessions()
    {
        var user = ExistingUser();
        user.IssueRefreshToken("token", _clock.UtcNow, _clock.UtcNow.AddDays(7));
        _users.GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Hash("Aa1!newpassword").Returns(PasswordHash.FromHash("new-hash"));

        var result = await new ResetUserPasswordCommandHandler(
                _users, _passwordHasher, _unitOfWork, _clock)
            .HandleAsync(new ResetUserPasswordCommand(user.Id.Value, "Aa1!newpassword"), default);

        result.IsSuccess.ShouldBeTrue();
        user.PasswordHash.Value.ShouldBe("new-hash");
        user.RefreshTokens.ShouldAllBe(token => !token.IsActive(_clock.UtcNow));
    }

    [Fact]
    public async Task An_unknown_permission_name_is_reported_as_a_validation_failure()
    {
        _roles.NameExistsAsync(Arg.Any<string>(), Arg.Any<RoleId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _permissions.GetByNamesAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await new CreateRoleCommandHandler(_roles, _permissions, _unitOfWork)
            .HandleAsync(
                new CreateRoleCommand("Moderator", "Moderates content.", ["items.nonsense"]),
                default);

        result.Error.Code.ShouldBe("permission.unknown");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task A_role_is_created_with_the_permissions_it_names()
    {
        var permission = Permission.Create(Permissions.ItemsRead, "Read items.");
        _roles.NameExistsAsync(Arg.Any<string>(), Arg.Any<RoleId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _permissions.GetByNamesAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([permission]);

        var result = await new CreateRoleCommandHandler(_roles, _permissions, _unitOfWork)
            .HandleAsync(
                new CreateRoleCommand("Moderator", "Moderates content.", [Permissions.ItemsRead]),
                default);

        result.IsSuccess.ShouldBeTrue();
        _roles.Received(1).Add(Arg.Is<Role>(role => role.Permissions.Count == 1));
    }

    [Fact]
    public async Task A_role_that_ships_with_the_platform_cannot_be_deleted()
    {
        var role = Role.Create(RoleNames.Administrator, "Full control.", isSystemRole: true);
        _roles.GetAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(role);

        var result = await new DeleteRoleCommandHandler(_roles, _unitOfWork)
            .HandleAsync(new DeleteRoleCommand(role.Id.Value), default);

        result.Error.Code.ShouldBe("role.system_immutable");
    }

    [Fact]
    public async Task A_role_that_users_still_hold_cannot_be_deleted()
    {
        var role = Role.Create("Moderator", "Moderates content.");
        _roles.GetAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(role);
        _roles.IsAssignedAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await new DeleteRoleCommandHandler(_roles, _unitOfWork)
            .HandleAsync(new DeleteRoleCommand(role.Id.Value), default);

        result.Error.Code.ShouldBe("role.in_use");
    }

    [Fact]
    public async Task Signing_out_with_an_unknown_token_still_succeeds()
    {
        var tokens = Substitute.For<ITokenService>();
        tokens.HashRefreshToken(Arg.Any<string>()).Returns("hash");
        _users.GetByRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await new LogoutCommandHandler(_users, tokens, _unitOfWork, _clock)
            .HandleAsync(new LogoutCommand("whatever"), default);

        result.IsSuccess.ShouldBeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
