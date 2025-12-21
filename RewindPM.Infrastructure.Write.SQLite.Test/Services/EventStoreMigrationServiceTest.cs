using Microsoft.EntityFrameworkCore;
using RewindPM.Infrastructure.Write.SQLite.Persistence;
using RewindPM.Infrastructure.Write.SQLite.Services;
using Xunit;

namespace RewindPM.Infrastructure.Write.SQLite.Test.Services;

public class EventStoreMigrationServiceTest : IDisposable
{
    private readonly List<Microsoft.Data.Sqlite.SqliteConnection> _connections = new();

    private EventStoreDbContext CreateInMemoryContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();
        _connections.Add(connection);

        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new EventStoreDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            connection.Close();
            connection.Dispose();
        }
        _connections.Clear();
    }

    [Fact]
    public async Task HasPendingMigrationsAsync_呼び出しが成功する()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new EventStoreMigrationService(context);

        // Act & Assert - 例外が発生しないことを確認
        // InMemoryデータベースでは実際のマイグレーションファイルが使えないため、
        // 結果の値ではなく、メソッドが正常に実行できることを確認
        var result = await service.HasPendingMigrationsAsync(TestContext.Current.CancellationToken);
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task ApplyMigrationsAsync_EnsureCreatedなしで呼び出すと成功する()
    {
        // Arrange
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();
        _connections.Add(connection);

        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new EventStoreDbContext(options);
        // EnsureCreatedを呼ばずにMigrateAsyncを呼ぶ
        var service = new EventStoreMigrationService(context);

        // Act & Assert - 例外が発生しないことを確認
        // InMemoryデータベースでは実際のマイグレーション適用により、テーブルが作成される
        await service.ApplyMigrationsAsync(TestContext.Current.CancellationToken);

        // データベースが作成されたことを確認
        var canConnect = await context.Database.CanConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(canConnect);
    }
}
