using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Exams;

/// <summary>
/// The ownership rule that supplements permission checks for exam editing.
/// </summary>
/// <remarks>
/// Returning a <see cref="Result"/> rather than a boolean lets each handler forward the failure
/// unchanged, so "exam does not exist" and "exam belongs to someone else" carry the same codes for
/// every use case and therefore for both API surfaces.
/// </remarks>
public static class ExamOwnershipPolicy
{
    /// <summary>Verifies that the exam exists and that the caller may change it.</summary>
    /// <param name="exam">The loaded exam, or <see langword="null"/> when it was not found.</param>
    /// <param name="currentUser">The principal on whose behalf the request executes.</param>
    /// <returns>Success when the caller may change the exam.</returns>
    public static Result Authorize(Exam? exam, ICurrentUser currentUser)
    {
        if (exam is null)
        {
            return Result.Failure(Error.NotFound("exam.not_found", "The exam does not exist."));
        }

        var isOwner = currentUser.UserId is { } userId && exam.OwnerId == userId;
        return isOwner || currentUser.IsInRole(RoleNames.Administrator)
            ? Result.Success()
            : Result.Failure(Error.Forbidden(
                "exam.not_owner",
                "Only the owner of an exam, or an administrator, may change it."));
    }
}
