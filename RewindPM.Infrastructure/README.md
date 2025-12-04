# RewindPM.Infrastructure

Infrastructure層の実装とDI登録方法

## Event Storeのセットアップ

Infrastructure層のサービスをDIコンテナに登録するには、`AddInfrastructure`拡張メソッドを使用します。

### 使用例

```csharp
using RewindPM.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure層のサービスを登録
builder.Services.AddInfrastructure(
    connectionString: builder.Configuration.GetConnectionString("EventStore")
        ?? "Data Source=eventstore.db"
);

var app = builder.Build();

// マイグレーションの適用（開発環境のみ推奨）
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
```

### appsettings.jsonの設定例

```json
{
  "ConnectionStrings": {
    "EventStore": "Data Source=eventstore.db"
  }
}
```

### 登録されるサービス

- `EventStoreDbContext`: EF CoreのDbContext（スコープド）
- `DomainEventSerializer`: イベントのシリアライザー（シングルトン）
- `IEventStore`: イベントストアの実装としてSqliteEventStore（スコープド）

### マイグレーション

```bash
# マイグレーションの作成
dotnet ef migrations add MigrationName --project RewindPM.Infrastructure

# マイグレーションの適用
dotnet ef database update --project RewindPM.Infrastructure
```
