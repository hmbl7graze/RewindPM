using MediatR;
using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Queries.Statistics;

/// <summary>
/// プロジェクトカード用の統計情報を取得
/// </summary>
public record GetProjectStatisticsSummaryQuery(Guid ProjectId) : IRequest<ProjectStatisticsSummaryDto>;
