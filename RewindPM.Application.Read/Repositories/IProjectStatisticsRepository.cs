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

    /// <summary>
    /// プロジェクト詳細画面用の統計情報を取得
    /// </summary>
    /// <param name="projectId">プロジェクトID</param>
    /// <param name="asOfDate">統計の基準日（リワインド対応）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task<ProjectStatisticsDetailDto?> GetProjectStatisticsDetailAsync(
        Guid projectId,
        DateTimeOffset asOfDate,
        CancellationToken cancellationToken = default);
}
