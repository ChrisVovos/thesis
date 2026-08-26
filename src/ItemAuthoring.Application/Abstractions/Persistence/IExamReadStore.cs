using ItemAuthoring.Application.Exams.Dtos;
using ItemAuthoring.Application.Identity.Dtos;

namespace ItemAuthoring.Application.Abstractions.Persistence;

/// <summary>
/// The read side of the exam builder.
/// </summary>
public interface IExamReadStore
{
    /// <summary>Opens a composable query over the exams, excluding deleted ones.</summary>
    /// <returns>The composable query.</returns>
    IQueryable<ExamSummaryDto> QuerySummaries();

    /// <summary>Loads the full projection of a single exam, including its item placements.</summary>
    /// <param name="examId">The exam to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The exam, or <see langword="null"/> when it does not exist.</returns>
    Task<ExamDetailDto?> GetDetailAsync(Guid examId, CancellationToken cancellationToken = default);

    /// <summary>Loads the sections of several exams in one round trip.</summary>
    /// <param name="examIds">The exams to load sections for.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The sections, keyed by exam identity.</returns>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ExamSectionDto>>> GetSectionsAsync(
        IReadOnlyList<Guid> examIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The read side of the user directory.
/// </summary>
public interface IIdentityReadStore
{
    /// <summary>Opens a composable query over the users.</summary>
    /// <returns>The composable query.</returns>
    IQueryable<UserDto> QueryUsers();

    /// <summary>Opens a composable query over the roles.</summary>
    /// <returns>The composable query.</returns>
    IQueryable<RoleDto> QueryRoles();

    /// <summary>Loads a single user.</summary>
    /// <param name="userId">The user to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The user, or <see langword="null"/> when they do not exist.</returns>
    Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Loads the permission catalogue.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>Every permission known to the application.</returns>
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads the roles assigned to several users in one round trip.</summary>
    /// <param name="userIds">The users to load roles for.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The roles, keyed by user identity.</returns>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<RoleDto>>> GetRolesByUserAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default);
}
