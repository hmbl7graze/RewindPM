using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Infrastructure.Read.SQLite.Entities;
using RewindPM.Infrastructure.Read.SQLite.Persistence;

namespace RewindPM.Infrastructure.Read.SQLite.Services;

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

        await ClearReadModelAndUpdateTimeZoneAsync(configuredTimeZone, cancellationToken);

        _logger.LogInformation("[Startup] ReadModel cleared. Please re-create your data or import from EventStore.");
        return true;
    }

    /// <inheritdoc/>
    public async Task ClearReadModelAndUpdateTimeZoneAsync(
        string newTimeZoneId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // ReadModelのデータをクリア(テーブル構造は維持)
            await _context.TaskHistories.ExecuteDeleteAsync(cancellationToken);
            await _context.ProjectHistories.ExecuteDeleteAsync(cancellationToken);
            await _context.Tasks.ExecuteDeleteAsync(cancellationToken);
            await _context.Projects.ExecuteDeleteAsync(cancellationToken);

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
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Failed to clear read model and update time zone to {NewTimeZoneId}. Transaction has been rolled back.",
                newTimeZoneId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task InitializeTimeZoneMetadataAsync(CancellationToken cancellationToken = default)
    {
        var currentTimeZone = _timeZoneService.TimeZone.Id;
        
        // トランザクション内でメタデータを初期化
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // メタデータのみ追加（データはクリアしない）
                var metadataEntity = new SystemMetadataEntity
                {
                    Key = SystemMetadataEntity.TimeZoneMetadataKey,
                    Value = currentTimeZone
                };

                _context.SystemMetadata.Add(metadataEntity);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                
                _logger.LogInformation("Initialized timezone metadata: {TimeZoneId}", currentTimeZone);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    ex,
                    "Failed to initialize timezone metadata with {TimeZoneId}. Transaction has been rolled back.",
                    currentTimeZone);
                throw;
            }
        });
    }
}
