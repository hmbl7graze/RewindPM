using Microsoft.EntityFrameworkCore;
using RewindPM.Infrastructure.Read.SQLite.Entities;

namespace RewindPM.Infrastructure.Read.SQLite.Persistence;

/// <summary>
/// ReadModel用のDbContext
/// 現在の状態と過去の状態（タイムトラベル用）を管理
/// </summary>
public class ReadModelDbContext : DbContext
{
    /// <summary>
    /// プロジェクト（現在の状態）
    /// </summary>
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();

    /// <summary>
    /// タスク（現在の状態）
    /// </summary>
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    /// <summary>
    /// プロジェクトの履歴（過去の状態）
    /// </summary>
    public DbSet<ProjectHistoryEntity> ProjectHistories => Set<ProjectHistoryEntity>();

    /// <summary>
    /// タスクの履歴（過去の状態）
    /// </summary>
    public DbSet<TaskHistoryEntity> TaskHistories => Set<TaskHistoryEntity>();

    /// <summary>
    /// システムメタデータ（設定情報の保存）
    /// </summary>
    public DbSet<SystemMetadataEntity> SystemMetadata => Set<SystemMetadataEntity>();

    public ReadModelDbContext(DbContextOptions<ReadModelDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ProjectEntity の設定
        modelBuilder.Entity<ProjectEntity>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
        });

        // TaskEntity の設定
        modelBuilder.Entity<TaskEntity>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired();

            // 予定期間と工数
            entity.Property(e => e.ScheduledStartDate);
            entity.Property(e => e.ScheduledEndDate);
            entity.Property(e => e.EstimatedHours);

            // 実績期間と工数
            entity.Property(e => e.ActualStartDate);
            entity.Property(e => e.ActualEndDate);
            entity.Property(e => e.ActualHours);

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);

            // インデックス: ProjectIdでの検索を高速化
            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("IX_Tasks_ProjectId");
        });

        // ProjectHistoryEntity の設定
        modelBuilder.Entity<ProjectHistoryEntity>(entity =>
        {
            entity.ToTable("ProjectHistories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.SnapshotDate).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.Property(e => e.SnapshotCreatedAt).IsRequired();

            // インデックス: ProjectIdでの検索を高速化
            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("IX_ProjectHistories_ProjectId");

            // インデックス: SnapshotDateでの検索を高速化（タイムトラベル機能）
            entity.HasIndex(e => e.SnapshotDate)
                .HasDatabaseName("IX_ProjectHistories_SnapshotDate");

            // 複合ユニークインデックス: 同じプロジェクトの同じ日付のスナップショットは1つまで
            entity.HasIndex(e => new { e.ProjectId, e.SnapshotDate })
                .IsUnique()
                .HasDatabaseName("IX_ProjectHistories_ProjectId_SnapshotDate");
        });

        // TaskHistoryEntity の設定
        modelBuilder.Entity<TaskHistoryEntity>(entity =>
        {
            entity.ToTable("TaskHistories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.TaskId).IsRequired();
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.SnapshotDate).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired();

            // 予定期間と工数
            entity.Property(e => e.ScheduledStartDate);
            entity.Property(e => e.ScheduledEndDate);
            entity.Property(e => e.EstimatedHours);

            // 実績期間と工数
            entity.Property(e => e.ActualStartDate);
            entity.Property(e => e.ActualEndDate);
            entity.Property(e => e.ActualHours);

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.Property(e => e.SnapshotCreatedAt).IsRequired();

            // インデックス: TaskIdでの検索を高速化
            entity.HasIndex(e => e.TaskId)
                .HasDatabaseName("IX_TaskHistories_TaskId");

            // インデックス: ProjectIdでの検索を高速化（プロジェクトの全タスク履歴取得）
            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("IX_TaskHistories_ProjectId");

            // インデックス: SnapshotDateでの検索を高速化（タイムトラベル機能）
            entity.HasIndex(e => e.SnapshotDate)
                .HasDatabaseName("IX_TaskHistories_SnapshotDate");

            // 複合ユニークインデックス: 同じタスクの同じ日付のスナップショットは1つまで
            entity.HasIndex(e => new { e.TaskId, e.SnapshotDate })
                .IsUnique()
                .HasDatabaseName("IX_TaskHistories_TaskId_SnapshotDate");

            // 複合インデックス: プロジェクトの特定日付の全タスク取得を高速化
            entity.HasIndex(e => new { e.ProjectId, e.SnapshotDate })
                .HasDatabaseName("IX_TaskHistories_ProjectId_SnapshotDate");
        });

        // SystemMetadataEntity の設定
        modelBuilder.Entity<SystemMetadataEntity>(entity =>
        {
            entity.ToTable("SystemMetadata");
            entity.HasKey(e => e.Key);

            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(500);
        });
    }
}
