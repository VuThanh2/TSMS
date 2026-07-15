using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOpenForEnrollmentToCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOpenForEnrollment",
                schema: "course",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // BACKFILL — bắt buộc, không được bỏ. Cột mới default = false, nghĩa là "Admin chưa
            // mở đăng ký". Nhưng mọi Course có sẵn trong DB đã được coi là đang mở từ trước khi
            // có khái niệm này. Không backfill thì chúng lập tức biến mất khỏi Available Courses
            // và Student không enroll được — im lặng, không exception, không log.
            // Course tạo MỚI sau migration vẫn = false đúng theo Course.Create().
            migrationBuilder.Sql(
                "UPDATE [course].[Courses] SET [IsOpenForEnrollment] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOpenForEnrollment",
                schema: "course",
                table: "Courses");
        }
    }
}
