using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string legacyUserId = "00000000-0000-0000-0000-000000000001";

            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = '{legacyUserId}')
BEGIN
    INSERT INTO [AspNetUsers]
    (
        [Id],
        [FullName],
        [Birthdate],
        [ProfilePicture],
        [UserName],
        [NormalizedUserName],
        [Email],
        [NormalizedEmail],
        [EmailConfirmed],
        [PasswordHash],
        [SecurityStamp],
        [ConcurrencyStamp],
        [PhoneNumber],
        [PhoneNumberConfirmed],
        [TwoFactorEnabled],
        [LockoutEnd],
        [LockoutEnabled],
        [AccessFailedCount]
    )
    VALUES
    (
        '{legacyUserId}',
        'Legacy EduVerse User',
        '2000-01-01',
        '',
        'legacy.eduverse.user',
        'LEGACY.EDUVERSE.USER',
        'legacy-user@eduverse.local',
        'LEGACY-USER@EDUVERSE.LOCAL',
        1,
        NULL,
        CONVERT(nvarchar(36), NEWID()),
        CONVERT(nvarchar(36), NEWID()),
        NULL,
        0,
        0,
        NULL,
        1,
        0
    );
END
");

            migrationBuilder.Sql($@"
UPDATE [Courses]
SET [OrgId] = '{legacyUserId}'
WHERE [OrgId] IS NULL
   OR [OrgId] = ''
   OR NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [AspNetUsers].[Id] = [Courses].[OrgId]);

UPDATE [Sessions]
SET [TrainerId] = '{legacyUserId}'
WHERE [TrainerId] IS NULL
   OR [TrainerId] = ''
   OR NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [AspNetUsers].[Id] = [Sessions].[TrainerId]);

UPDATE [Enrollments]
SET [StudentId] = '{legacyUserId}'
WHERE [StudentId] IS NULL
   OR [StudentId] = ''
   OR NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [AspNetUsers].[Id] = [Enrollments].[StudentId]);

UPDATE [Payments]
SET [StudentId] = '{legacyUserId}'
WHERE [StudentId] IS NULL
   OR [StudentId] = ''
   OR NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [AspNetUsers].[Id] = [Payments].[StudentId]);

UPDATE [Ratings]
SET [StudentId] = '{legacyUserId}'
WHERE [StudentId] IS NULL
   OR [StudentId] = ''
   OR NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [AspNetUsers].[Id] = [Ratings].[StudentId]);

UPDATE [AssignmentSubmissions]
SET [StudentId] = '{legacyUserId}'
WHERE [StudentId] IS NULL
   OR [StudentId] = ''
   OR NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [AspNetUsers].[Id] = [AssignmentSubmissions].[StudentId]);
");

            migrationBuilder.AlterColumn<string>(
                name: "TrainerId",
                table: "Sessions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "OrgId",
                table: "Courses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TrainerId",
                table: "Sessions",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_StudentId",
                table: "Ratings",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_StudentId",
                table: "Payments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_OrgId",
                table: "Courses",
                column: "OrgId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmissions_AspNetUsers_StudentId",
                table: "AssignmentSubmissions",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_AspNetUsers_OrgId",
                table: "Courses",
                column: "OrgId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_AspNetUsers_StudentId",
                table: "Enrollments",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_StudentId",
                table: "Payments",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_AspNetUsers_StudentId",
                table: "Ratings",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_AspNetUsers_TrainerId",
                table: "Sessions",
                column: "TrainerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmissions_AspNetUsers_StudentId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_AspNetUsers_OrgId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_AspNetUsers_StudentId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_StudentId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_AspNetUsers_StudentId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_AspNetUsers_TrainerId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_TrainerId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_StudentId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Payments_StudentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Courses_OrgId",
                table: "Courses");

            migrationBuilder.AlterColumn<string>(
                name: "TrainerId",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "OrgId",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
