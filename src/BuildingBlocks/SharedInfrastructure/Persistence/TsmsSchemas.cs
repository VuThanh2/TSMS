namespace SharedInfrastructure.Persistence;

// Tên schema cố định cho từng Bounded Context trong DB TSMS gộp chung.
// Dùng thống nhất ở DI registration, IDesignTimeDbContextFactory và TSMS.DbMigrator.
public static class TsmsSchemas {
    public const string Identity = "identity";
    public const string Course = "course";
    public const string Enrollment = "enrollment";
    public const string Reporting = "reporting";
}