using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Exams.Dtos;
using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Exams.Queries;

/// <summary>Searches, filters, sorts and pages the exam list.</summary>
public sealed record ExamSearchCriteria : PagedQuery
{
    /// <summary>Gets the lifecycle statuses to include; all statuses when empty.</summary>
    public IReadOnlyList<ExamStatus>? Statuses { get; init; }

    /// <summary>Gets the owning instructor to restrict the search to.</summary>
    public Guid? OwnerId { get; init; }
}

/// <summary>Returns one page of the exam list.</summary>
/// <param name="Criteria">The search, filter, sort and paging criteria.</param>
[RequiresPermission(Permissions.ExamsRead)]
public sealed record SearchExamsQuery(ExamSearchCriteria Criteria)
    : IQuery<Result<PagedResult<ExamSummaryDto>>>;

/// <summary>Returns the full projection of a single exam.</summary>
/// <param name="ExamId">The exam to load.</param>
[RequiresPermission(Permissions.ExamsRead)]
public sealed record GetExamByIdQuery(Guid ExamId) : IQuery<Result<ExamDetailDto>>;

/// <summary>Handles <see cref="SearchExamsQuery"/>.</summary>
/// <param name="readStore">The read side of the exam builder.</param>
/// <param name="executor">The asynchronous query executor.</param>
internal sealed class SearchExamsQueryHandler(IExamReadStore readStore, IAsyncQueryExecutor executor)
    : IRequestHandler<SearchExamsQuery, Result<PagedResult<ExamSummaryDto>>>
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<ExamSummaryDto>>> HandleAsync(
        SearchExamsQuery request,
        CancellationToken cancellationToken)
    {
        var criteria = request.Criteria;
        var query = readStore.QuerySummaries();

        if (criteria.Statuses is { Count: > 0 } statuses)
        {
            query = query.Where(exam => statuses.Contains(exam.Status));
        }

        if (criteria.OwnerId is { } ownerId)
        {
            query = query.Where(exam => exam.OwnerId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim();
            query = query.Where(exam => exam.Title.Contains(term));
        }

        var totalCount = await executor.CountAsync(query, cancellationToken);
        if (totalCount == 0)
        {
            return Result.Success(PagedResult<ExamSummaryDto>.Empty(criteria.Page, criteria.PageSize));
        }

        var ordered = criteria.SortBy?.Trim().ToLowerInvariant() switch
        {
            "title" => criteria.SortDescending
                ? query.OrderByDescending(exam => exam.Title).ThenBy(exam => exam.Id)
                : query.OrderBy(exam => exam.Title).ThenBy(exam => exam.Id),
            "status" => criteria.SortDescending
                ? query.OrderByDescending(exam => exam.Status).ThenBy(exam => exam.Id)
                : query.OrderBy(exam => exam.Status).ThenBy(exam => exam.Id),
            _ => query.OrderByDescending(exam => exam.CreatedAtUtc).ThenBy(exam => exam.Id),
        };

        var page = await executor.ToListAsync(
            ordered.Skip(criteria.Skip).Take(criteria.PageSize),
            cancellationToken);

        return Result.Success(
            new PagedResult<ExamSummaryDto>(page, totalCount, criteria.Page, criteria.PageSize));
    }
}

/// <summary>Handles <see cref="GetExamByIdQuery"/>.</summary>
/// <param name="readStore">The read side of the exam builder.</param>
internal sealed class GetExamByIdQueryHandler(IExamReadStore readStore)
    : IRequestHandler<GetExamByIdQuery, Result<ExamDetailDto>>
{
    /// <inheritdoc />
    public async Task<Result<ExamDetailDto>> HandleAsync(
        GetExamByIdQuery request,
        CancellationToken cancellationToken)
    {
        var exam = await readStore.GetDetailAsync(request.ExamId, cancellationToken);
        return exam is null
            ? Result.Failure<ExamDetailDto>(Error.NotFound("exam.not_found", "The exam does not exist."))
            : Result.Success(exam);
    }
}
