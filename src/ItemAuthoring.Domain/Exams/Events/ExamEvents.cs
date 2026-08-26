using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Domain.Exams.Events;

/// <summary>Raised when a new exam has been created.</summary>
/// <param name="ExamId">The identity of the new exam.</param>
/// <param name="OwnerId">The instructor who owns the exam.</param>
public sealed record ExamCreatedDomainEvent(ExamId ExamId, UserId OwnerId) : DomainEvent;

/// <summary>Raised when an exam has been frozen for delivery.</summary>
/// <param name="ExamId">The identity of the exam.</param>
/// <param name="PublishedAtUtc">The publication instant.</param>
public sealed record ExamPublishedDomainEvent(ExamId ExamId, DateTimeOffset PublishedAtUtc)
    : DomainEvent;
