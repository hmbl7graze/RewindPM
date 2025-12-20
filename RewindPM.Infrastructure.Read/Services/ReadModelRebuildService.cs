using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.Persistence;

namespace RewindPM.Infrastructure.Read.Services;

/// <summary>
/// ReadModelの再構築を管理するサービス
/// </summary>
public class ReadModelRebuildService : IReadModelRebuildService
{
    private readonly ReadModelDbContext _context;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ILogger<ReadModelRebuildService> _logger;

    public ReadModelRebuildService(
        ReadModelDbContext context,
        ITimeZoneService timeZoneService,
        ILogger<ReadModelRebuildService> logger)
    {
        _context = context;
        _timeZoneService = timeZoneService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string?> GetStoredTimeZoneIdAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemMetadata
            .Where(m => m.Key == SystemMetadataEntity.TimeZoneMetadataKey)
            .Select(m => m.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CheckAndRebuildIfTimeZoneChangedAsync(CancellationToken cancellationToken = default)
    {
        var storedTimeZone = await GetStoredTimeZoneIdAsync(cancellationToken);
        var configuredTimeZone = _timeZoneService.TimeZone.Id;

        if (storedTimeZone == configuredTimeZone)
        {
            return false;
        }

        _logger.LogInformation(
            "[Startup] TimeZone changed: {StoredTimeZone} -> {ConfiguredTimeZone}",
            storedTimeZone ?? "none",
            configuredTimeZone);
        _logger.LogInformation("[Startup] Rebuilding ReadModel database...");

        var transaction = await ClearReadModelAndUpdateTimeZoneAsync(configuredTimeZone, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await transaction.DisposeAsync();

        _logger.LogInformation("[Startup] ReadModel cleared. Please re-create your data or import from EventStore.");
        return true;
    }

    /// <inheritdoc/>
    public async Task<IDbContextTransaction> ClearReadModelAndUpdateTimeZoneAsync(
        string newTimeZoneId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // ReadModelのデータをクリア(テーブル構造は維持)
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM TaskHistories", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM ProjectHistories", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Tasks", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Projects", cancellationToken);

            // タイムゾーンIDを更新
            var metadata = await _context.SystemMetadata
                .FirstOrDefaultAsync(
                    m => m.Key == SystemMetadataEntity.TimeZoneMetadataKey,
                    cancellationToken);

            if (metadata == null)
            {
                _context.SystemMetadata.Add(new SystemMetadataEntity
                {
                    Key = SystemMetadataEntity.TimeZoneMetadataKey,
                    Value = newTimeZoneId
                });
            }
            else
            {
                metadata.Value = newTimeZoneId;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return transaction;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
