using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Application.Read.DTOs;

/// <summary>
/// タスクの読み取りモデル
/// </summary>
public record TaskDto
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required TaskStatus Status { get; init; }

    // 予定期間
    public DateTimeOffset? ScheduledStartDate { get; init; }
    public DateTimeOffset? ScheduledEndDate { get; init; }
    public int? EstimatedHours { get; init; }

    // 実績期間
    public DateTimeOffset? ActualStartDate { get; init; }
    public DateTimeOffset? ActualEndDate { get; init; }
    public int? ActualHours { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? UpdatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
