using MediatR;
using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Queries.Projects;

/// <summary>
/// 全プロジェクトの一覧を取得するクエリ
/// </summary>
public record GetAllProjectsQuery() : IRequest<List<ProjectDto>>;
