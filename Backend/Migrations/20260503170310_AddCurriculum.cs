using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE dbo.Semesters (
    SemesterId        INT IDENTITY(1,1) NOT NULL,
    Code              VARCHAR(20)       NOT NULL,
    Name              NVARCHAR(100)     NOT NULL,
    StartDate         DATE              NOT NULL,
    EndDate           DATE              NOT NULL,
    Status            INT               NOT NULL CONSTRAINT DF_Semesters_Status       DEFAULT (1),
    CreatedByUserId   INT               NOT NULL,
    CreatedAtUtc      DATETIME2         NOT NULL CONSTRAINT DF_Semesters_CreatedAtUtc DEFAULT (GETUTCDATE()),
    UpdatedByUserId   INT               NOT NULL,
    UpdatedAtUtc      DATETIME2         NOT NULL CONSTRAINT DF_Semesters_UpdatedAtUtc DEFAULT (GETUTCDATE()),
    ConcurrencyStamp  ROWVERSION        NOT NULL,

    CONSTRAINT PK_Semesters             PRIMARY KEY (SemesterId),
    CONSTRAINT UQ_Semesters_Code        UNIQUE (Code),
    CONSTRAINT CK_Semesters_Dates       CHECK (EndDate > StartDate),
    CONSTRAINT FK_Semesters_CreatedBy   FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Semesters_UpdatedBy   FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId)
);

ALTER TABLE dbo.Subjects
    ADD Description       NVARCHAR(1000) NULL,
        Status            INT  NOT NULL CONSTRAINT DF_Subjects_Status          DEFAULT (1),
        CreatedByUserId   INT  NOT NULL CONSTRAINT DF_Subjects_CreatedByUserId DEFAULT (1),
        CreatedAtUtc      DATETIME2 NOT NULL CONSTRAINT DF_Subjects_CreatedAtUtc DEFAULT (GETUTCDATE()),
        UpdatedByUserId   INT  NOT NULL CONSTRAINT DF_Subjects_UpdatedByUserId DEFAULT (1),
        UpdatedAtUtc      DATETIME2 NOT NULL CONSTRAINT DF_Subjects_UpdatedAtUtc DEFAULT (GETUTCDATE()),
        ConcurrencyStamp  ROWVERSION NOT NULL;

ALTER TABLE dbo.Subjects ALTER COLUMN Code VARCHAR(50) NOT NULL;
ALTER TABLE dbo.Subjects ADD CONSTRAINT UQ_Subjects_Code UNIQUE (Code);

ALTER TABLE dbo.Subjects
    ADD CONSTRAINT FK_Subjects_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Subjects_UpdatedBy FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId);

ALTER TABLE dbo.Chapters
    ADD Description       NVARCHAR(1000) NULL,
        DisplayOrder      INT  NOT NULL CONSTRAINT DF_Chapters_DisplayOrder    DEFAULT (0),
        Status            INT  NOT NULL CONSTRAINT DF_Chapters_Status          DEFAULT (1),
        CreatedByUserId   INT  NOT NULL CONSTRAINT DF_Chapters_CreatedByUserId DEFAULT (1),
        CreatedAtUtc      DATETIME2 NOT NULL CONSTRAINT DF_Chapters_CreatedAtUtc DEFAULT (GETUTCDATE()),
        UpdatedByUserId   INT  NOT NULL CONSTRAINT DF_Chapters_UpdatedByUserId DEFAULT (1),
        UpdatedAtUtc      DATETIME2 NOT NULL CONSTRAINT DF_Chapters_UpdatedAtUtc DEFAULT (GETUTCDATE()),
        ConcurrencyStamp  ROWVERSION NOT NULL;

ALTER TABLE dbo.Chapters
    ADD CONSTRAINT FK_Chapters_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Chapters_UpdatedBy FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId);

CREATE UNIQUE INDEX UQ_Chapters_Subject_Name ON dbo.Chapters(SubjectId, Name);

ALTER TABLE dbo.Classes DROP COLUMN Semester;
ALTER TABLE dbo.Classes ADD SemesterId INT NOT NULL CONSTRAINT DF_Classes_SemesterId DEFAULT (1);
ALTER TABLE dbo.Classes
    ADD CONSTRAINT FK_Classes_Semesters FOREIGN KEY (SemesterId) REFERENCES dbo.Semesters(SemesterId);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE dbo.Classes DROP CONSTRAINT FK_Classes_Semesters;
ALTER TABLE dbo.Classes DROP CONSTRAINT DF_Classes_SemesterId;
ALTER TABLE dbo.Classes DROP COLUMN SemesterId;
ALTER TABLE dbo.Classes ADD Semester NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Classes_Semester DEFAULT ('');

DROP INDEX UQ_Chapters_Subject_Name ON dbo.Chapters;

ALTER TABLE dbo.Chapters DROP CONSTRAINT FK_Chapters_CreatedBy;
ALTER TABLE dbo.Chapters DROP CONSTRAINT FK_Chapters_UpdatedBy;
ALTER TABLE dbo.Chapters DROP CONSTRAINT DF_Chapters_DisplayOrder;
ALTER TABLE dbo.Chapters DROP CONSTRAINT DF_Chapters_Status;
ALTER TABLE dbo.Chapters DROP CONSTRAINT DF_Chapters_CreatedByUserId;
ALTER TABLE dbo.Chapters DROP CONSTRAINT DF_Chapters_CreatedAtUtc;
ALTER TABLE dbo.Chapters DROP CONSTRAINT DF_Chapters_UpdatedByUserId;
ALTER TABLE dbo.Chapters DROP CONSTRAINT DF_Chapters_UpdatedAtUtc;

ALTER TABLE dbo.Chapters
    DROP COLUMN Description,
                DisplayOrder,
                Status,
                CreatedByUserId,
                CreatedAtUtc,
                UpdatedByUserId,
                UpdatedAtUtc,
                ConcurrencyStamp;

ALTER TABLE dbo.Subjects DROP CONSTRAINT FK_Subjects_CreatedBy;
ALTER TABLE dbo.Subjects DROP CONSTRAINT FK_Subjects_UpdatedBy;
ALTER TABLE dbo.Subjects DROP CONSTRAINT UQ_Subjects_Code;

ALTER TABLE dbo.Subjects DROP CONSTRAINT DF_Subjects_Status;
ALTER TABLE dbo.Subjects DROP CONSTRAINT DF_Subjects_CreatedByUserId;
ALTER TABLE dbo.Subjects DROP CONSTRAINT DF_Subjects_CreatedAtUtc;
ALTER TABLE dbo.Subjects DROP CONSTRAINT DF_Subjects_UpdatedByUserId;
ALTER TABLE dbo.Subjects DROP CONSTRAINT DF_Subjects_UpdatedAtUtc;

ALTER TABLE dbo.Subjects ALTER COLUMN Code VARCHAR(50) NULL;

ALTER TABLE dbo.Subjects
    DROP COLUMN Description,
                Status,
                CreatedByUserId,
                CreatedAtUtc,
                UpdatedByUserId,
                UpdatedAtUtc,
                ConcurrencyStamp;

DROP TABLE dbo.Semesters;
            ");
        }
    }
}
