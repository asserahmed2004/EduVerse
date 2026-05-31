using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseSoftDeleteAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedById",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByName",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DeletedByName",
                table: "Courses");
        }
    }
}
