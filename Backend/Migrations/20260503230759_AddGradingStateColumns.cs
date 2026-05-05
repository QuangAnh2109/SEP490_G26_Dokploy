using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddGradingStateColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentAnswers_SubmissionId",
                table: "StudentAnswers");

            migrationBuilder.AddColumn<string>(
                name: "GradingError",
                table: "Submissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "GradingStatus",
                table: "Submissions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "StudentAnswers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PointsEarned",
                table: "StudentAnswers",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_Submission_IsCorrect",
                table: "StudentAnswers",
                columns: new[] { "SubmissionId", "IsCorrect" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentAnswers_Submission_IsCorrect",
                table: "StudentAnswers");

            migrationBuilder.DropColumn(
                name: "GradingError",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "GradingStatus",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "StudentAnswers");

            migrationBuilder.DropColumn(
                name: "PointsEarned",
                table: "StudentAnswers");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_SubmissionId",
                table: "StudentAnswers",
                column: "SubmissionId");
        }
    }
}
