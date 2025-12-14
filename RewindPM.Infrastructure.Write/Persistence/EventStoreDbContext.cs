using Microsoft.EntityFrameworkCore;
using RewindPM.Infrastructure.Write.Entities;

namespace RewindPM.Infrastructure.Write.Persistence;

/// <summary>
/// イベントストア用のDbContext
/// イベントソーシングのためのイベント永続化を担当
/// </summary>
public class EventStoreDbContext : DbContext
{
    /// <summary>
    /// イベントのDbSet
    /// </summary>
    public DbSet<EventEntity> Events => Set<EventEntity>();

    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventEntity>(entity =>
        {
            // テーブル名
            entity.ToTable("Events");

            // 主キー
            entity.HasKey(e => e.EventId);

            // プロパティの設定
            entity.Property(e => e.EventId)
                .IsRequired();

            entity.Property(e => e.AggregateId)
                .IsRequired();

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.EventData)
                .IsRequired();

            entity.Property(e => e.OccurredAt)
                .IsRequired()
                .HasConversion(
                    v => v.UtcDateTime,
                    v => new DateTimeOffset(v, TimeSpan.Zero));

            entity.Property(e => e.Version)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.UtcDateTime,
                    v => new DateTimeOffset(v, TimeSpan.Zero));

            // インデックス
            // AggregateIdでの検索を高速化（特定のAggregateのイベント取得）
            entity.HasIndex(e => e.AggregateId)
                .HasDatabaseName("IX_Events_AggregateId");

            // OccurredAtでの検索を高速化（タイムトラベル機能）
            entity.HasIndex(e => e.OccurredAt)
                .HasDatabaseName("IX_Events_OccurredAt");

            // AggregateId + Versionの複合インデックス（楽観的同時実行制御の高速化）
            entity.HasIndex(e => new { e.AggregateId, e.Version })
                .IsUnique()
                .HasDatabaseName("IX_Events_AggregateId_Version");

            // EventTypeでの検索を高速化（イベント種別での検索）
            entity.HasIndex(e => e.EventType)
                .HasDatabaseName("IX_Events_EventType");
        });
    }
}
