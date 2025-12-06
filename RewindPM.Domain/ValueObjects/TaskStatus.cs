namespace RewindPM.Domain.ValueObjects;

/// <summary>
/// タスクのステータスを表す列挙型
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// 未着手
    /// </summary>
    Todo = 0,

    /// <summary>
    /// 進行中
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// レビュー中
    /// </summary>
    InReview = 2,

    /// <summary>
    /// 完了
    /// </summary>
    Done = 3
}
