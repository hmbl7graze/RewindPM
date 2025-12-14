using RewindPM.Domain.Common;
using RewindPM.Domain.Events;

namespace RewindPM.Domain.Aggregates;

/// <summary>
/// プロジェクトのAggregate
/// プロジェクトの基本情報を管理する
/// </summary>
public class ProjectAggregate : AggregateRoot
{
    /// <summary>
    /// プロジェクトのタイトル
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// プロジェクトの説明
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// プロジェクトを作成したユーザーID
    /// </summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>
    /// プロジェクトを最後に更新したユーザーID
    /// </summary>
    public string UpdatedBy { get; private set; } = string.Empty;

    /// <summary>
    /// デフォルトコンストラクタ（イベントリプレイ用）
    /// テストからアクセス可能にするためinternalとする
    /// </summary>
    internal ProjectAggregate()
    {
    }

    /// <summary>
    /// 新しいプロジェクトを作成する
    /// </summary>
    /// <param name="id">プロジェクトID</param>
    /// <param name="title">プロジェクトのタイトル</param>
    /// <param name="description">プロジェクトの説明</param>
    /// <param name="createdBy">作成者のユーザーID</param>
    /// <param name="dateTimeProvider">時刻プロバイダー</param>
    /// <returns>新しいProjectAggregateインスタンス</returns>
    public static ProjectAggregate Create(Guid id, string title, string description, string createdBy, IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("プロジェクトのタイトルは必須です");
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new DomainException("作成者のユーザーIDは必須です");
        }

        var project = new ProjectAggregate();
        project.ApplyEvent(new ProjectCreated
        {
            AggregateId = id,
            OccurredAt = dateTimeProvider.UtcNow,
            Title = title,
            Description = description ?? string.Empty,
            CreatedBy = createdBy
        });

        return project;
    }

    /// <summary>
    /// プロジェクトの情報を更新する
    /// </summary>
    /// <param name="title">新しいタイトル</param>
    /// <param name="description">新しい説明</param>
    /// <param name="updatedBy">更新者のユーザーID</param>
    /// <param name="dateTimeProvider">時刻プロバイダー</param>
    public void Update(string title, string description, string updatedBy, IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("プロジェクトのタイトルは必須です");
        }

        if (string.IsNullOrWhiteSpace(updatedBy))
        {
            throw new DomainException("更新者のユーザーIDは必須です");
        }

        ApplyEvent(new ProjectUpdated
        {
            AggregateId = Id,
            OccurredAt = dateTimeProvider.UtcNow,
            Title = title,
            Description = description ?? string.Empty,
            UpdatedBy = updatedBy
        });
    }

    /// <summary>
    /// プロジェクトを削除する（論理削除）
    /// </summary>
    /// <param name="deletedBy">削除者のユーザーID</param>
    /// <param name="dateTimeProvider">時刻プロバイダー</param>
    public void Delete(string deletedBy, IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            throw new DomainException("削除者のユーザーIDは必須です");
        }

        ApplyEvent(new ProjectDeleted
        {
            AggregateId = Id,
            OccurredAt = dateTimeProvider.UtcNow,
            DeletedBy = deletedBy,
            Reason = string.Empty
        });
    }

    /// <summary>
    /// イベントに応じてAggregateの状態を変更する
    /// </summary>
    /// <param name="event">適用するドメインイベント</param>
    protected override void When(IDomainEvent @event)
    {
        switch (@event)
        {
            case ProjectCreated e:
                Id = e.AggregateId;
                Title = e.Title;
                Description = e.Description;
                CreatedBy = e.CreatedBy;
                UpdatedBy = e.CreatedBy;
                break;

            case ProjectUpdated e:
                Title = e.Title;
                Description = e.Description;
                UpdatedBy = e.UpdatedBy;
                break;

            case ProjectDeleted:
                // 論理削除のため、Aggregate自体の状態は変更しない
                // イベントストアに記録するのみ
                break;
        }
    }
}
