using MediatR;
using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Queries.Tasks;

/// <summary>
/// 指定されたプロジェクトに属する全タスクを取得するクエリ
/// </summary>
/// <param name="ProjectId">プロジェクトID</param>
public record GetTasksByProjectIdQuery(Guid ProjectId) : IRequest<List<TaskDto>>;
