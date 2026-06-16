using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameStudentSessionProgressForPersonalTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsCompleted",
                table: "StudentSessionProgresses",
                newName: "IsDone");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "StudentSessionProgresses",
                newName: "DoneAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "StudentSessionProgresses",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSessionProgresses_CourseId",
                table: "StudentSessionProgresses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSessionProgresses_SessionId",
                table: "StudentSessionProgresses",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSessionProgresses_AspNetUsers_StudentId",
                table: "StudentSessionProgresses",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSessionProgresses_Courses_CourseId",
                table: "StudentSessionProgresses",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSessionProgresses_Sessions_SessionId",
                table: "StudentSessionProgresses",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentSessionProgresses_AspNetUsers_StudentId",
                table: "StudentSessionProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSessionProgresses_Courses_CourseId",
                table: "StudentSessionProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSessionProgresses_Sessions_SessionId",
                table: "StudentSessionProgresses");

            migrationBuilder.DropIndex(
                name: "IX_StudentSessionProgresses_CourseId",
                table: "StudentSessionProgresses");

            migrationBuilder.DropIndex(
                name: "IX_StudentSessionProgresses_SessionId",
                table: "StudentSessionProgresses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StudentSessionProgresses");

            migrationBuilder.RenameColumn(
                name: "IsDone",
                table: "StudentSessionProgresses",
                newName: "IsCompleted");

            migrationBuilder.RenameColumn(
                name: "DoneAt",
                table: "StudentSessionProgresses",
                newName: "CompletedAt");
        }
    }
}
