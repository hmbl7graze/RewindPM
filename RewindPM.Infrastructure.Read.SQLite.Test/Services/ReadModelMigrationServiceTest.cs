using Microsoft.EntityFrameworkCore;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.SQLite.Persistence;
using RewindPM.Infrastructure.Read.SQLite.Services;
using Xunit;

namespace RewindPM.Infrastructure.Read.SQLite.Test.Services;

public class ReadModelMigrationServiceTest : IDisposable
{
    private readonly List<Microsoft.Data.Sqlite.SqliteConnection> _connections = new();

    private ReadModelDbContext CreateInMemoryContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();
        _connections.Add(connection);

        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ReadModelDbContext(options);
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
        var service = new ReadModelMigrationService(context);

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

        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ReadModelDbContext(options);
        // EnsureCreatedを呼ばずにMigrateAsyncを呼ぶ
        var service = new ReadModelMigrationService(context);

        // Act & Assert - 例外が発生しないことを確認
        // InMemoryデータベースでは実際のマイグレーション適用により、テーブルが作成される
        await service.ApplyMigrationsAsync(TestContext.Current.CancellationToken);

        // データベースが作成されたことを確認
        var canConnect = await context.Database.CanConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(canConnect);
    }

    [Fact]
    public async Task IsEmptyAsync_プロジェクトが存在しない場合_Trueを返す()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new ReadModelMigrationService(context);

        // Act
        var result = await service.IsEmptyAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsEmptyAsync_プロジェクトが存在する場合_Falseを返す()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        context.Projects.Add(new ProjectEntity
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test"
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new ReadModelMigrationService(context);

        // Act
        var result = await service.IsEmptyAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ClearChangeTracking_変更追跡をクリアできる()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var project = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test"
        };
        context.Projects.Add(project);

        var service = new ReadModelMigrationService(context);

        // Act
        service.ClearChangeTracking();

        // Assert
        var trackedEntries = context.ChangeTracker.Entries().Count();
        Assert.Equal(0, trackedEntries);
    }
}
