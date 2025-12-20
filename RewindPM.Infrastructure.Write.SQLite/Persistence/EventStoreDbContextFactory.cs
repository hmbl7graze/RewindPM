using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RewindPM.Infrastructure.Write.SQLite.Persistence;

/// <summary>
/// EF Core Migrationsのデザインタイム用ファクトリ
/// マイグレーション作成時にDbContextを構築するために使用
/// </summary>
public class EventStoreDbContextFactory : IDesignTimeDbContextFactory<EventStoreDbContext>
{
    public EventStoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();

        // デザインタイム用のSQLite接続文字列
        // 実際の接続文字列はDIコンテナで設定される
        optionsBuilder.UseSqlite("Data Source=eventstore.db");

        return new EventStoreDbContext(optionsBuilder.Options);
    }
}
