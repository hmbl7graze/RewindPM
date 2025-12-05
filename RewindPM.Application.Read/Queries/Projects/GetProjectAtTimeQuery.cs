using MediatR;
using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Queries.Projects;

/// <summary>
/// 指定された時点のプロジェクト状態を取得するクエリ（タイムトラベル用）
/// </summary>
/// <param name="ProjectId">プロジェクトID</param>
/// <param name="PointInTime">取得する時点</param>
public record GetProjectAtTimeQuery(Guid ProjectId, DateTime PointInTime) : IRequest<ProjectDto?>;
