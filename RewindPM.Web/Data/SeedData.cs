using MediatR;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Commands.Tasks;

namespace RewindPM.Web.Data;

/// <summary>
/// サンプルデータをデータベースに追加するクラス
/// </summary>
public class SeedData
{
    private readonly IMediator _mediator;

    public SeedData(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// サンプルプロジェクトとタスクを作成
    /// </summary>
    public async Task SeedAsync()
    {
        // プロジェクト: ECサイトリニューアルプロジェクト
        var projectId = Guid.NewGuid();
        await _mediator.Send(new CreateProjectCommand(
            Id: projectId,
            Title: "ECサイトリニューアルプロジェクト",
            Description: "既存ECサイトの全面リニューアル。モダンなUIと高速なパフォーマンスを実現し、ユーザー体験を向上させる。",
            CreatedBy: "admin"
        ));

        // タスク1: 要件定義（完了、実績あり）
        var task1Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task1Id,
            ProjectId: projectId,
            Title: "要件定義",
            Description: "ステークホルダーへのヒアリングと要件定義書の作成",
            ScheduledStartDate: DateTime.Now.AddDays(-60),
            ScheduledEndDate: DateTime.Now.AddDays(-50),
            EstimatedHours: 40,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task1Id,
            ActualStartDate: DateTime.Now.AddDays(-60),
            ActualEndDate: DateTime.Now.AddDays(-49),
            ActualHours: 38,
            ChangedBy: "admin"
        ));

