using Microsoft.EntityFrameworkCore;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Infrastructure.Read.SQLite.Persistence;

namespace RewindPM.Infrastructure.Read.SQLite.Services;

/// <summary>
/// ReadModelデータベースのマイグレーション管理サービスの実装
/// </summary>
public class ReadModelMigrationService : IReadModelMigrationService
{
    private readonly ReadModelDbContext _context;

    public ReadModelMigrationService(ReadModelDbContext context)
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

    /// <inheritdoc/>
    public async Task<bool> IsEmptyAsync(CancellationToken cancellationToken = default)
    {
        return !await _context.Projects.AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void ClearChangeTracking()
    {
        _context.ChangeTracker.Clear();
    }
}
