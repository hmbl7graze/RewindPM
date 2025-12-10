using MediatR;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;
using RewindPM.Infrastructure.Write.Services;
using Microsoft.Extensions.DependencyInjection;

namespace RewindPM.Web.Data;

/// <summary>
/// サンプルデータをデータベースに追加するクラス
/// 時系列に沿ったイベントを作成するため、直接AggregateとEventStoreを使用
/// </summary>
public class SeedData
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FixedDateTimeProvider _dateTimeProvider;

    public SeedData(IMediator mediator, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // SeedData用にFixedDateTimeProviderを作成（過去60日前から開始）
        var startDate = DateTime.UtcNow.AddDays(-60).Date; // 日付のみ、時刻は00:00:00
        _dateTimeProvider = new FixedDateTimeProvider(startDate.AddHours(9)); // 09:00から開始
    }

    /// <summary>
    /// サンプルプロジェクトとタスクを作成
    /// 時系列に沿ってイベントを発生させる
    /// </summary>
    public async Task SeedAsync()
    {
        // スコープを作成してRepositoryを取得
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository>();

        // 60日前の09:00: プロジェクト作成
        var projectId = Guid.NewGuid();
        var project = ProjectAggregate.Create(
            projectId,
            "ECサイトリニューアルプロジェクト",
            "既存ECサイトの全面リニューアル。モダンなUIと高速なパフォーマンスを実現し、ユーザー体験を向上させる。",
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(project);

        // 59日前の10:00: タスク1作成（要件定義）
        _dateTimeProvider.Advance(TimeSpan.FromHours(25)); // 1日と1時間進める
        var task1Id = Guid.NewGuid();
        var task1 = TaskAggregate.Create(
            task1Id,
            projectId,
            "要件定義",
            "ステークホルダーへのヒアリングと要件定義書の作成",
            new ScheduledPeriod(DateTime.Now.AddDays(-60), DateTime.Now.AddDays(-50), 40),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task1);

        // 58日前の09:00: タスク1開始
        _dateTimeProvider.Advance(TimeSpan.FromHours(23));
        task1 = await repository.GetByIdAsync<TaskAggregate>(task1Id);
        task1!.ChangeActualPeriod(
            new ActualPeriod(DateTime.UtcNow.AddDays(-58), null, null),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task1);

        // 49日前の17:00: タスク1完了
        _dateTimeProvider.Advance(TimeSpan.FromDays(9) + TimeSpan.FromHours(8));
        task1 = await repository.GetByIdAsync<TaskAggregate>(task1Id);
        task1!.ChangeActualPeriod(
            new ActualPeriod(DateTime.UtcNow.AddDays(-58), DateTime.UtcNow.AddDays(-49), 38),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task1);

        // 54日前の10:00: タスク2作成（競合サイト調査）
        _dateTimeProvider.SetCurrentTime(DateTime.UtcNow.AddDays(-54).Date.AddHours(10));
        var task2Id = Guid.NewGuid();
        var task2 = TaskAggregate.Create(
            task2Id,
            projectId,
            "競合サイト調査",
            "競合他社のECサイトの機能・UI/UX分析",
            new ScheduledPeriod(DateTime.Now.AddDays(-55), DateTime.Now.AddDays(-48), 20),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task2);

        // 53日前の09:00: タスク2開始
        _dateTimeProvider.Advance(TimeSpan.FromHours(23));
        task2 = await repository.GetByIdAsync<TaskAggregate>(task2Id);
        task2!.ChangeActualPeriod(
            new ActualPeriod(DateTime.UtcNow.AddDays(-53), null, null),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task2);

        // 47日前の18:00: タスク2完了
        _dateTimeProvider.Advance(TimeSpan.FromDays(6) + TimeSpan.FromHours(9));
        task2 = await repository.GetByIdAsync<TaskAggregate>(task2Id);
        task2!.ChangeActualPeriod(
            new ActualPeriod(DateTime.UtcNow.AddDays(-53), DateTime.UtcNow.AddDays(-47), 22),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task2);

        // 47日前の11:00: タスク3作成（画面設計）
        _dateTimeProvider.SetCurrentTime(DateTime.UtcNow.AddDays(-47).Date.AddHours(11));
        var task3Id = Guid.NewGuid();
        var task3 = TaskAggregate.Create(
            task3Id,
            projectId,
            "画面設計・ワイヤーフレーム作成",
            "全画面のワイヤーフレームとUIデザインの作成",
            new ScheduledPeriod(DateTime.Now.AddDays(-48), DateTime.Now.AddDays(-35), 60),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task3);

        // 46日前の09:00: タスク3開始
        _dateTimeProvider.Advance(TimeSpan.FromHours(22));
        task3 = await repository.GetByIdAsync<TaskAggregate>(task3Id);
        task3!.ChangeActualPeriod(
            new ActualPeriod(DateTime.UtcNow.AddDays(-46), null, null),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task3);

        // 34日前の16:00: タスク3完了
        _dateTimeProvider.Advance(TimeSpan.FromDays(12) + TimeSpan.FromHours(7));
        task3 = await repository.GetByIdAsync<TaskAggregate>(task3Id);
        task3!.ChangeActualPeriod(
            new ActualPeriod(DateTime.UtcNow.AddDays(-46), DateTime.UtcNow.AddDays(-34), 65),
            "admin",
            _dateTimeProvider
        );
        await repository.SaveAsync(task3);

        Console.WriteLine("[SeedData] Sample project and 3 tasks have been created successfully with timeline-based events.");
        Console.WriteLine($"[SeedData] Events span from {DateTime.UtcNow.AddDays(-60):yyyy-MM-dd} to {DateTime.UtcNow.AddDays(-34):yyyy-MM-dd}");
    }
}
