using MediatR;
using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Queries.Projects;

/// <summary>
/// 指定されたIDのプロジェクトを取得するクエリ
/// </summary>
/// <param name="ProjectId">プロジェクトID</param>
public record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDto?>;
