using HotChocolate;
using HotChocolate.Types;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Exams.Commands;

namespace ItemAuthoring.Api.GraphQL;

/// <summary>
/// The exam builder mutations, merged into the root mutation type.
/// </summary>
/// <remarks>
/// The mutation surface is split across several classes purely for readability; the schema still
/// presents one flat <c>Mutation</c> type, exactly as the REST surface presents one flat route space.
/// </remarks>
[ExtendObjectType<Mutation>]
public sealed class ExamMutation
{
    /// <summary>Creates a draft exam.</summary>
    /// <param name="input">The exam to create.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new exam.</returns>
    public async Task<Guid> CreateExam(
        CreateExamCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Replaces the editorial details of a draft exam.</summary>
    /// <param name="input">The new details.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> UpdateExam(
        UpdateExamCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Logically removes an exam.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> DeleteExam(
        Guid examId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new DeleteExamCommand(examId), cancellationToken)).UnwrapOrThrow();

    /// <summary>Appends a section to a draft exam.</summary>
    /// <param name="input">The section to add.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new section.</returns>
    public async Task<Guid> AddExamSection(
        AddExamSectionCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Replaces the editorial details of a section.</summary>
    /// <param name="input">The new details.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> UpdateExamSection(
        UpdateExamSectionCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Removes a section together with all of its placements.</summary>
    /// <param name="input">The section to remove.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> RemoveExamSection(
        RemoveExamSectionCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Reorders the sections of an exam.</summary>
    /// <param name="input">The desired order.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> ReorderExamSections(
        ReorderExamSectionsCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Places an existing bank item into a section.</summary>
    /// <param name="input">The placement to add.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new placement.</returns>
    public async Task<Guid> AddExamItem(
        AddExamItemCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Removes a placement from a section.</summary>
    /// <param name="input">The placement to remove.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> RemoveExamItem(
        RemoveExamItemCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Reorders the placements inside a section.</summary>
    /// <param name="input">The desired order.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> ReorderExamItems(
        ReorderExamItemsCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Freezes a draft exam for delivery.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> PublishExam(
        Guid examId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new PublishExamCommand(examId), cancellationToken)).UnwrapOrThrow();

    /// <summary>Withdraws a published exam from delivery.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> ArchiveExam(
        Guid examId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new ArchiveExamCommand(examId), cancellationToken)).UnwrapOrThrow();

    /// <summary>Returns a published or archived exam to draft.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> ReturnExamToDraft(
        Guid examId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new ReturnExamToDraftCommand(examId), cancellationToken))
            .UnwrapOrThrow();
}
