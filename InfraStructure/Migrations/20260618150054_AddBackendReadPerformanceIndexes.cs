using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBackendReadPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProviderIntentionId",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MerchantOrderId",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "CertificateRecords",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CourseId",
                table: "Sessions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CourseId_SessionNumber",
                table: "Sessions",
                columns: new[] { "CourseId", "SessionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MerchantOrderId",
                table: "Payments",
                column: "MerchantOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentStatus",
                table: "Payments",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderIntentionId",
                table: "Payments",
                column: "ProviderIntentionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SubmittingDate",
                table: "Payments",
                column: "SubmittingDate");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_IsDeleted",
                table: "Courses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRecords_CourseId",
                table: "CertificateRecords",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRecords_StudentId",
                table: "CertificateRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_AssignmentId",
                table: "AssignmentSubmissions",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_SessionId",
                table: "Assignments",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_CourseId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_CourseId_SessionNumber",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_MerchantOrderId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentStatus",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderIntentionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SubmittingDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Courses_IsDeleted",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CertificateRecords_CourseId",
                table: "CertificateRecords");

            migrationBuilder.DropIndex(
                name: "IX_CertificateRecords_StudentId",
                table: "CertificateRecords");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmissions_AssignmentId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_SessionId",
                table: "Assignments");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderIntentionId",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "MerchantOrderId",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "CertificateRecords",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
