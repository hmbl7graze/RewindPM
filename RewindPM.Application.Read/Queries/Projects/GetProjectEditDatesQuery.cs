using MediatR;

namespace RewindPM.Application.Read.Queries.Projects;

/// <summary>
/// 指定されたプロジェクトの編集日一覧を取得するクエリ
/// リワインド機能で日付移動に使用
/// </summary>
/// <param name="ProjectId">プロジェクトID</param>
/// <param name="Ascending">昇順（古い順）かどうか。デフォルトはfalse（新しい順）</param>
public record GetProjectEditDatesQuery(Guid ProjectId, bool Ascending = false) : IRequest<List<DateTimeOffset>>;
