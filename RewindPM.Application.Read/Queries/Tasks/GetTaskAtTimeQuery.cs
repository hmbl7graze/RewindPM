using MediatR;
using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Queries.Tasks;

/// <summary>
/// 指定された時点のタスク状態を取得するクエリ（タイムトラベル用）
/// </summary>
/// <param name="TaskId">タスクID</param>
/// <param name="PointInTime">取得する時点</param>
public record GetTaskAtTimeQuery(Guid TaskId, DateTime PointInTime) : IRequest<TaskDto?>;
