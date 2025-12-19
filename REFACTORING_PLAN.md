# RewindPM リファクタリング計画書

**作成日:** 2025-12-19
**対象ブランチ:** dev-refactor
**目的:** 動作を変えずにコード品質を向上させる

---

## 調査結果サマリー

全体的にコードの品質は高いですが、開発が進んできたことで以下の改善点が見つかりました:

- **コードの重複:** Projection Event Handlersで約275行、統計計算で約150行の重複
- **長すぎるメソッド:** 最大158行のメソッドが存在
- **命名の一貫性:** 一部で省略形が使用されている
- **ドキュメントと実装の矛盾:** コメントと実装が一致していない箇所がある

---

## 優先度: 高 - 早急に対応すべき項目

### 1. Projection Event Handlersにおけるコードの大量重複 ⚠️

**問題:**
5つのTask関連Event Handlerで、スナップショット更新処理が完全に重複しています(約55行 × 5 = 275行の重複)

**対象ファイル:**
- `RewindPM.Projection/Handlers/TaskUpdatedEventHandler.cs` (行57-111)
- `RewindPM.Projection/Handlers/TaskScheduledPeriodChangedEventHandler.cs` (行58-112)
- `RewindPM.Projection/Handlers/TaskActualPeriodChangedEventHandler.cs` (行58-112)
- `RewindPM.Projection/Handlers/TaskStatusChangedEventHandler.cs` (行57-111)
- `RewindPM.Projection/Handlers/TaskCompletelyUpdatedEventHandler.cs` (行65-119)

**重複内容:**
```csharp
// 全てのハンドラーで同じロジック
private async Task UpsertTaskSnapshotAsync(TaskEntity task, DateTimeOffset occurredAt)
{
    var snapshotDate = _timeZoneService.ConvertToLocalDate(occurredAt);
    var existingSnapshot = await _context.TaskHistories
        .FirstOrDefaultAsync(th => th.TaskId == task.Id && th.SnapshotDate.Date == snapshotDate.Date);

    if (existingSnapshot != null)
    {
        // 既存スナップショットの更新（全プロパティをコピー）
        existingSnapshot.ProjectId = task.ProjectId;
        existingSnapshot.Title = task.Title;
        // ... 以下10行以上のプロパティコピー
    }
    else
    {
        // 新規スナップショットの作成
        var snapshot = new TaskHistoryEntity { /* ... */ };
        _context.TaskHistories.Add(snapshot);
    }
}
```

**修正案:**
1. 新規作成: `RewindPM.Projection/Services/TaskSnapshotService.cs`
2. `UpsertTaskSnapshotAsync` メソッドを共通化
3. 各Event Handlerはこのサービスを注入して使用

