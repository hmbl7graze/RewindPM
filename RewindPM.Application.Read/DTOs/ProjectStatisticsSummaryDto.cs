namespace RewindPM.Application.Read.DTOs;

/// <summary>
/// プロジェクトカード用の統計情報
/// </summary>
public record ProjectStatisticsSummaryDto
{
    public required Guid ProjectId { get; init; }
    public required int TotalTasks { get; init; }
    public required int CompletedTasks { get; init; }
    public required int InProgressTasks { get; init; }
    public required int InReviewTasks { get; init; }
    public required int TodoTasks { get; init; }

    /// <summary>
    /// 完了率（0-100）
    /// </summary>
    public double CompletionRate => TotalTasks > 0
        ? Math.Round((double)CompletedTasks / TotalTasks * 100, 1)
        : 0;
}
