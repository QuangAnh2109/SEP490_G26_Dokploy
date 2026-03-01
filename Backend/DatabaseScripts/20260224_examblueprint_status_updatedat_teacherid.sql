/*
Adds ownership and status fields for ExamBlueprint.
If your ExamBlueprint table already contains rows, you must backfill TeacherId
before the script can enforce NOT NULL + FK.
*/

IF COL_LENGTH('dbo.ExamBlueprint', 'Status') IS NULL
BEGIN
    ALTER TABLE dbo.ExamBlueprint
    ADD [Status] INT NOT NULL CONSTRAINT DF_ExamBlueprint_Status DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.ExamBlueprint', 'UpdatedAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.ExamBlueprint
    ADD [UpdatedAtUtc] DATETIME2 NOT NULL CONSTRAINT DF_ExamBlueprint_UpdatedAtUtc DEFAULT (GETUTCDATE());
END
GO

IF COL_LENGTH('dbo.ExamBlueprint', 'TeacherId') IS NULL
BEGIN
    ALTER TABLE dbo.ExamBlueprint
    ADD [TeacherId] INT NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM dbo.ExamBlueprint
    WHERE TeacherId IS NULL
)
BEGIN
    THROW 51000, 'Backfill ExamBlueprint.TeacherId before enforcing NOT NULL and FK.', 1;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ExamBlueprint')
      AND name = 'TeacherId'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.ExamBlueprint
    ALTER COLUMN [TeacherId] INT NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Blueprints_Users'
)
BEGIN
    ALTER TABLE dbo.ExamBlueprint
    ADD CONSTRAINT FK_Blueprints_Users
        FOREIGN KEY (TeacherId) REFERENCES dbo.Users(UserId);
END
GO