**実装例:**
```csharp
// RewindPM.Projection/Services/TaskSnapshotService.cs
public class TaskSnapshotService
{
    private readonly ReadModelDbContext _context;
    private readonly ITimeZoneService _timeZoneService;

    public async Task UpsertTaskSnapshotAsync(TaskEntity task, DateTimeOffset occurredAt)
    {
        // 共通化されたスナップショット処理
    }
}

// Event Handlerでの使用
public class TaskUpdatedEventHandler : IEventHandler<TaskUpdated>
{
    private readonly TaskSnapshotService _snapshotService;

    public async Task Handle(TaskUpdated @event, CancellationToken cancellationToken)
    {
        // タスク本体の更新
        var task = await _context.Tasks.FindAsync(@event.AggregateId);
        task.Title = @event.Title;

        // スナップショット処理（共通化）
        await _snapshotService.UpsertTaskSnapshotAsync(task, @event.OccurredAt);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

**効果:**
- 約220行のコード削減
- 保守性の大幅な向上
- スナップショット処理の一貫性確保

**テスト要件:**
- 既存の全Projectionテストが通過すること
- 各Event Handlerのテストを実行し、スナップショット作成が正常に動作すること

---

### 2. ProjectStatisticsRepositoryにおける計算ロジックの重複 ⚠️

**問題:**
`TaskEntity`と`TaskHistoryEntity`に対する統計計算メソッドが完全に重複しています(約150行の重複)

**対象ファイル:**
- `RewindPM.Infrastructure.Read/Repositories/ProjectStatisticsRepository.cs`

**重複メソッド:**

1. **CalculateRemainingEstimatedHours**
   - 行303-312: TaskEntity用
   - 行319-328: TaskHistoryEntity用

2. **CalculateDelayStatistics**
   - 行334-360: TaskEntity用
   - 行366-392: TaskHistoryEntity用

3. **CalculateEstimateAccuracy**
   - 行398-445: TaskEntity用
   - 行451-498: TaskHistoryEntity用

**修正案:**

1. 新規インターフェース作成:
```csharp
// RewindPM.Infrastructure.Read/Contracts/ITaskStatisticsData.cs
public interface ITaskStatisticsData
{
    TaskStatus Status { get; }
    int? EstimatedHours { get; }
    int? ActualHours { get; }
    DateTimeOffset? ScheduledStartDate { get; }
    DateTimeOffset? ScheduledEndDate { get; }
    DateTimeOffset? ActualStartDate { get; }
    DateTimeOffset? ActualEndDate { get; }
}
```

2. TaskEntityとTaskHistoryEntityに実装を追加:
```csharp
// RewindPM.Infrastructure.Read/Entities/TaskEntity.cs
public partial class TaskEntity : ITaskStatisticsData
{
    // 既存のプロパティがインターフェースを満たす
}

// RewindPM.Infrastructure.Read/Entities/TaskHistoryEntity.cs
public partial class TaskHistoryEntity : ITaskStatisticsData
{
    // 既存のプロパティがインターフェースを満たす
}
```

3. ジェネリックメソッドに統合:
```csharp
// ProjectStatisticsRepository.cs
private int CalculateRemainingEstimatedHours<T>(IEnumerable<T> tasks)
    where T : ITaskStatisticsData
{
    return tasks
        .Where(t => t.Status != TaskStatus.Done)
        .Sum(t => t.EstimatedHours ?? 0);
}

private (int delayedCount, int onTimeCount, double averageDelayDays)
    CalculateDelayStatistics<T>(IEnumerable<T> tasks)
    where T : ITaskStatisticsData
{
    // 統合された実装
}

private (double overallAccuracy, double hourAccuracy)
    CalculateEstimateAccuracy<T>(IEnumerable<T> tasks)
    where T : ITaskStatisticsData
{
    // 統合された実装
}
```

**効果:**
- 約150行のコード削減
- タイプセーフティの向上
- 将来的な統計計算の拡張が容易

**テスト要件:**
- 既存の統計関連テストが全て通過すること
- 現在と過去の統計データが正しく計算されること

---

## 優先度: 中 - 計画的に対応すべき項目

### 3. 長すぎるメソッドの分割

**問題:**
158行のメソッドが存在し、循環的複雑度が高く保守が困難

**対象:**
- `RewindPM.Infrastructure.Read/Repositories/ProjectStatisticsRepository.cs`
  - `GetProjectStatisticsDetailAsync` (行67-225) - 158行

**現在の責務:**
1. TaskHistoriesからのデータ取得
2. フォールバック処理（Tasksテーブルへの移行）
3. タスク数のカウント
4. 工数統計の計算
5. スケジュール統計の計算
6. 見積もり精度の計算

**修正案:**

以下のヘルパーメソッドに分割:

```csharp
// 1. データ取得部分
private async Task<IEnumerable<ITaskStatisticsData>> GetTasksDataAsync(
    Guid projectId, DateTimeOffset? asOfDate)
{
    // TaskHistories または Tasks からデータを取得
}

