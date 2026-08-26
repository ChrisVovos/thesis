using ItemAuthoring.Domain.Exams;

namespace ItemAuthoring.Application.Abstractions.Persistence;

/// <summary>
/// Loads and stores <see cref="Exam"/> aggregates.
/// </summary>
public interface IExamRepository
{
    /// <summary>Loads an exam together with its sections and item placements.</summary>
    /// <param name="examId">The exam to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The exam, or <see langword="null"/> when it does not exist or was deleted.</returns>
    Task<Exam?> GetAsync(ExamId examId, CancellationToken cancellationToken = default);

    /// <summary>Determines whether an exam exists and has not been deleted.</summary>
    /// <param name="examId">The exam to test for.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> when the exam exists.</returns>
    Task<bool> ExistsAsync(ExamId examId, CancellationToken cancellationToken = default);

    /// <summary>Registers a new exam for insertion.</summary>
    /// <param name="exam">The exam to add.</param>
    void Add(Exam exam);
}
