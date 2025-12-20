using RewindPM.Infrastructure.Read.Entities;

namespace RewindPM.Infrastructure.Read.Services;

/// <summary>
/// ReadModelのデータベースコンテキストインターフェース
/// Projectionハンドラーがデータベースにアクセスするための抽象化層
/// </summary>
public interface IReadModelContext
{
    /// <summary>
    /// プロジェクトの現在状態を保持するテーブル
    /// </summary>
    IQueryable<ProjectEntity> Projects { get; }

    /// <summary>
    /// プロジェクトの履歴スナップショットを保持するテーブル
    /// </summary>
    IQueryable<ProjectHistoryEntity> ProjectHistories { get; }

    /// <summary>
    /// タスクの現在状態を保持するテーブル
    /// </summary>
    IQueryable<TaskEntity> Tasks { get; }

    /// <summary>
    /// タスクの履歴スナップショットを保持するテーブル
    /// </summary>
    IQueryable<TaskHistoryEntity> TaskHistories { get; }

    /// <summary>
    /// プロジェクトエンティティを追加する
    /// </summary>
    void AddProject(ProjectEntity project);

    /// <summary>
    /// プロジェクト履歴エンティティを追加する
    /// </summary>
    void AddProjectHistory(ProjectHistoryEntity projectHistory);

    /// <summary>
    /// タスクエンティティを追加する
    /// </summary>
    void AddTask(TaskEntity task);

    /// <summary>
    /// タスク履歴エンティティを追加する
    /// </summary>
    void AddTaskHistory(TaskHistoryEntity taskHistory);

    /// <summary>
    /// 変更を保存する
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
