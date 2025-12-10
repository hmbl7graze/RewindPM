using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Repositories;

/// <summary>
/// ReadModelへのアクセスを抽象化するリポジトリインターフェース
/// Infrastructure.Read層で実装される
/// </summary>
public interface IReadModelRepository
{
    /// <summary>
    /// 全プロジェクトを取得
    /// </summary>
    Task<List<ProjectDto>> GetAllProjectsAsync();

    /// <summary>
    /// 指定されたIDのプロジェクトを取得
    /// </summary>
    /// <param name="projectId">プロジェクトID</param>
    /// <returns>プロジェクト、存在しない場合はnull</returns>
    Task<ProjectDto?> GetProjectByIdAsync(Guid projectId);

    /// <summary>
    /// 指定されたプロジェクトに属する全タスクを取得
    /// </summary>
    /// <param name="projectId">プロジェクトID</param>
    Task<List<TaskDto>> GetTasksByProjectIdAsync(Guid projectId);

    /// <summary>
    /// 指定されたIDのタスクを取得
    /// </summary>
    /// <param name="taskId">タスクID</param>
    /// <returns>タスク、存在しない場合はnull</returns>
    Task<TaskDto?> GetTaskByIdAsync(Guid taskId);

    /// <summary>
    /// 指定された時点のプロジェクト状態を取得（タイムトラベル用）
    /// </summary>
    /// <param name="projectId">プロジェクトID</param>
    /// <param name="pointInTime">取得する時点</param>
    /// <returns>その時点のプロジェクト、存在しない場合はnull</returns>
    Task<ProjectDto?> GetProjectAtTimeAsync(Guid projectId, DateTime pointInTime);

    /// <summary>
    /// 指定された時点のタスク状態を取得（タイムトラベル用）
    /// </summary>
    /// <param name="taskId">タスクID</param>
    /// <param name="pointInTime">取得する時点</param>
    /// <returns>その時点のタスク、存在しない場合はnull</returns>
    Task<TaskDto?> GetTaskAtTimeAsync(Guid taskId, DateTime pointInTime);

    /// <summary>
    /// 指定された時点のプロジェクトに属する全タスクを取得（タイムトラベル用）
    /// </summary>
    /// <param name="projectId">プロジェクトID</param>
    /// <param name="pointInTime">取得する時点</param>
    Task<List<TaskDto>> GetTasksByProjectIdAtTimeAsync(Guid projectId, DateTime pointInTime);

    /// <summary>
    /// 指定されたプロジェクトの編集日一覧を取得（リワインド機能用）
    /// タスクが作成・更新された日付のリストを返す
    /// </summary>
    /// <param name="projectId">プロジェクトID</param>
    /// <param name="ascending">昇順（古い順）かどうか。デフォルトはfalse（新しい順）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>編集日のリスト</returns>
    Task<List<DateTime>> GetProjectEditDatesAsync(Guid projectId, bool ascending = false, CancellationToken cancellationToken = default);
}
