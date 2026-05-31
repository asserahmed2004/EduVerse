using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseRestoreAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RestoredAt",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RestoredById",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RestoredByName",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RestoredAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RestoredById",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RestoredByName",
                table: "Courses");
        }
    }
}
