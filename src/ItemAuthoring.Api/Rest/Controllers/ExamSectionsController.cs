using Asp.Versioning;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Exams.Commands;
using Microsoft.AspNetCore.Mvc;

namespace ItemAuthoring.Api.Rest.Controllers;

/// <summary>
/// The composition of an exam: its sections and the items placed in them.
/// </summary>
/// <remarks>
/// The routes are nested under the exam because a section has no identity outside the exam that owns
/// it, which mirrors the aggregate boundary in the domain.
/// </remarks>
/// <param name="sender">The request dispatcher.</param>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/exams/{examId:guid}/sections")]
public sealed class ExamSectionsController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Appends a section to a draft exam.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="command">The section to add.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new section.</returns>
    [HttpPost(Name = nameof(AddSection))]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddSection(
        Guid examId,
        [FromBody] AddExamSectionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var result = await Sender.SendAsync(command with { ExamId = examId }, cancellationToken);
        return RespondCreated(
            result,
            nameof(ExamsController.GetExam),
            new { id = examId });
    }

    /// <summary>Replaces the editorial details of a section.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="sectionId">The identity of the section.</param>
    /// <param name="command">The new details.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{sectionId:guid}", Name = nameof(UpdateSection))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateSection(
        Guid examId,
        Guid sectionId,
        [FromBody] UpdateExamSectionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Respond(await Sender.SendAsync(
            command with { ExamId = examId, SectionId = sectionId },
            cancellationToken));
    }

    /// <summary>Removes a section together with all of its placements.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="sectionId">The identity of the section.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{sectionId:guid}", Name = nameof(RemoveSection))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveSection(
        Guid examId,
        Guid sectionId,
        CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(
            new RemoveExamSectionCommand(examId, sectionId),
            cancellationToken));

    /// <summary>Reorders the sections of an exam.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="orderedSectionIds">Every section of the exam, in the desired order.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("order", Name = nameof(ReorderSections))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderSections(
        Guid examId,
        [FromBody] IReadOnlyList<Guid> orderedSectionIds,
        CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(
            new ReorderExamSectionsCommand(examId, orderedSectionIds),
            cancellationToken));

    /// <summary>Places an existing bank item into a section.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="sectionId">The identity of the section.</param>
    /// <param name="command">The placement to add.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new placement.</returns>
    [HttpPost("{sectionId:guid}/items", Name = nameof(AddItem))]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddItem(
        Guid examId,
        Guid sectionId,
        [FromBody] AddExamItemCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var result = await Sender.SendAsync(
            command with { ExamId = examId, SectionId = sectionId },
            cancellationToken);
        return RespondCreated(result, nameof(ExamsController.GetExam), new { id = examId });
    }

    /// <summary>Removes a placement from a section.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="sectionId">The identity of the section.</param>
    /// <param name="examItemId">The identity of the placement.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{sectionId:guid}/items/{examItemId:guid}", Name = nameof(RemoveItem))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveItem(
        Guid examId,
        Guid sectionId,
        Guid examItemId,
        CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(
            new RemoveExamItemCommand(examId, sectionId, examItemId),
            cancellationToken));

    /// <summary>Reorders the placements inside a section.</summary>
    /// <param name="examId">The identity of the exam.</param>
    /// <param name="sectionId">The identity of the section.</param>
    /// <param name="orderedExamItemIds">Every placement of the section, in the desired order.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{sectionId:guid}/items/order", Name = nameof(ReorderItems))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderItems(
        Guid examId,
        Guid sectionId,
        [FromBody] IReadOnlyList<Guid> orderedExamItemIds,
        CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(
            new ReorderExamItemsCommand(examId, sectionId, orderedExamItemIds),
            cancellationToken));
}
