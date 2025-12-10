using MediatR;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.QueryHandlers.Statistics;

/// <summary>
/// プロジェクト統計サマリクエリのハンドラ
/// </summary>
public class GetProjectStatisticsSummaryQueryHandler
    : IRequestHandler<GetProjectStatisticsSummaryQuery, ProjectStatisticsSummaryDto>
{
    private readonly IProjectStatisticsRepository _statisticsRepository;

    public GetProjectStatisticsSummaryQueryHandler(IProjectStatisticsRepository statisticsRepository)
    {
        _statisticsRepository = statisticsRepository;
    }

    public async Task<ProjectStatisticsSummaryDto> Handle(
        GetProjectStatisticsSummaryQuery request,
        CancellationToken cancellationToken)
    {
        return await _statisticsRepository.GetProjectStatisticsSummaryAsync(
            request.ProjectId,
            cancellationToken);
    }
}
