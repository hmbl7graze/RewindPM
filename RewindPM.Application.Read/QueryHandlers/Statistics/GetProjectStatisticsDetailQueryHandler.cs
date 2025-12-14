using MediatR;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.QueryHandlers.Statistics;

/// <summary>
/// プロジェクト詳細統計情報取得クエリハンドラ
/// </summary>
public class GetProjectStatisticsDetailQueryHandler
    : IRequestHandler<GetProjectStatisticsDetailQuery, ProjectStatisticsDetailDto?>
{
    private readonly IProjectStatisticsRepository _repository;

    public GetProjectStatisticsDetailQueryHandler(IProjectStatisticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProjectStatisticsDetailDto?> Handle(
        GetProjectStatisticsDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetProjectStatisticsDetailAsync(
            request.ProjectId,
            request.AsOfDate ?? DateTimeOffset.UtcNow,
            cancellationToken);
    }
}
