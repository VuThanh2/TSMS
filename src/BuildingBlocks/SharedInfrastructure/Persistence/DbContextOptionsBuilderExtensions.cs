using Microsoft.EntityFrameworkCore;

namespace SharedInfrastructure.Persistence;

// Cấu hình UseSqlServer thống nhất cho cả 4 DbContext khi share chung 1 Database.
// Mỗi Context cần MigrationsHistoryTable riêng theo schema của mình, nếu không EF Core
// sẽ coi cả 4 Context dùng chung 1 bảng lịch sử migration (mặc định __EFMigrationsHistory ở dbo)
// và bị nhầm giữa các model snapshot khác nhau.
public static class DbContextOptionsBuilderExtensions {
    private const string MigrationsHistoryTableName = "__EFMigrationsHistory";

    // Dùng trong AddDbContext(...) delegate và IDesignTimeDbContextFactory — không cần chain .Options.
    public static DbContextOptionsBuilder UseTsmsSqlServer(
        this DbContextOptionsBuilder builder,
        string connectionString,
        string schema) {
        builder.UseSqlServer(connectionString, sql =>
            sql.MigrationsHistoryTable(MigrationsHistoryTableName, schema));
        return builder;
    }

    // dùng trong TSMS.DbMigrator nơi cần build DbContextOptions<TContext> thủ công (không qua DI).
    public static DbContextOptionsBuilder<TContext> UseTsmsSqlServer<TContext>(
        this DbContextOptionsBuilder<TContext> builder,
        string connectionString,
        string schema) where TContext : DbContext {
        ((DbContextOptionsBuilder)builder).UseTsmsSqlServer(connectionString, schema);
        return builder;
    }
} 