using FluentValidation;

namespace ItemAuthoring.Application.Identity;

/// <summary>
/// The password policy enforced everywhere a password is accepted.
/// </summary>
/// <remarks>
/// Written once as a reusable rule set rather than repeated in each validator, so that strengthening
/// the policy is a single edit and cannot leave one entry point behind.
/// </remarks>
public static class PasswordPolicy
{
    /// <summary>The inclusive minimum number of characters a password must contain.</summary>
    public const int MinimumLength = 12;

    /// <summary>The inclusive maximum number of characters a password may contain.</summary>
    public const int MaximumLength = 256;

    /// <summary>Applies the password policy to a string rule.</summary>
    /// <typeparam name="T">The command type being validated.</typeparam>
    /// <param name="rule">The rule builder for the password property.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(
        this IRuleBuilder<T, string> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return rule
            .NotEmpty()
            .MinimumLength(MinimumLength)
            .MaximumLength(MaximumLength)
            .Must(password => password.Any(char.IsUpper))
            .WithMessage("The password must contain an upper case letter.")
            .Must(password => password.Any(char.IsLower))
            .WithMessage("The password must contain a lower case letter.")
            .Must(password => password.Any(char.IsDigit))
            .WithMessage("The password must contain a digit.")
            .Must(password => password.Any(character => !char.IsLetterOrDigit(character)))
            .WithMessage("The password must contain a symbol.");
    }
}
