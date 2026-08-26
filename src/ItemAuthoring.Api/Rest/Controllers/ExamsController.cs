using Asp.Versioning;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Exams.Commands;
using ItemAuthoring.Application.Exams.Dtos;
using ItemAuthoring.Application.Exams.Queries;
using ItemAuthoring.Domain.Exams;
using Microsoft.AspNetCore.Mvc;

namespace ItemAuthoring.Api.Rest.Controllers;

/// <summary>
/// The exam builder.
/// </summary>
/// <param name="sender">The request dispatcher.</param>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/exams")]
public sealed class ExamsController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Searches, sorts and pages the exam list.</summary>
    /// <param name="page">The one based page index.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="search">A free-text search term.</param>
    /// <param name="sortBy">The property to sort by.</param>
    /// <param name="sortDescending">Whether the sort is descending.</param>
    /// <param name="status">The lifecycle statuses to include.</param>
    /// <param name="ownerId">The owning instructor to restrict the search to.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>One page of exam summaries together with paging metadata.</returns>
    [HttpGet(Name = nameof(SearchExams))]
    [ProducesResponseType(typeof(PagedResult<ExamSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchExams(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        [FromQuery] ExamStatus[]? status = null,
        [FromQuery] Guid? ownerId = null)
    {
        var criteria = new ExamSearchCriteria
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Statuses = status,
            OwnerId = ownerId,
        };
        return Respond(await Sender.SendAsync(new SearchExamsQuery(criteria), cancellationToken));
    }

    /// <summary>Reads a single exam together with its full composition.</summary>
    /// <param name="id">The identity of the exam.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The full projection of the exam.</returns>
    [HttpGet("{id:guid}", Name = nameof(GetExam))]
    [ProducesResponseType(typeof(ExamDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExam(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetExamByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return Problem(result.Error);
        }

        var summary = result.Value.Summary;
        var versionToken =
            $"{summary.Id}:{summary.Status}:{summary.ItemCount}:{summary.TotalScore}:{summary.PublishedAtUtc:O}";
        return RespondWithEntityTag(result, versionToken, TimeSpan.FromSeconds(15));
    }

    /// <summary>Creates a draft exam.</summary>
    /// <param name="command">The exam to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new exam.</returns>
    [HttpPost(Name = nameof(CreateExam))]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateExam(
        [FromBody] CreateExamCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(command, cancellationToken);
        return RespondCreated(result, nameof(GetExam), new { id = result.IsSuccess ? result.Value : Guid.Empty });
    }

    /// <summary>Replaces the editorial details of a draft exam.</summary>
    /// <param name="id">The identity of the exam.</param>
    /// <param name="command">The new details.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id:guid}", Name = nameof(UpdateExam))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateExam(
        Guid id,
        [FromBody] UpdateExamCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Respond(await Sender.SendAsync(command with { ExamId = id }, cancellationToken));
    }

    /// <summary>Logically removes an exam.</summary>
    /// <param name="id">The identity of the exam.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}", Name = nameof(DeleteExam))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteExam(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new DeleteExamCommand(id), cancellationToken));

    /// <summary>Freezes a draft exam for delivery.</summary>
    /// <param name="id">The identity of the exam.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/publish", Name = nameof(PublishExam))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishExam(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new PublishExamCommand(id), cancellationToken));

    /// <summary>Withdraws a published exam from delivery.</summary>
    /// <param name="id">The identity of the exam.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/archive", Name = nameof(ArchiveExam))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ArchiveExam(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new ArchiveExamCommand(id), cancellationToken));

    /// <summary>Returns a published or archived exam to draft.</summary>
    /// <param name="id">The identity of the exam.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/return-to-draft", Name = nameof(ReturnExamToDraft))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReturnExamToDraft(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new ReturnExamToDraftCommand(id), cancellationToken));
}
