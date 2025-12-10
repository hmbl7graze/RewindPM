using MediatR;
using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Queries.Statistics;

/// <summary>
/// プロジェクト詳細統計情報取得クエリ
/// </summary>
public record GetProjectStatisticsDetailQuery(Guid ProjectId, DateTime? AsOfDate = null) 
    : IRequest<ProjectStatisticsDetailDto?>;
