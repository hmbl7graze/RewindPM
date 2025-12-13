namespace RewindPM.Application.Read.DTOs;

/// <summary>
/// プロジェクト統計の時系列データ
/// </summary>
public record ProjectStatisticsTimeSeriesDto
{
    public required Guid ProjectId { get; init; }
    public required List<DailyStatisticsSnapshot> DailySnapshots { get; init; }
}

/// <summary>
/// 日次統計スナップショット
/// </summary>
public record DailyStatisticsSnapshot
{
    public required DateTimeOffset Date { get; init; }
    public required int TotalTasks { get; init; }
    public required int CompletedTasks { get; init; }
    public required int InProgressTasks { get; init; }
    public required int InReviewTasks { get; init; }
    public required int TodoTasks { get; init; }

    /// <summary>
    /// 残タスク数（バーンダウンチャート用）
    /// </summary>
    public int RemainingTasks => TotalTasks - CompletedTasks;
}
