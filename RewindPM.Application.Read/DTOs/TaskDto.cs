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
    public DateTime? ScheduledStartDate { get; init; }
    public DateTime? ScheduledEndDate { get; init; }
    public int? EstimatedHours { get; init; }

    // 実績期間
    public DateTime? ActualStartDate { get; init; }
    public DateTime? ActualEndDate { get; init; }
    public int? ActualHours { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? UpdatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
