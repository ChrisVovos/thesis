using System.Text;
using ItemAuthoring.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ItemAuthoring.Api.Security;

/// <summary>
/// Registers JWT bearer authentication and the role based authorization policies.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>Registers authentication and authorization.</summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddScoped<Application.Abstractions.Security.ICurrentUser, CurrentUser>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    // A five second skew is enough to absorb clock drift between hosts without
                    // meaningfully extending the life of a token.
                    ClockSkew = TimeSpan.FromSeconds(5),
                    RoleClaimType = ApplicationClaimTypes.Role,
                    NameClaimType = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub,
                };
            });

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().SetFallbackPolicy(null);

        return services;
    }

    /// <summary>
    /// Applies the issuer, audience and signing key from <see cref="JwtOptions"/> to the bearer handler.
    /// </summary>
    /// <remarks>
    /// The values are applied here rather than inline so that a single validated options object is the
    /// only source of the signing key, for both issuing and validating tokens.
    /// </remarks>
    /// <param name="jwtOptions">The validated token settings.</param>
    private sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
        : IConfigureNamedOptions<JwtBearerOptions>
    {
        public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);

        public void Configure(string? name, JwtBearerOptions options)
        {
            if (name is not JwtBearerDefaults.AuthenticationScheme)
            {
                return;
            }

            var settings = jwtOptions.Value;
            options.TokenValidationParameters.ValidIssuer = settings.Issuer;
            options.TokenValidationParameters.ValidAudience = settings.Audience;
            options.TokenValidationParameters.IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        }
    }
}
