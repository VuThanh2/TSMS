using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnrollmentManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "enrollment");

            migrationBuilder.CreateTable(
                name: "Attendances",
                schema: "enrollment",
                columns: table => new
                {
                    AttendanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.AttendanceId);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                schema: "enrollment",
                columns: table => new
                {
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Grade = table.Column<decimal>(type: "decimal(4,2)", nullable: true),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.EnrollmentId);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnrolledSessions",
                schema: "enrollment",
                columns: table => new
                {
                    EnrolledSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeeklySlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrolledSessions", x => x.EnrolledSessionId);
                    table.ForeignKey(
                        name: "FK_EnrolledSessions_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalSchema: "enrollment",
                        principalTable: "Enrollments",
                        principalColumn: "EnrollmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ClassSessionId",
                schema: "enrollment",
                table: "Attendances",
                column: "ClassSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId_ClassSessionId",
                schema: "enrollment",
                table: "Attendances",
                columns: new[] { "StudentId", "ClassSessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrolledSessions_EnrollmentId_WeeklySlotId",
                schema: "enrollment",
                table: "EnrolledSessions",
                columns: new[] { "EnrollmentId", "WeeklySlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_CourseId",
                schema: "enrollment",
                table: "Enrollments",
                columns: new[] { "StudentId", "CourseId" },
                unique: true);
            
            migrationBuilder.Sql(@"
                ALTER TABLE [enrollment].[Enrollments]
                ADD CONSTRAINT [FK_Enrollments_AspNetUsers_StudentId]
                FOREIGN KEY ([StudentId]) REFERENCES [identity].[AspNetUsers]([Id])
                ON DELETE NO ACTION;
             
                ALTER TABLE [enrollment].[Enrollments]
                ADD CONSTRAINT [FK_Enrollments_Courses_CourseId]
                FOREIGN KEY ([CourseId]) REFERENCES [course].[Courses]([CourseId])
                ON DELETE NO ACTION;
             
                ALTER TABLE [enrollment].[EnrolledSessions]
                ADD CONSTRAINT [FK_EnrolledSessions_WeeklySlots_WeeklySlotId]
                FOREIGN KEY ([WeeklySlotId]) REFERENCES [course].[WeeklySlots]([WeeklySlotId])
                ON DELETE NO ACTION;
             
                ALTER TABLE [enrollment].[Attendances]
                ADD CONSTRAINT [FK_Attendances_AspNetUsers_StudentId]
                FOREIGN KEY ([StudentId]) REFERENCES [identity].[AspNetUsers]([Id])
                ON DELETE NO ACTION;
             
                ALTER TABLE [enrollment].[Attendances]
                ADD CONSTRAINT [FK_Attendances_ClassSessions_ClassSessionId]
                FOREIGN KEY ([ClassSessionId]) REFERENCES [course].[ClassSessions]([ClassSessionId])
                ON DELETE NO ACTION;
             
                ALTER TABLE [enrollment].[Attendances]
                ADD CONSTRAINT [FK_Attendances_Courses_CourseId]
                FOREIGN KEY ([CourseId]) REFERENCES [course].[Courses]([CourseId])
                ON DELETE NO ACTION;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE [enrollment].[Attendances] DROP CONSTRAINT [FK_Attendances_Courses_CourseId];
                ALTER TABLE [enrollment].[Attendances] DROP CONSTRAINT [FK_Attendances_ClassSessions_ClassSessionId];
                ALTER TABLE [enrollment].[Attendances] DROP CONSTRAINT [FK_Attendances_AspNetUsers_StudentId];
                ALTER TABLE [enrollment].[EnrolledSessions] DROP CONSTRAINT [FK_EnrolledSessions_WeeklySlots_WeeklySlotId];
                ALTER TABLE [enrollment].[Enrollments] DROP CONSTRAINT [FK_Enrollments_Courses_CourseId];
                ALTER TABLE [enrollment].[Enrollments] DROP CONSTRAINT [FK_Enrollments_AspNetUsers_StudentId];
            ");
            
            migrationBuilder.DropTable(
                name: "Attendances",
                schema: "enrollment");

            migrationBuilder.DropTable(
                name: "EnrolledSessions",
                schema: "enrollment");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "enrollment");

            migrationBuilder.DropTable(
                name: "Enrollments",
                schema: "enrollment");
        }
    }
}