// 2. タスク数カウント
private (int totalTasks, int completedTasks, int inProgressTasks, int todoTasks, int reviewTasks)
    CalculateTaskCountStatistics(IEnumerable<ITaskStatisticsData> tasks)
{
    // タスク数の集計
}

// 3. 工数統計
private (int totalEstimatedHours, int totalActualHours, int remainingEstimatedHours)
    CalculateHoursStatistics(IEnumerable<ITaskStatisticsData> tasks)
{
    // 工数の集計
}

// 4. スケジュール統計
private (DateTime? earliestStartDate, DateTime? latestEndDate, int delayedTasksCount,
         int onTimeTasksCount, double averageDelayDays)
    CalculateScheduleStatistics(IEnumerable<ITaskStatisticsData> tasks)
{
    // スケジュール関連の統計
}

// 5. メインメソッド（大幅に簡潔化）
public async Task<ProjectStatisticsDetailDto> GetProjectStatisticsDetailAsync(
    Guid projectId, DateTimeOffset? asOfDate = null)
{
    var tasks = await GetTasksDataAsync(projectId, asOfDate);
    var taskCounts = CalculateTaskCountStatistics(tasks);
    var hours = CalculateHoursStatistics(tasks);
    var schedule = CalculateScheduleStatistics(tasks);
    var accuracy = CalculateEstimateAccuracy(tasks);

    return new ProjectStatisticsDetailDto
    {
        // DTOの構築
    };
}
```

**効果:**
- 各メソッドが20-30行以内に収まる
- 可読性と保守性の大幅な向上
- 単体テストが容易になる
- 将来的な統計項目の追加が簡単

**テスト要件:**
- 既存のテストが全て通過すること
- リファクタリング前後で出力結果が完全に一致すること

---

### 4. バリデーションロジックの重複

**問題:**
期間のバリデーションロジックが3つのValidatorで重複

**対象ファイル:**
- `RewindPM.Application.Write/Validators/Tasks/CreateTaskCommandValidator.cs` (行32-35, 43-46)
- `RewindPM.Application.Write/Validators/Tasks/ChangeTaskScheduleCommandValidator.cs` (行17-19)
- `RewindPM.Application.Write/Validators/Tasks/ChangeTaskActualPeriodCommandValidator.cs` (行18-21)

**重複例:**
```csharp
// CreateTaskCommandValidator
RuleFor(x => x)
    .Must(x => !x.ScheduledStartDate.HasValue || !x.ScheduledEndDate.HasValue ||
               x.ScheduledEndDate.Value > x.ScheduledStartDate.Value)
    .WithMessage("予定終了日は予定開始日より後でなければなりません");

// ChangeTaskScheduleCommandValidator
RuleFor(x => x.ScheduledEndDate)
    .GreaterThan(x => x.ScheduledStartDate)
    .WithMessage("予定終了日は予定開始日より後でなければなりません");
```

**修正案:**

新規作成: `RewindPM.Application.Write/Validators/Common/PeriodValidationRules.cs`

```csharp
public static class PeriodValidationRules
{
    public static IRuleBuilderOptions<T, DateTimeOffset?> EndDateMustBeAfterStartDate<T>(
        this IRuleBuilder<T, DateTimeOffset?> ruleBuilder,
        Func<T, DateTimeOffset?> startDateSelector)
    {
        return ruleBuilder
            .Must((model, endDate) =>
            {
                var startDate = startDateSelector(model);
                return !startDate.HasValue || !endDate.HasValue || endDate.Value > startDate.Value;
            })
            .WithMessage("終了日は開始日より後でなければなりません");
    }

    public static IRuleBuilderOptions<T, int?> HoursMustBePositive<T>(
        this IRuleBuilder<T, int?> ruleBuilder)
    {
        return ruleBuilder
            .Must(hours => !hours.HasValue || hours.Value > 0)
            .WithMessage("工数は正の数でなければなりません");
    }
}
```

**使用例:**
```csharp
// CreateTaskCommandValidator
RuleFor(x => x.ScheduledEndDate)
    .EndDateMustBeAfterStartDate(x => x.ScheduledStartDate);

