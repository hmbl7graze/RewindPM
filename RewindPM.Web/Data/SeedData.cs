using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;
using RewindPM.Infrastructure.Write.Services;
using Microsoft.Extensions.DependencyInjection;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Web.Data;

/// <summary>
/// サンプルデータをデータベースに追加するクラス
/// プロジェクト初期に全タスクの計画を立て、時系列に沿って実績を記録していく
/// 月次で見直しを行い、遅延に応じて計画を後ろ倒しにする
/// 当初60日の計画が、最終的に90日（3か月）かかる様子を表現する
/// 前のタスクの遅延が次のタスクに影響する
/// </summary>
public class SeedData
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FixedDateTimeProvider _dateTimeProvider;
    private readonly DateTime _projectStartDate;

    public SeedData(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // SeedData用にFixedDateTimeProviderを作成（過去90日前から開始）
        var startDate = DateTime.UtcNow.AddDays(-90).Date; // 日付のみ、時刻は00:00:00
        _projectStartDate = startDate.AddHours(9); // 09:00から開始
        _dateTimeProvider = new FixedDateTimeProvider(_projectStartDate);
    }

    /// <summary>
    /// サンプルプロジェクトとタスクを作成
    /// 当初60日で完了する計画を立てるが、遅延が発生し月次見直しで計画を後ろ倒しにする
    /// 最終的に90日（3か月）かかる
    /// </summary>
    public async Task SeedAsync()
    {
        // スコープを作成してRepositoryを取得
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository>();

        // 90日前の09:00: プロジェクト作成
        var projectId = Guid.NewGuid();
        var project = ProjectAggregate.Create(
            projectId,
            "ECサイトリニューアルプロジェクト",
            "既存ECサイトの全面リニューアル。モダンなUIと高速なパフォーマンスを実現し、ユーザー体験を向上させる。当初の計画は60日間だったが、遅延が発生し月次で計画を見直した結果、最終的には90日間（3か月）かかった。",
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(project);

        // ========== プロジェクト初期：全タスクの計画を立てる（当初60日で完了する予定）==========

        var taskPlans = new List<(Guid Id, string Title, string Description, int ScheduledStartDay, int ScheduledDuration, int EstimatedHours)>
        {
            // 初期フェーズ：要件定義・調査
            (Guid.NewGuid(), "プロジェクトキックオフ", "プロジェクトメンバー、ステークホルダーとのキックオフ。プロジェクト目標、スコープ、スケジュールの共有。", 1, 2, 16),
            (Guid.NewGuid(), "要件定義", "ビジネス要件、機能要件、非機能要件の定義。", 3, 5, 40),
            (Guid.NewGuid(), "ユーザー調査", "競合調査、ユーザーインタビュー、ペルソナ設計。", 8, 4, 32),

            // 設計フェーズ
            (Guid.NewGuid(), "情報アーキテクチャ設計", "サイト構造、ナビゲーション、カテゴリー設計。", 12, 3, 24),
            (Guid.NewGuid(), "画面設計", "主要画面のワイヤーフレームとデザイン。", 15, 5, 40),
            (Guid.NewGuid(), "デザインシステム構築", "カラーパレット、タイポグラフィ、コンポーネントライブラリの作成。", 20, 3, 24),
            (Guid.NewGuid(), "データベース設計", "テーブル設計、ER図作成、インデックス設計。", 23, 3, 24),
            (Guid.NewGuid(), "REST API設計", "エンドポイント設計とOpenAPI仕様書作成。", 26, 3, 24),

            // 開発フェーズ前半
            (Guid.NewGuid(), "開発環境構築", "Git、CI/CD、Docker環境のセットアップ。開発ガイドラインの作成。", 29, 2, 16),
            (Guid.NewGuid(), "フロントエンド基盤開発", "共通コンポーネント、ヘッダー・フッター、レイアウトの実装。", 31, 5, 40),
            (Guid.NewGuid(), "フロントエンド画面実装", "トップページ、商品一覧、検索・フィルター、商品詳細画面の実装。", 36, 6, 48),
            (Guid.NewGuid(), "バックエンドAPI実装（認証・商品）", "認証、ユーザー管理、商品管理、検索APIの実装。", 42, 6, 48),

            // 開発フェーズ後半
            (Guid.NewGuid(), "フロントエンド購入フロー実装", "カート、チェックアウト、マイページの実装。", 48, 5, 40),
            (Guid.NewGuid(), "バックエンドAPI実装（注文・決済）", "カート・注文管理、決済サービス連携の実装。", 53, 4, 32),

            // テスト・リリースフェーズ
            (Guid.NewGuid(), "統合テスト", "全機能の統合テスト実施。主要フローの動作確認。", 57, 2, 16),
        };

        // プロジェクト初期（14:00）に全タスクを計画として作成（60日で完了する予定）
        _dateTimeProvider.SetCurrentTime(_projectStartDate.AddHours(5)); // 14:00

        foreach (var plan in taskPlans)
        {
            var scheduledStart = _projectStartDate.AddDays(plan.ScheduledStartDay - 1); // Day 1 = 開始日
            var scheduledEnd = scheduledStart.AddDays(plan.ScheduledDuration - 1);

            var task = TaskAggregate.Create(
                plan.Id,
                projectId,
                plan.Title,
                plan.Description,
                new ScheduledPeriod(scheduledStart, scheduledEnd, plan.EstimatedHours),
                "admin",
                _dateTimeProvider
            );
            await repository.SaveAsync(task);
        }

        Console.WriteLine("[SeedData] Initial plan created: 15 tasks scheduled to complete in 60 days");

        // ========== タスク実行開始 ==========

        var taskIds = taskPlans.Select(p => p.Id).ToList();

        // タスクの実績工数（予定よりも多くかかる）
        var actualResults = new List<(int Index, int ActualDuration, int ActualHours)>
        {
            // Phase 1: 最初の8タスク
            (0, 2, 16),    // キックオフ（予定2日→実2日）
            (1, 6, 48),   // 要件定義（予定5日→実6日、+1日遅延）
            (2, 4, 32),   // ユーザー調査（予定4日→実4日）
            (3, 3, 24),   // 情報アーキテクチャ（予定3日→実3日）
            (4, 6, 48),   // 画面設計（予定5日→実6日、+1日遅延）
            (5, 3, 24),   // デザインシステム（予定3日→実3日）
            (6, 4, 32),   // DB設計（予定3日→実4日、+1日遅延）
            (7, 3, 24),   // API設計（予定3日→実3日）

            // Phase 2: 次の5タスク
            (8, 3, 24),   // 開発環境（予定2日→実3日、+1日遅延）
            (9, 8, 64),   // フロントエンド基盤（予定5日→実8日、+3日遅延）
            (10, 8, 64),  // フロントエンド画面（予定6日→実8日、+2日遅延）
            (11, 9, 72),  // バックエンドAPI（認証・商品）（予定6日→実9日、+3日遅延）
            (12, 6, 48),  // フロントエンド購入フロー（予定5日→実6日、+1日遅延）

            // Phase 3: 残りの2タスク
            (13, 5, 40),  // バックエンドAPI（注文・決済）（予定4日→実5日、+1日遅延）
            (14, 3, 24),  // 統合テスト（予定2日→実3日、+1日遅延）
        };

        // Phase 1: 最初の8タスクを実行
        int completedCount = 0;
        for (int i = 0; i < 8; i++)
        {
            var result = actualResults[i];
            await StartAndCompleteTaskSequentially(repository, taskIds[result.Index],
                actualDuration: result.ActualDuration,
                actualHours: result.ActualHours);
            completedCount++;
        }

        var elapsedDays = (_dateTimeProvider.UtcNow - _projectStartDate).Days;
        Console.WriteLine($"[SeedData] Phase 1 completed: {completedCount} tasks done, {elapsedDays} days elapsed (planned ~28 days)");

        // ========== 第1回月次見直し（プロジェクト開始から31日後）==========
        var reviewDate1 = _projectStartDate.AddDays(31).Date.AddHours(15);
        _dateTimeProvider.SetCurrentTime(reviewDate1);

        var actualElapsed1 = (_dateTimeProvider.UtcNow - _projectStartDate).Days;
        Console.WriteLine($"[SeedData] Month 1 Review (Day 31 from start): Actually {actualElapsed1} calendar days elapsed");

        // 未完了タスク（8以降）の予定を後ろ倒し
        int delayDays1 = actualElapsed1 - 28; // 実際の経過日数 - 予定28日
        await RescheduleRemainingTasks(repository, taskIds, 8, delayDays1); // 遅延分

        Console.WriteLine($"[SeedData] Rescheduled remaining tasks +{delayDays1} days");

        // Phase 2: 次の5タスクを実行
        // 見直し後、翌営業日から再開
        //_dateTimeProvider.SetCurrentTime(_dateTimeProvider.UtcNow.Date.AddDays(1).AddHours(9));

        for (int i = 8; i < 13; i++)
        {
            var result = actualResults[i];
            await StartAndCompleteTaskSequentially(repository, taskIds[result.Index],
                actualDuration: result.ActualDuration,
                actualHours: result.ActualHours);
            completedCount++;
        }

        elapsedDays = (_dateTimeProvider.UtcNow - _projectStartDate).Days;
        Console.WriteLine($"[SeedData] Phase 2 completed: {completedCount} tasks done, {elapsedDays} days elapsed (planned ~55 days)");

        // ========== 第2回月次見直し（プロジェクト開始から65日後）==========
        var reviewDate2 = _projectStartDate.AddDays(65).Date.AddHours(15);
        _dateTimeProvider.SetCurrentTime(reviewDate2);

        var actualElapsed2 = (_dateTimeProvider.UtcNow - _projectStartDate).Days;
        Console.WriteLine($"[SeedData] Month 2 Review (Day 65 from start): Actually {actualElapsed2} calendar days elapsed");

        // 未完了タスク（13以降）の予定をさらに後ろ倒し
        int delayDays2 = actualElapsed2 - 55; // 実際の経過日数 - 予定55日
        await RescheduleRemainingTasks(repository, taskIds, 13, delayDays2); // 遅延分

        Console.WriteLine($"[SeedData] Rescheduled remaining tasks +{delayDays2} days");

        // Phase 3: 残りのタスクを実行
        // 見直し後、翌営業日から再開
        // _dateTimeProvider.SetCurrentTime(_dateTimeProvider.UtcNow.Date.AddDays(1).AddHours(9));

        for (int i = 13; i < 15; i++)
        {
            var result = actualResults[i];
            await StartAndCompleteTaskSequentially(repository, taskIds[result.Index],
                actualDuration: result.ActualDuration,
                actualHours: result.ActualHours);
            completedCount++;
        }

        elapsedDays = (_dateTimeProvider.UtcNow - _projectStartDate).Days;
        Console.WriteLine($"[SeedData] Phase 3 completed: {completedCount} tasks done, {elapsedDays} days elapsed");

        // ========== 追加タスク（予定外の作業が発生）==========

        // 追加タスク1: パフォーマンス改善
        //_dateTimeProvider.Advance(TimeSpan.FromDays(1));
        var addTask1Id = Guid.NewGuid();
        var addTask1 = TaskAggregate.Create(
            addTask1Id,
            projectId,
            "【追加】初期ロードパフォーマンス改善",
            "【追加タスク】ページロードが遅いとの報告を受け、画像最適化とコード分割を実装。",
            new ScheduledPeriod(_dateTimeProvider.UtcNow, _dateTimeProvider.UtcNow.AddDays(3), 24),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(addTask1);
        await StartAndCompleteTaskSequentially(repository, addTask1Id, actualDuration: 3, actualHours: 24);

        Console.WriteLine("[SeedData] Unplanned task added: Performance optimization");

        // 追加タスク2: セキュリティ対策
        //_dateTimeProvider.Advance(TimeSpan.FromDays(1));
        var addTask2Id = Guid.NewGuid();
        var addTask2 = TaskAggregate.Create(
            addTask2Id,
            projectId,
            "【追加】セキュリティ対策強化",
            "【追加タスク】セキュリティ監査で指摘された脆弱性の修正。SQLインジェクション、XSS対策を強化。",
            new ScheduledPeriod(_dateTimeProvider.UtcNow, _dateTimeProvider.UtcNow.AddDays(2), 16),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(addTask2);
        await StartAndCompleteTaskSequentially(repository, addTask2Id, actualDuration: 2, actualHours: 16);

        Console.WriteLine("[SeedData] Unplanned task added: Security enhancements");

        // 追加タスク3: ドキュメント整備（進行中）
        //_dateTimeProvider.Advance(TimeSpan.FromDays(1));
        var task37Id = Guid.NewGuid();
        var task37 = TaskAggregate.Create(
            task37Id,
            projectId,
            "運用監視とドキュメント整備",
            "リリース後の運用監視体制の確立と運用ドキュメントの整備。現在進行中。",
            new ScheduledPeriod(
                _dateTimeProvider.UtcNow,
                _dateTimeProvider.UtcNow.AddDays(3),
                24),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task37);
        await StartTask(repository, task37Id);

        elapsedDays = (_dateTimeProvider.UtcNow - _projectStartDate).Days;
        Console.WriteLine($"[SeedData] Final task in progress: Operations documentation");
        Console.WriteLine("[SeedData] ===== Project Summary =====");
        Console.WriteLine($"[SeedData] Project start: {_projectStartDate:yyyy-MM-dd}");
        Console.WriteLine($"[SeedData] Current date: {_dateTimeProvider.UtcNow:yyyy-MM-dd}");
        Console.WriteLine($"[SeedData] Actual days elapsed: {elapsedDays} days");
        Console.WriteLine("[SeedData] Initial plan: 60 days, 15 tasks");
        Console.WriteLine($"[SeedData] Actual result: {elapsedDays} days, 18 tasks (15 planned + 3 unplanned)");
        Console.WriteLine("[SeedData] Month 1 Review (Day 31): Rescheduled due to delays");
        Console.WriteLine("[SeedData] Month 2 Review (Day 65): Rescheduled due to continued delays");
        Console.WriteLine($"[SeedData] Total delay: {elapsedDays - 60} days from original 60-day plan");
        Console.WriteLine("[SeedData] Status: 17 tasks completed, 1 task in progress");
    }

    /// <summary>
    /// 月次見直し：未完了タスクの予定を後ろ倒しにする
    /// </summary>
    private async Task RescheduleRemainingTasks(
        IAggregateRepository repository,
        List<Guid> taskIds,
        int startIndex,
        int delayDays)
    {
        for (int i = startIndex; i < taskIds.Count; i++)
        {
            var task = await repository.GetByIdAsync<TaskAggregate>(taskIds[i]);
            if (task == null || task.Status == TaskStatus.Done)
            {
                continue; // 完了済みタスクはスキップ
            }

            // 予定を後ろ倒し
            var newScheduledStart = task.ScheduledPeriod.StartDate?.AddDays(delayDays);
            var newScheduledEnd = task.ScheduledPeriod.EndDate?.AddDays(delayDays);
            var newScheduledPeriod = new ScheduledPeriod(
                newScheduledStart,
                newScheduledEnd,
                task.ScheduledPeriod.EstimatedHours
            );

            task.ChangeSchedule(newScheduledPeriod, "admin", _dateTimeProvider);
            await repository.SaveAsync(task);
        }
    }

    /// <summary>
    /// タスクを順次開始・完了するヘルパーメソッド
    /// 前のタスク完了後、翌営業日に次のタスクを開始する
    /// </summary>
    private async Task StartAndCompleteTaskSequentially(
        IAggregateRepository repository,
        Guid taskId,
        int actualDuration,
        int actualHours)
    {
        // タスクを取得してInProgressに変更
        var task = await repository.GetByIdAsync<TaskAggregate>(taskId);
        task!.ChangeStatus(TaskStatus.InProgress, "admin", _dateTimeProvider);

        // 実績開始日を設定
        var actualStart = _dateTimeProvider.UtcNow;
        task.ChangeActualPeriod(
            new ActualPeriod(actualStart, null, null),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task);

        // タスク完了（actualDuration日後の17:00）
        _dateTimeProvider.Advance(TimeSpan.FromDays(actualDuration - 1) + TimeSpan.FromHours(8)); // 17:00に完了
        task = await repository.GetByIdAsync<TaskAggregate>(taskId);

        // ステータスをDoneに変更
        task!.ChangeStatus(TaskStatus.Done, "admin", _dateTimeProvider);

        // 実績終了日と実績工数を設定
        var actualEnd = _dateTimeProvider.UtcNow;
        task.ChangeActualPeriod(
            new ActualPeriod(actualStart, actualEnd, actualHours),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task);

        // 翌営業日の9:00にタスク開始（前のタスク完了の翌日）
        _dateTimeProvider.SetCurrentTime(_dateTimeProvider.UtcNow.Date.AddDays(1).AddHours(9));
    }

    /// <summary>
    /// タスクを開始するヘルパーメソッド（完了しない）
    /// </summary>
    private async Task StartTask(
        IAggregateRepository repository,
        Guid taskId)
    {
        // 1時間後にタスク開始
        _dateTimeProvider.Advance(TimeSpan.FromHours(1));

        var task = await repository.GetByIdAsync<TaskAggregate>(taskId);

        // ステータスをInProgressに変更
        task!.ChangeStatus(TaskStatus.InProgress, "admin", _dateTimeProvider);

        // 実績開始日を設定
        task.ChangeActualPeriod(
            new ActualPeriod(_dateTimeProvider.UtcNow, null, null),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task);
    }
}
