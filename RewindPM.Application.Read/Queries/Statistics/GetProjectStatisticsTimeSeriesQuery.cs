using MediatR;
using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Queries.Statistics;

/// <summary>
/// プロジェクト統計の時系列データ取得クエリ
/// </summary>
public record GetProjectStatisticsTimeSeriesQuery(
    Guid ProjectId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate
) : IRequest<ProjectStatisticsTimeSeriesDto?>;
