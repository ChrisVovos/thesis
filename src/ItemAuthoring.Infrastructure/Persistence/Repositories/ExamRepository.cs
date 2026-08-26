using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Domain.Exams;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence.Repositories;

/// <summary>The Entity Framework Core implementation of <see cref="IExamRepository"/>.</summary>
/// <param name="context">The Entity Framework Core session.</param>
internal sealed class ExamRepository(ApplicationDbContext context) : IExamRepository
{
    /// <inheritdoc />
    public Task<Exam?> GetAsync(ExamId examId, CancellationToken cancellationToken = default)
        => context.Exams
            .Include(exam => exam.Sections)
            .ThenInclude(section => section.Items)
            .FirstOrDefaultAsync(exam => exam.Id == examId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(ExamId examId, CancellationToken cancellationToken = default)
        => context.Exams.AnyAsync(exam => exam.Id == examId, cancellationToken);

    /// <inheritdoc />
    public void Add(Exam exam) => context.Exams.Add(exam);
}
