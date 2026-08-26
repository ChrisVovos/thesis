namespace ItemAuthoring.Domain.Exams;

/// <summary>
/// The lifecycle of an assembled examination.
/// </summary>
public enum ExamStatus
{
    /// <summary>The exam is being assembled and its composition may change.</summary>
    Draft = 1,

    /// <summary>The exam is frozen and may be delivered.</summary>
    Published = 2,

    /// <summary>The exam is withdrawn from delivery but retained for audit.</summary>
    Archived = 3,
}
