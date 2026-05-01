using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupAnswers",
                columns: table => new
                {
                    GroupAnswerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DependsOnGroupId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__GroupAns__2DBBC7BF07E0BCF8", x => x.GroupAnswerId);
                    table.ForeignKey(
                        name: "FK_GroupAnswers_DependsOnGroup",
                        column: x => x.DependsOnGroupId,
                        principalTable: "GroupAnswers",
                        principalColumn: "GroupAnswerId");
                });

            migrationBuilder.CreateTable(
                name: "InputTypes",
                columns: table => new
                {
                    InputTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Regex = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    GroupType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__InputTyp__CA63BB5A702ACA0D", x => x.InputTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__8AFACE1A13F124CB", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Subjects__AC1BA3A86E08C448", x => x.SubjectId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "char(60)", unicode: false, fixedLength: true, maxLength: 60, nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SecurityStamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    StudentId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__1788CC4C1B357F2D", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId");
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ChapterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Chapters__0893A36AEE81EE8B", x => x.ChapterId);
                    table.ForeignKey(
                        name: "FK_Chapters_Subjects",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId");
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    ClassId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Semester = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    InvitationCode = table.Column<string>(type: "char(8)", unicode: false, fixedLength: true, maxLength: 8, nullable: false),
                    InvitationCodeStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Classes__CB1927C04A621E7C", x => x.ClassId);
                    table.ForeignKey(
                        name: "FK_Classes_Subjects",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId");
                    table.ForeignKey(
                        name: "FK_Classes_Users",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ExamBlueprints",
                columns: table => new
                {
                    ExamBlueprintId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ExamBlue__C1EF9CEF974A7B17", x => x.ExamBlueprintId);
                    table.ForeignKey(
                        name: "FK_Blueprints_Subjects",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId");
                    table.ForeignKey(
                        name: "FK_ExamBlueprints_Users",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuestionContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    QuestionPurpose = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Question__0DC06FACF1A0E339", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_Questions_Chapters",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId");
                    table.ForeignKey(
                        name: "FK_Questions_Users",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ClassMembers",
                columns: table => new
                {
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    MemberStatus = table.Column<int>(type: "int", nullable: false),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ClassMem__4835757955CD1CFB", x => new { x.ClassId, x.StudentId });
                    table.ForeignKey(
                        name: "FK_ClassMembers_Classes",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId");
                    table.ForeignKey(
                        name: "FK_ClassMembers_Users",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ExamBlueprintChapter",
                columns: table => new
                {
                    ExamBlueprintId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    TotalOfQuestions = table.Column<int>(type: "int", nullable: false),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ExamBlue__E9BFA7D0F0EC64DB", x => new { x.ExamBlueprintId, x.ChapterId, x.Difficulty });
                    table.ForeignKey(
                        name: "FK_EBC_Blueprints",
                        column: x => x.ExamBlueprintId,
                        principalTable: "ExamBlueprints",
                        principalColumn: "ExamBlueprintId");
                    table.ForeignKey(
                        name: "FK_EBC_Chapters",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId");
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    ExamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamBlueprintId = table.Column<int>(type: "int", nullable: true),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    ShowScore = table.Column<int>(type: "int", nullable: false),
                    ShowAnswer = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    VisibleFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CloseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShuffleQuestion = table.Column<bool>(type: "bit", nullable: false),
                    AnswerTimingMode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Exams__297521C70CDECF29", x => x.ExamId);
                    table.ForeignKey(
                        name: "FK_Exams_Classes",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId");
                    table.ForeignKey(
                        name: "FK_Exams_ExamBlueprints",
                        column: x => x.ExamBlueprintId,
                        principalTable: "ExamBlueprints",
                        principalColumn: "ExamBlueprintId");
                    table.ForeignKey(
                        name: "FK_Exams_Subjects",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId");
                    table.ForeignKey(
                        name: "FK_Exams_Users",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "QuestionAnswers",
                columns: table => new
                {
                    QuestionAnswerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GroupAnswerId = table.Column<int>(type: "int", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    Point = table.Column<int>(type: "int", nullable: true),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Question__86BEDFCF73CA15C6", x => x.QuestionAnswerId);
                    table.ForeignKey(
                        name: "FK_QuestionAnswers_GroupAnswers",
                        column: x => x.GroupAnswerId,
                        principalTable: "GroupAnswers",
                        principalColumn: "GroupAnswerId");
                    table.ForeignKey(
                        name: "FK_QuestionAnswers_Questions",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId");
                });

            migrationBuilder.CreateTable(
                name: "Papers",
                columns: table => new
                {
                    PaperId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Papers__AB86120B71F05C29", x => x.PaperId);
                    table.ForeignKey(
                        name: "FK_Papers_Exams",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId");
                });

            migrationBuilder.CreateTable(
                name: "BlankInputs",
                columns: table => new
                {
                    QuestionAnswerId = table.Column<int>(type: "int", nullable: false),
                    InputTypeId = table.Column<int>(type: "int", nullable: false),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BlankInp__3A18E47A824B6BDA", x => new { x.QuestionAnswerId, x.InputTypeId });
                    table.ForeignKey(
                        name: "FK_BlankInputs_InputTypes",
                        column: x => x.InputTypeId,
                        principalTable: "InputTypes",
                        principalColumn: "InputTypeId");
                    table.ForeignKey(
                        name: "FK_BlankInputs_QuestionAnswers",
                        column: x => x.QuestionAnswerId,
                        principalTable: "QuestionAnswers",
                        principalColumn: "QuestionAnswerId");
                });

            migrationBuilder.CreateTable(
                name: "PaperQuestion",
                columns: table => new
                {
                    PaperId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PaperQue__7B5A14F1873A63DF", x => new { x.PaperId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_PQ_Papers",
                        column: x => x.PaperId,
                        principalTable: "Papers",
                        principalColumn: "PaperId");
                    table.ForeignKey(
                        name: "FK_PQ_Questions",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId");
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    SubmissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    PaperId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalPoints = table.Column<decimal>(type: "decimal(5,3)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Submissi__449EE12553B1053B", x => x.SubmissionId);
                    table.ForeignKey(
                        name: "FK_Submissions_Papers",
                        column: x => x.PaperId,
                        principalTable: "Papers",
                        principalColumn: "PaperId");
                    table.ForeignKey(
                        name: "FK_Submissions_Users",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "StudentAnswers",
                columns: table => new
                {
                    StudentAnswerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    QuestionAnswerId = table.Column<int>(type: "int", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentA__6E3EA405089AC96F", x => x.StudentAnswerId);
                    table.ForeignKey(
                        name: "FK_StudentAnswers_QuestionAnswers",
                        column: x => x.QuestionAnswerId,
                        principalTable: "QuestionAnswers",
                        principalColumn: "QuestionAnswerId");
                    table.ForeignKey(
                        name: "FK_StudentAnswers_Submissions",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlankInputs_InputTypeId",
                table: "BlankInputs",
                column: "InputTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SubjectId",
                table: "Chapters",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_SubjectId",
                table: "Classes",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_TeacherId",
                table: "Classes",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "UQ__Classes__286690FF0D037D77",
                table: "Classes",
                column: "InvitationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassMembers_StudentId",
                table: "ClassMembers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamBlueprintChapter_ChapterId",
                table: "ExamBlueprintChapter",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamBlueprints_SubjectId",
                table: "ExamBlueprints",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamBlueprints_TeacherId",
                table: "ExamBlueprints",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ClassId",
                table: "Exams",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ExamBlueprintId",
                table: "Exams",
                column: "ExamBlueprintId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_SubjectId",
                table: "Exams",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_TeacherId",
                table: "Exams",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAnswers_DependsOnGroupId",
                table: "GroupAnswers",
                column: "DependsOnGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperQuestion_QuestionId",
                table: "PaperQuestion",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Papers_ExamId",
                table: "Papers",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAnswers_GroupAnswerId",
                table: "QuestionAnswers",
                column: "GroupAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAnswers_QuestionId",
                table: "QuestionAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ChapterId",
                table: "Questions",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CreatedByUserId",
                table: "Questions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_QuestionAnswerId",
                table: "StudentAnswers",
                column: "QuestionAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_SubmissionId",
                table: "StudentAnswers",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_PaperId",
                table: "Submissions",
                column: "PaperId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId",
                table: "Submissions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D10534BF4ED69B",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlankInputs");

            migrationBuilder.DropTable(
                name: "ClassMembers");

            migrationBuilder.DropTable(
                name: "ExamBlueprintChapter");

            migrationBuilder.DropTable(
                name: "PaperQuestion");

            migrationBuilder.DropTable(
                name: "StudentAnswers");

            migrationBuilder.DropTable(
                name: "InputTypes");

            migrationBuilder.DropTable(
                name: "QuestionAnswers");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "GroupAnswers");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Papers");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "ExamBlueprints");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
