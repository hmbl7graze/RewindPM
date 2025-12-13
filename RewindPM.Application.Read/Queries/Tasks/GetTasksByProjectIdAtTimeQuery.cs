using MediatR;
using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Queries.Tasks;

/// <summary>
/// 指定された時点のプロジェクトに属する全タスクを取得するクエリ（タイムトラベル用）
/// </summary>
/// <param name="ProjectId">プロジェクトID</param>
/// <param name="PointInTime">取得する時点</param>
public record GetTasksByProjectIdAtTimeQuery(Guid ProjectId, DateTimeOffset PointInTime) : IRequest<List<TaskDto>>;
