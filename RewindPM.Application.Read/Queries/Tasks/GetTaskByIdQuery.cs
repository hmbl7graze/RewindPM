using MediatR;
using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Queries.Tasks;

/// <summary>
/// 指定されたIDのタスクを取得するクエリ
/// </summary>
/// <param name="TaskId">タスクID</param>
public record GetTaskByIdQuery(Guid TaskId) : IRequest<TaskDto?>;
