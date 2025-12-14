using MediatR;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.QueryHandlers.Statistics;

/// <summary>
/// プロジェクト統計時系列データ取得クエリハンドラ
/// </summary>
public class GetProjectStatisticsTimeSeriesQueryHandler
    : IRequestHandler<GetProjectStatisticsTimeSeriesQuery, ProjectStatisticsTimeSeriesDto?>
{
    private readonly IProjectStatisticsRepository _repository;

    public GetProjectStatisticsTimeSeriesQueryHandler(IProjectStatisticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProjectStatisticsTimeSeriesDto?> Handle(
        GetProjectStatisticsTimeSeriesQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetProjectStatisticsTimeSeriesAsync(
            request.ProjectId,
            request.StartDate,
            request.EndDate,
            cancellationToken);
    }
}
