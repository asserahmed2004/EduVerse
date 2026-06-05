using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLmsLearningFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttendanceCode",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendanceCodeCreatedAt",
                table: "Sessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalLink",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateCode",
                table: "Enrollments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Enrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Enrollments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "ProgressPercentage",
                table: "Enrollments",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "InstructorId",
                table: "Courses",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "AssignmentSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Grade",
                table: "AssignmentSubmissions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                table: "AssignmentSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "AssignmentSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextAnswer",
                table: "AssignmentSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttendanceCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CertificateCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionMaterials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentSessionProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSessionProgresses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_InstructorId",
                table: "Courses",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId_SessionId",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRecords_CertificateCode",
                table: "CertificateRecords",
                column: "CertificateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentSessionProgresses_StudentId_SessionId",
                table: "StudentSessionProgresses",
                columns: new[] { "StudentId", "SessionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_AspNetUsers_InstructorId",
                table: "Courses",
                column: "InstructorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_AspNetUsers_InstructorId",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "CertificateRecords");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "SessionMaterials");

            migrationBuilder.DropTable(
                name: "StudentSessionProgresses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_InstructorId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AttendanceCode",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "AttendanceCodeCreatedAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ExternalLink",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "CertificateCode",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "IsLate",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "TextAnswer",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Assignments");
        }
    }
}
