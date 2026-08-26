using System.ComponentModel.DataAnnotations;

namespace ItemAuthoring.Infrastructure.Persistence;

/// <summary>
/// Controls what the database seeder does at start-up.
/// </summary>
/// <remarks>
/// The administrator password is required rather than defaulted. A well known default credential is
/// one of the most common ways a demonstration system becomes a production incident, so the process
/// refuses to start with seeding enabled and no password supplied.
/// </remarks>
public sealed class SeedOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Seed";

    /// <summary>Gets or sets a value indicating whether reference data is seeded at start-up.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether pending migrations are applied at start-up.</summary>
    public bool ApplyMigrations { get; set; }

    /// <summary>Gets or sets the login identifier of the bootstrap administrator.</summary>
    [Required]
    [EmailAddress]
    public string AdministratorEmail { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the bootstrap administrator.</summary>
    public string AdministratorDisplayName { get; set; } = "Platform Administrator";

    /// <summary>Gets or sets the initial password of the bootstrap administrator.</summary>
    public string AdministratorPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether a sample item bank is generated.</summary>
    public bool IncludeSampleContent { get; set; }

    /// <summary>Gets or sets the number of sample items generated when sample content is enabled.</summary>
    [Range(0, 10_000)]
    public int SampleItemCount { get; set; } = 250;
}
