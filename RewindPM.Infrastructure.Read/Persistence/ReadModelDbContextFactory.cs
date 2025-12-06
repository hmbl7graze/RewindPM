using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RewindPM.Infrastructure.Read.Persistence;

/// <summary>
/// デザイン時のDbContextファクトリ
/// マイグレーション作成時に使用される
/// </summary>
public class ReadModelDbContextFactory : IDesignTimeDbContextFactory<ReadModelDbContext>
{
    public ReadModelDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReadModelDbContext>();

        // デザイン時の接続文字列（マイグレーション作成用）
        optionsBuilder.UseSqlite("Data Source=readmodel_design.db");

        return new ReadModelDbContext(optionsBuilder.Options);
    }
}