RuleFor(x => x.EstimatedHours)
    .HoursMustBePositive();
```

**効果:**
- DRY原則の徹底
- バリデーションルールの一貫性向上
- バリデーションメッセージの統一管理

**テスト要件:**
- 既存のValidatorテストが全て通過すること

---

### 5. DeleteProjectCommandHandlerの矛盾解消

**問題:**
コメントと実装が矛盾しています

**対象:**
- `RewindPM.Application.Write/CommandHandlers/Projects/DeleteProjectCommandHandler.cs` (行40-51)

**現在のコード:**
```csharp
// 各タスクを削除（並列処理はせず順次処理でトランザクションの整合性を保つ）
var deleteTasks = taskIds.Select(async taskId =>
{
    var task = await _repository.GetByIdAsync<TaskAggregate>(taskId);
    if (task != null)
    {
        task.Delete(request.DeletedBy, _dateTimeProvider);
        await _repository.SaveAsync(task);
    }
});

// すべてのタスク削除を実行
await Task.WhenAll(deleteTasks); // ← 実際には並列実行されている
```

**修正案A (推奨): 順次処理に変更**
```csharp
// 各タスクを削除（イベントの順序保証のため順次処理）
foreach (var taskId in taskIds)
{
    var task = await _repository.GetByIdAsync<TaskAggregate>(taskId);
    if (task != null)
    {
        task.Delete(request.DeletedBy, _dateTimeProvider);
        await _repository.SaveAsync(task);
    }
}
```

**修正案B: 並列処理を維持しコメントを修正**
```csharp
// 各タスクを並列削除（パフォーマンス優先、イベント順序は保証されない）
var deleteTasks = taskIds.Select(async taskId => { /* ... */ });
await Task.WhenAll(deleteTasks);
```

**推奨:** 案A（イベントソーシングの順序保証のため）

**効果:**
- コードとドキュメントの整合性確保
- イベントソーシングにおける予測可能な動作

**テスト要件:**
- プロジェクト削除テストが通過すること
- カスケード削除が正しく動作すること

---

## 優先度: 低 - 時間があれば対応

### 6. 不使用コードの削除

**対象:**
- `RewindPM.Domain.Test/UnitTest1.cs` - 標準的でないファイル名

**修正案:**
1. ファイルの内容を確認
2. 使用されていない場合は削除
3. 使用されている場合は適切な名前にリネーム

**効果:**
- コードベースのクリーンアップ
- 混乱の防止

---

### 7. 命名の一貫性改善

**問題:**
一部で短い変数名が使用されています

**対象:**
- `RewindPM.Infrastructure.Read/Repositories/ReadModelRepository.cs`
  - `th` → `taskHistory`
  - `h` → `history`
  - `ph` → `projectHistory`

**修正例:**
```csharp
// Before
var th = await _context.TaskHistories
    .FirstOrDefaultAsync(h => h.TaskId == taskId && h.SnapshotDate.Date <= targetDate);

// After
var taskHistory = await _context.TaskHistories
    .FirstOrDefaultAsync(history =>
        history.TaskId == taskId &&
        history.SnapshotDate.Date <= targetDate);
