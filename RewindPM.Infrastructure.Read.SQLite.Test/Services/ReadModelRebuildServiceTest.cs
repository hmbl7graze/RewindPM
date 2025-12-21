using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.SQLite.Entities;
using RewindPM.Infrastructure.Read.SQLite.Persistence;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Infrastructure.Read.SQLite.Services;
using Xunit;

namespace RewindPM.Infrastructure.Read.SQLite.Test.Services;

/// <summary>
/// ReadModelRebuildServiceのテスト
/// </summary>
public class ReadModelRebuildServiceTest : IDisposable
{
    private readonly ITimeZoneService _mockTimeZoneService;
    private readonly ILogger<ReadModelRebuildService> _mockLogger;
    private readonly List<Microsoft.Data.Sqlite.SqliteConnection> _connections = new();

    public ReadModelRebuildServiceTest()
    {
        _mockTimeZoneService = Substitute.For<ITimeZoneService>();
        _mockLogger = Substitute.For<ILogger<ReadModelRebuildService>>();
    }

    private ReadModelDbContext CreateInMemoryContext()
    {
        // SQLiteのInMemoryモードを使用(生SQLとトランザクションに対応)
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();
        _connections.Add(connection); // Dispose用に記録

        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ReadModelDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        // すべての接続をクローズして破棄
        foreach (var connection in _connections)
        {
            connection.Close();
            connection.Dispose();
        }
        _connections.Clear();
    }

    [Fact]
    public async Task GetStoredTimeZoneIdAsync_メタデータが存在しない場合_Nullを返す()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new ReadModelRebuildService(context, _mockTimeZoneService, _mockLogger);

        // Act
        var result = await service.GetStoredTimeZoneIdAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStoredTimeZoneIdAsync_メタデータが存在する場合_タイムゾーンIDを返す()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        context.SystemMetadata.Add(new SystemMetadataEntity
        {
            Key = SystemMetadataEntity.TimeZoneMetadataKey,
            Value = "Asia/Tokyo"
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new ReadModelRebuildService(context, _mockTimeZoneService, _mockLogger);

        // Act
        var result = await service.GetStoredTimeZoneIdAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Asia/Tokyo", result);
    }

    [Fact]
    public async Task CheckAndRebuildIfTimeZoneChangedAsync_タイムゾーンが変更されていない場合_Falseを返す()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
        _mockTimeZoneService.TimeZone.Returns(mockTimeZoneInfo);

        context.SystemMetadata.Add(new SystemMetadataEntity
        {
            Key = SystemMetadataEntity.TimeZoneMetadataKey,
            Value = "Asia/Tokyo"
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new ReadModelRebuildService(context, _mockTimeZoneService, _mockLogger);

        // Act
        var result = await service.CheckAndRebuildIfTimeZoneChangedAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckAndRebuildIfTimeZoneChangedAsync_タイムゾーンが変更された場合_Trueを返しデータをクリアする()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("UTC");
        _mockTimeZoneService.TimeZone.Returns(mockTimeZoneInfo);

        context.SystemMetadata.Add(new SystemMetadataEntity
        {
            Key = SystemMetadataEntity.TimeZoneMetadataKey,
            Value = "Asia/Tokyo"
        });

        // テストデータを追加
        context.Projects.Add(new ProjectEntity
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test"
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new ReadModelRebuildService(context, _mockTimeZoneService, _mockLogger);

        // Act
        var result = await service.CheckAndRebuildIfTimeZoneChangedAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        // データがクリアされているか確認
        var projectCount = await context.Projects.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, projectCount);

        // タイムゾーンIDが更新されているか確認
        var storedTimeZone = await service.GetStoredTimeZoneIdAsync(TestContext.Current.CancellationToken);
        Assert.Equal("UTC", storedTimeZone);
    }

    [Fact]
    public async Task ClearReadModelAndUpdateTimeZoneAsync_データをクリアしタイムゾーンを更新する()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // テストデータを追加
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test"
        });

        context.Tasks.Add(new TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "Test Task",
            Status = Domain.ValueObjects.TaskStatus.Todo,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test"
        });

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new ReadModelRebuildService(context, _mockTimeZoneService, _mockLogger);

        // Act
        await service.ClearReadModelAndUpdateTimeZoneAsync("UTC", TestContext.Current.CancellationToken);

        // Assert
        // データがクリアされているか確認
        var projectCount = await context.Projects.CountAsync(TestContext.Current.CancellationToken);
        var taskCount = await context.Tasks.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, projectCount);
        Assert.Equal(0, taskCount);

        // タイムゾーンIDが更新されているか確認
        var storedTimeZone = await service.GetStoredTimeZoneIdAsync(TestContext.Current.CancellationToken);
        Assert.Equal("UTC", storedTimeZone);
    }

    [Fact]
    public async Task ClearReadModelAndUpdateTimeZoneAsync_メタデータが存在しない場合_新規作成する()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new ReadModelRebuildService(context, _mockTimeZoneService, _mockLogger);

        // Act
        await service.ClearReadModelAndUpdateTimeZoneAsync("UTC", TestContext.Current.CancellationToken);

        // Assert
        var storedTimeZone = await service.GetStoredTimeZoneIdAsync(TestContext.Current.CancellationToken);
        Assert.Equal("UTC", storedTimeZone);
    }

    [Fact]
    public async Task InitializeTimeZoneMetadataAsync_タイムゾーンメタデータを初期化する()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
        _mockTimeZoneService.TimeZone.Returns(mockTimeZoneInfo);
        var service = new ReadModelRebuildService(context, _mockTimeZoneService, _mockLogger);

        // Act
        await service.InitializeTimeZoneMetadataAsync(TestContext.Current.CancellationToken);

        // Assert
        var storedTimeZone = await service.GetStoredTimeZoneIdAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Asia/Tokyo", storedTimeZone);
    }
}
