using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Repositories;

/// <summary>
/// プロジェクト統計情報のリポジトリインターフェース
/// </summary>
public interface IProjectStatisticsRepository
{
    /// <summary>
    /// プロジェクトカード用の統計情報を取得
    /// </summary>
    Task<ProjectStatisticsSummaryDto> GetProjectStatisticsSummaryAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
