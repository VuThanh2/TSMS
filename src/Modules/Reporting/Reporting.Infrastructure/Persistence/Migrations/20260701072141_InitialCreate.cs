using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "CourseAttendanceReports",
                schema: "reporting",
                columns: table => new
                {
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StudentEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TotalSessions = table.Column<int>(type: "int", nullable: false),
                    PresentCount = table.Column<int>(type: "int", nullable: false),
                    ExcusedCount = table.Column<int>(type: "int", nullable: false),
                    AbsentCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseAttendanceReports", x => x.EnrollmentId);
                });

            migrationBuilder.CreateTable(
                name: "CourseScoreDistributions",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScoreGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RangeStart = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    RangeEnd = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    StudentCount = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(6,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseScoreDistributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseStatistics",
                schema: "reporting",
                columns: table => new
                {
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LecturerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LecturerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EnrolledCount = table.Column<int>(type: "int", nullable: false),
                    AverageScore = table.Column<decimal>(type: "decimal(4,2)", nullable: true),
                    GradedStudentCount = table.Column<int>(type: "int", nullable: false),
                    UngradedStudentCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseStatistics", x => x.CourseId);
                });

            migrationBuilder.CreateTable(
                name: "StudentGradeReports",
                schema: "reporting",
                columns: table => new
                {
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StudentEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Grade = table.Column<decimal>(type: "decimal(4,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGradeReports", x => x.EnrollmentId);
                });

            migrationBuilder.CreateTable(
                name: "StudentPersonalSummaries",
                schema: "reporting",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Grade = table.Column<decimal>(type: "decimal(4,2)", nullable: true),
                    TotalSessions = table.Column<int>(type: "int", nullable: false),
                    PresentCount = table.Column<int>(type: "int", nullable: false),
                    ExcusedCount = table.Column<int>(type: "int", nullable: false),
                    AbsentCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPersonalSummaries", x => new { x.StudentId, x.CourseId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseAttendanceReports_CourseId",
                schema: "reporting",
                table: "CourseAttendanceReports",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseAttendanceReports_StudentId_CourseId",
                schema: "reporting",
                table: "CourseAttendanceReports",
                columns: new[] { "StudentId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseScoreDistributions_CourseId_ScoreGroup",
                schema: "reporting",
                table: "CourseScoreDistributions",
                columns: new[] { "CourseId", "ScoreGroup" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseStatistics_LecturerId",
                schema: "reporting",
                table: "CourseStatistics",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGradeReports_CourseId",
                schema: "reporting",
                table: "StudentGradeReports",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPersonalSummaries_StudentId",
                schema: "reporting",
                table: "StudentPersonalSummaries",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseAttendanceReports",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "CourseScoreDistributions",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "CourseStatistics",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "StudentGradeReports",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "StudentPersonalSummaries",
                schema: "reporting");
        }
    }
}
