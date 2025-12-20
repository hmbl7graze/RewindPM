using Microsoft.EntityFrameworkCore;
using RewindPM.Infrastructure.Write.Services;
using RewindPM.Infrastructure.Write.SQLite.Persistence;

namespace RewindPM.Infrastructure.Write.SQLite.Services;

/// <summary>
/// EventStoreデータベースのマイグレーション管理サービスの実装
/// </summary>
public class EventStoreMigrationService : IEventStoreMigrationService
{
    private readonly EventStoreDbContext _context;

    public EventStoreMigrationService(EventStoreDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<bool> HasPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
        return pendingMigrations.Any();
    }

    /// <inheritdoc/>
    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);
    }
}
