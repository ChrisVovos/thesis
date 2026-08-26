using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;
using Shouldly;

namespace ItemAuthoring.Domain.Tests.Identity;

public sealed class UserAuthenticationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    private static User NewUser() => User.Create(
        EmailAddress.Create("author@itemauthoring.local"),
        DisplayName.Create("Test Author"),
        PasswordHash.FromHash("hashed"));

    [Fact]
    public void A_new_user_is_active_and_not_locked_out()
    {
        var user = NewUser();

        user.IsActive.ShouldBeTrue();
        user.IsLockedOut(Now).ShouldBeFalse();
    }

    [Fact]
    public void Repeated_failures_lock_the_account_for_a_bounded_period()
    {
        var user = NewUser();

        for (var attempt = 0; attempt < User.MaxFailedSignInAttempts; attempt++)
        {
            user.RecordFailedSignIn(Now);
        }

        user.IsLockedOut(Now).ShouldBeTrue();
        user.IsLockedOut(Now.AddMinutes(User.LockoutMinutes + 1)).ShouldBeFalse();
    }

    [Fact]
    public void A_successful_sign_in_clears_the_failure_counter()
    {
        var user = NewUser();
        user.RecordFailedSignIn(Now);

        user.RecordSuccessfulSignIn(Now);

        user.FailedSignInAttempts.ShouldBe(0);
        user.LastSignInAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void Rotating_a_refresh_token_revokes_the_presented_one()
    {
        var user = NewUser();
        user.IssueRefreshToken("first", Now, Now.AddDays(7));

        var replacement = user.RotateRefreshToken("first", "second", Now, Now.AddDays(7));

        replacement.TokenHash.ShouldBe("second");
        user.RefreshTokens.Single(token => token.TokenHash == "first")
            .IsActive(Now).ShouldBeFalse();
        replacement.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void Reusing_an_already_rotated_token_revokes_the_whole_family()
    {
        var user = NewUser();
        user.IssueRefreshToken("first", Now, Now.AddDays(7));
        user.RotateRefreshToken("first", "second", Now, Now.AddDays(7));

        Should.Throw<DomainException>(() =>
                user.RotateRefreshToken("first", "third", Now, Now.AddDays(7)))
            .Code.ShouldBe("auth.refresh_token_reused");

        user.RefreshTokens.ShouldAllBe(token => !token.IsActive(Now));
    }

    [Fact]
    public void An_unknown_refresh_token_is_rejected_without_touching_the_others()
    {
        var user = NewUser();
        user.IssueRefreshToken("first", Now, Now.AddDays(7));

        Should.Throw<DomainException>(() =>
                user.RotateRefreshToken("unknown", "second", Now, Now.AddDays(7)))
            .Code.ShouldBe("auth.refresh_token_unknown");

        user.RefreshTokens.Single().IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void Changing_the_password_ends_every_open_session()
    {
        var user = NewUser();
        user.IssueRefreshToken("first", Now, Now.AddDays(7));

        user.ChangePassword(PasswordHash.FromHash("new-hash"), Now);

        user.RefreshTokens.ShouldAllBe(token => !token.IsActive(Now));
    }

    [Fact]
    public void Deactivating_a_user_ends_every_open_session()
    {
        var user = NewUser();
        user.IssueRefreshToken("first", Now, Now.AddDays(7));

        user.Deactivate(Now);

        user.IsActive.ShouldBeFalse();
        user.RefreshTokens.ShouldAllBe(token => !token.IsActive(Now));
    }

    [Fact]
    public void Expired_tokens_are_pruned()
    {
        var user = NewUser();
        user.IssueRefreshToken("expired", Now.AddDays(-9), Now.AddDays(-2));
        user.IssueRefreshToken("current", Now, Now.AddDays(7));

        user.PruneRefreshTokens(Now);

        user.RefreshTokens.ShouldHaveSingleItem().TokenHash.ShouldBe("current");
    }

    [Fact]
    public void A_user_must_keep_at_least_one_role()
    {
        var user = NewUser();

        Should.Throw<DomainException>(() => user.ReplaceRoles([]))
            .Code.ShouldBe("user.roles_required");
    }

    [Theory]
    [InlineData("not-an-address")]
    [InlineData("missing@domain")]
    [InlineData("@no-local.part")]
    public void A_malformed_address_is_rejected(string candidate)
        => Should.Throw<DomainException>(() => EmailAddress.Create(candidate))
            .Code.ShouldBe("user.email_invalid");

    [Fact]
    public void A_password_hash_never_reveals_itself_in_diagnostics()
        => PasswordHash.FromHash("super-secret-hash").ToString().ShouldBe("***");

    [Fact]
    public void A_system_role_cannot_be_renamed()
    {
        var role = Role.Create(RoleNames.Administrator, "Full control.", isSystemRole: true);

        Should.Throw<DomainException>(() => role.Rename("Something else"))
            .Code.ShouldBe("role.system_immutable");
    }

    [Fact]
    public void Granting_the_same_permission_twice_is_idempotent()
    {
        var role = Role.Create("Custom", "A custom role.");
        var permissionId = PermissionId.New();

        role.Grant(permissionId);
        role.Grant(permissionId);

        role.Permissions.ShouldHaveSingleItem();
    }
}