        // タスク2: 競合サイト調査（完了、実績あり）
        var task2Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task2Id,
            ProjectId: projectId,
            Title: "競合サイト調査",
            Description: "競合他社のECサイトの機能・UI/UX分析",
            ScheduledStartDate: DateTime.Now.AddDays(-55),
            ScheduledEndDate: DateTime.Now.AddDays(-48),
            EstimatedHours: 20,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task2Id,
            ActualStartDate: DateTime.Now.AddDays(-55),
            ActualEndDate: DateTime.Now.AddDays(-47),
            ActualHours: 22,
            ChangedBy: "admin"
        ));

        // タスク3: 画面設計（完了、実績あり）
        var task3Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task3Id,
            ProjectId: projectId,
            Title: "画面設計・ワイヤーフレーム作成",
            Description: "全画面のワイヤーフレームとUIデザインの作成",
            ScheduledStartDate: DateTime.Now.AddDays(-48),
            ScheduledEndDate: DateTime.Now.AddDays(-35),
            EstimatedHours: 60,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task3Id,
            ActualStartDate: DateTime.Now.AddDays(-48),
            ActualEndDate: DateTime.Now.AddDays(-34),
            ActualHours: 65,
            ChangedBy: "admin"
        ));

        // タスク4: データベース設計（完了、実績あり）
        var task4Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task4Id,
            ProjectId: projectId,
            Title: "データベース設計",
            Description: "ER図の作成、テーブル定義、インデックス設計",
            ScheduledStartDate: DateTime.Now.AddDays(-40),
            ScheduledEndDate: DateTime.Now.AddDays(-30),
            EstimatedHours: 35,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task4Id,
            ActualStartDate: DateTime.Now.AddDays(-40),
            ActualEndDate: DateTime.Now.AddDays(-29),
            ActualHours: 32,
            ChangedBy: "admin"
        ));

        // タスク5: API設計（完了、実績あり）
        var task5Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task5Id,
            ProjectId: projectId,
            Title: "REST API設計",
            Description: "エンドポイント設計、OpenAPI仕様書作成",
            ScheduledStartDate: DateTime.Now.AddDays(-35),
            ScheduledEndDate: DateTime.Now.AddDays(-28),
            EstimatedHours: 25,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task5Id,
            ActualStartDate: DateTime.Now.AddDays(-35),
            ActualEndDate: DateTime.Now.AddDays(-27),
            ActualHours: 28,
            ChangedBy: "admin"
        ));

        // タスク6: 認証機能実装（完了、実績あり）
        var task6Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task6Id,
            ProjectId: projectId,
            Title: "ユーザー認証機能実装",
            Description: "JWT認証、パスワードリセット機能の実装",
            ScheduledStartDate: DateTime.Now.AddDays(-28),
            ScheduledEndDate: DateTime.Now.AddDays(-20),
            EstimatedHours: 30,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task6Id,
            ActualStartDate: DateTime.Now.AddDays(-28),
            ActualEndDate: DateTime.Now.AddDays(-19),
            ActualHours: 35,
            ChangedBy: "admin"
        ));

        // タスク7: 商品管理機能実装（完了、実績あり）
        var task7Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task7Id,
            ProjectId: projectId,
            Title: "商品管理機能実装",
            Description: "商品の登録・編集・削除、カテゴリ管理",
            ScheduledStartDate: DateTime.Now.AddDays(-25),
            ScheduledEndDate: DateTime.Now.AddDays(-15),
            EstimatedHours: 45,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task7Id,
            ActualStartDate: DateTime.Now.AddDays(-25),
            ActualEndDate: DateTime.Now.AddDays(-14),
            ActualHours: 48,
            ChangedBy: "admin"
        ));

        // タスク8: カート機能実装（完了、実績あり）
        var task8Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task8Id,
            ProjectId: projectId,
            Title: "ショッピングカート機能実装",
            Description: "カート追加・削除、数量変更、一時保存機能",
            ScheduledStartDate: DateTime.Now.AddDays(-20),
            ScheduledEndDate: DateTime.Now.AddDays(-12),
            EstimatedHours: 40,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task8Id,
            ActualStartDate: DateTime.Now.AddDays(-20),
            ActualEndDate: DateTime.Now.AddDays(-11),
            ActualHours: 42,
            ChangedBy: "admin"
        ));

        // タスク9: 決済機能実装（進行中、実績あり）
        var task9Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task9Id,
            ProjectId: projectId,
            Title: "決済機能実装",
            Description: "クレジットカード決済、コンビニ決済の実装",
            ScheduledStartDate: DateTime.Now.AddDays(-15),
            ScheduledEndDate: DateTime.Now.AddDays(-5),
            EstimatedHours: 50,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task9Id,
            ActualStartDate: DateTime.Now.AddDays(-15),
            ActualEndDate: null,
            ActualHours: 30,
            ChangedBy: "admin"
        ));

        // タスク10: 注文管理機能実装（進行中、実績あり）
        var task10Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task10Id,
            ProjectId: projectId,
            Title: "注文管理機能実装",
            Description: "注文履歴、注文詳細、ステータス管理",
            ScheduledStartDate: DateTime.Now.AddDays(-12),
            ScheduledEndDate: DateTime.Now.AddDays(-3),
            EstimatedHours: 35,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task10Id,
            ActualStartDate: DateTime.Now.AddDays(-12),
            ActualEndDate: null,
            ActualHours: 20,
            ChangedBy: "admin"
        ));

        // タスク11: 検索機能実装（進行中、実績あり）
        var task11Id = Guid.NewGuid();
        await _mediator.Send(new CreateTaskCommand(
            Id: task11Id,
            ProjectId: projectId,
            Title: "商品検索機能実装",
            Description: "全文検索、フィルタリング、並び替え機能",
            ScheduledStartDate: DateTime.Now.AddDays(-10),
            ScheduledEndDate: DateTime.Now.AddDays(2),
            EstimatedHours: 40,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));
        await _mediator.Send(new ChangeTaskActualPeriodCommand(
            TaskId: task11Id,
            ActualStartDate: DateTime.Now.AddDays(-10),
            ActualEndDate: null,
            ActualHours: 25,
            ChangedBy: "admin"
        ));

        // タスク12: レビュー機能実装（TODO、実績なし）
        await _mediator.Send(new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            Title: "商品レビュー機能実装",
            Description: "レビュー投稿・編集・削除、評価機能",
            ScheduledStartDate: DateTime.Now.AddDays(-5),
            ScheduledEndDate: DateTime.Now.AddDays(5),
            EstimatedHours: 30,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));

        // タスク13: お気に入り機能実装（TODO、実績なし）
        await _mediator.Send(new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            Title: "お気に入り機能実装",
            Description: "お気に入り登録・削除、一覧表示",
            ScheduledStartDate: DateTime.Now.AddDays(-3),
            ScheduledEndDate: DateTime.Now.AddDays(4),
            EstimatedHours: 20,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));

        // タスク14: クーポン機能実装（TODO、実績なし）
        await _mediator.Send(new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            Title: "クーポン・割引機能実装",
            Description: "クーポンコード適用、割引計算機能",
            ScheduledStartDate: DateTime.Now.AddDays(1),
            ScheduledEndDate: DateTime.Now.AddDays(8),
            EstimatedHours: 25,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));

        // タスク15: 管理画面実装（TODO、実績なし）
        await _mediator.Send(new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            Title: "管理者用ダッシュボード実装",
            Description: "売上統計、ユーザー管理、商品管理画面",
            ScheduledStartDate: DateTime.Now.AddDays(3),
            ScheduledEndDate: DateTime.Now.AddDays(15),
            EstimatedHours: 60,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));

        // タスク16: メール通知機能実装（TODO、実績なし）
        await _mediator.Send(new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            Title: "メール通知機能実装",
            Description: "注文確認、発送通知、パスワードリセットメール",
            ScheduledStartDate: DateTime.Now.AddDays(5),
            ScheduledEndDate: DateTime.Now.AddDays(12),
            EstimatedHours: 30,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));

        // タスク17: パフォーマンス最適化（TODO、実績なし）
        await _mediator.Send(new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            Title: "パフォーマンス最適化",
            Description: "クエリ最適化、キャッシュ実装、画像最適化",
            ScheduledStartDate: DateTime.Now.AddDays(10),
            ScheduledEndDate: DateTime.Now.AddDays(18),
            EstimatedHours: 40,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));

        // タスク18: セキュリティ対策（TODO、実績なし）
        await _mediator.Send(new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            Title: "セキュリティ対策実装",
            Description: "XSS対策、CSRF対策、SQLインジェクション対策",
            ScheduledStartDate: DateTime.Now.AddDays(12),
            ScheduledEndDate: DateTime.Now.AddDays(20),
            EstimatedHours: 35,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));

        // タスク19: 単体テスト作成（TODO、実績なし）
        await _mediator.Send(new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            Title: "単体テスト作成",
            Description: "全API・ビジネスロジックの単体テスト作成",
            ScheduledStartDate: DateTime.Now.AddDays(15),
            ScheduledEndDate: DateTime.Now.AddDays(25),
            EstimatedHours: 50,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));

        // タスク20: E2Eテスト作成（TODO、実績なし）
        await _mediator.Send(new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            Title: "E2Eテスト作成",
            Description: "主要フローのE2Eテストシナリオ作成と実行",
            ScheduledStartDate: DateTime.Now.AddDays(20),
            ScheduledEndDate: DateTime.Now.AddDays(30),
            EstimatedHours: 45,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));

        // タスク21: 本番環境デプロイ（TODO、実績なし）
        await _mediator.Send(new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: projectId,
            Title: "本番環境デプロイ",
            Description: "本番環境へのデプロイとリリース作業",
            ScheduledStartDate: DateTime.Now.AddDays(28),
            ScheduledEndDate: DateTime.Now.AddDays(32),
            EstimatedHours: 20,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "admin"
        ));

        Console.WriteLine("[SeedData] Sample project and 21 tasks have been created successfully.");
    }
}