```

**効果:**
- コードの可読性向上
- IDE の補完機能が活用しやすくなる

---

## リファクタリング実施計画

### フェーズ1: 重複コードの排除（優先度: 高）

**作業内容:**
1. TaskSnapshotService の作成
2. Event Handlersの修正
3. ITaskStatisticsData インターフェースの作成
4. 統計計算メソッドの統合

**予想作業時間:** 4-6時間

**テスト要件:**
- 既存の全Projectionテストが通過すること
- 既存の全統計テストが通過すること
- リファクタリング前後で出力結果が完全に一致すること

**ブランチ戦略:**
- `refactor/duplicate-code-removal` ブランチで作業
- レビュー後 `dev-refactor` にマージ

---

### フェーズ2: メソッド分割と改善（優先度: 中）

**作業内容:**
1. GetProjectStatisticsDetailAsync の分割
2. バリデーションルールの共通化
3. DeleteProjectCommandHandler の矛盾解消

**予想作業時間:** 3-4時間

**テスト要件:**
- Application層のテストが全て通過すること
- Infrastructure.Read層のテストが全て通過すること
- 統合テストが通過すること

**ブランチ戦略:**
- `refactor/method-simplification` ブランチで作業
- レビュー後 `dev-refactor` にマージ

---

### フェーズ3: クリーンアップ（優先度: 低）

**作業内容:**
1. 不使用コードの削除
2. 命名の改善

**予想作業時間:** 1-2時間

**テスト要件:**
- 全テストが通過すること

**ブランチ戦略:**
- `refactor/cleanup` ブランチで作業
- レビュー後 `dev-refactor` にマージ

---

## 全体スケジュール

| フェーズ | 作業時間 | 開始予定 | 完了予定 |
|---------|---------|---------|---------|
| フェーズ1 | 4-6時間 | TBD | TBD |
| フェーズ2 | 3-4時間 | TBD | TBD |
| フェーズ3 | 1-2時間 | TBD | TBD |
| **合計** | **8-12時間** | - | - |

---

## リスク管理

### リスク1: イベントソーシングの動作変更

**リスクレベル:** 高

**対策:**
- Event Handlerの修正は慎重に実施
- 各修正後に全テストを実行
- 動作変更が疑われる場合はロールバック

### リスク2: 統計計算の結果変更

**リスクレベル:** 中

**対策:**
- リファクタリング前後で統計データのスナップショットを取得
- 差分がないことを確認してからマージ

### リスク3: パフォーマンスの低下

**リスクレベル:** 低

**対策:**
- 大きな変更の場合はパフォーマンステストを実施
- 問題があれば最適化を検討

---

## 注意事項

1. **動作を変えない**: リファクタリングは動作を変更せず、コード構造のみを改善する
2. **テストファースト**: 各修正後、必ず全テストを実行してリグレッションがないことを確認
3. **小さなコミット**: 意味のある単位で小さくコミットし、レビューしやすくする
4. **ドキュメント更新**: 必要に応じてREADME.mdや他のドキュメントも更新

---

## 成果物チェックリスト

### フェーズ1完了時
- [ ] TaskSnapshotServiceが作成されている
- [ ] 5つのEvent HandlerがTaskSnapshotServiceを使用している
- [ ] ITaskStatisticsDataインターフェースが作成されている
- [ ] 統計計算メソッドがジェネリック化されている
- [ ] 全テストが通過している
- [ ] コードレビューが完了している

### フェーズ2完了時
- [ ] GetProjectStatisticsDetailAsyncが小さなメソッドに分割されている
- [ ] 共通バリデーションルールが作成されている
- [ ] DeleteProjectCommandHandlerの矛盾が解消されている
- [ ] 全テストが通過している
- [ ] コードレビューが完了している

### フェーズ3完了時
- [ ] 不使用コードが削除されている
- [ ] 命名が改善されている
- [ ] 全テストが通過している
- [ ] コードレビューが完了している

### 最終確認
- [ ] 全フェーズのマージが完了している
- [ ] dev-refactorブランチで全テストが通過している
- [ ] ビルドが成功している
- [ ] リファクタリング前後の動作が同一であることを確認している

---

## 参考資料

- [README.md](README.md) - プロジェクト概要
- [CLAUDE.md](CLAUDE.md) - 開発指針
- C# コーディング規約: https://docs.microsoft.com/ja-jp/dotnet/csharp/fundamentals/coding-style/coding-conventions
- リファクタリング: https://refactoring.com/

---

**承認者:** _________________
**承認日:** _________________
