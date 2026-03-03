using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Backend.Models;

public partial class MtcaSep490G26Context : DbContext
{
    public MtcaSep490G26Context()
    {
    }

    public MtcaSep490G26Context(DbContextOptions<MtcaSep490G26Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Chapter> Chapters { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassMember> ClassMembers { get; set; }

    public virtual DbSet<Exam> Exams { get; set; }

    public virtual DbSet<ExamBlueprint> ExamBlueprints { get; set; }

    public virtual DbSet<ExamBlueprintChapter> ExamBlueprintChapters { get; set; }

    public virtual DbSet<GroupAnswer> GroupAnswers { get; set; }

    public virtual DbSet<InputType> InputTypes { get; set; }

    public virtual DbSet<Paper> Papers { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionAnswer> QuestionAnswers { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<StudentAnswer> StudentAnswers { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.ChapterId).HasName("PK__Chapters__0893A36AC87B0DBF");

            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Subject).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chapters_Subjects");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927C0CCFF34F2");

            entity.HasIndex(e => e.InvitationCode, "UQ__Classes__286690FFEEF1AE44").IsUnique();

            entity.Property(e => e.ConcurrencyStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.InvitationCode)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.InvitationCodeStatus).HasDefaultValue(1);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Semester)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Status).HasDefaultValue(1);

            entity.HasOne(d => d.Subject).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Subjects");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Users");
        });

        modelBuilder.Entity<ClassMember>(entity =>
        {
            entity.HasKey(e => new { e.ClassId, e.StudentId }).HasName("PK__ClassMem__48357579D9FDC25D");

            entity.Property(e => e.ConcurrencyStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.MemberStatus).HasDefaultValue(1);

            entity.HasOne(d => d.Class).WithMany(p => p.ClassMembers)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassMembers_Classes");

            entity.HasOne(d => d.Student).WithMany(p => p.ClassMembers)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassMembers_Users");
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(e => e.ExamId).HasName("PK__Exams__297521C70627A098");

            entity.Property(e => e.ConcurrencyStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.MaxAttempts).HasDefaultValue(1);
            entity.Property(e => e.ShowScore).HasDefaultValue(true);
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Class).WithMany(p => p.Exams)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK_Exams_Classes");

            entity.HasOne(d => d.ExamBlueprint).WithMany(p => p.Exams)
                .HasForeignKey(d => d.ExamBlueprintId)
                .HasConstraintName("FK_Exams_ExamBlueprints");

            entity.HasOne(d => d.Subject).WithMany(p => p.Exams)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Exams_Subjects");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Exams)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Exams_Users");
        });

        modelBuilder.Entity<ExamBlueprint>(entity =>
        {
            entity.HasKey(e => e.ExamBlueprintId).HasName("PK__ExamBlue__C1EF9CEFE4A15D5F");

            entity.Property(e => e.ConcurrencyStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Subject).WithMany(p => p.ExamBlueprints)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Blueprints_Subjects");

            entity.HasOne(d => d.Teacher).WithMany(p => p.ExamBlueprints)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamBlueprints_Users");
        });

        modelBuilder.Entity<ExamBlueprintChapter>(entity =>
        {
            entity.HasKey(e => new { e.ExamBlueprintId, e.ChapterId, e.Difficulty }).HasName("PK__ExamBlue__E9BFA7D088EF7B9E");

            entity.ToTable("ExamBlueprintChapter");

            entity.Property(e => e.ConcurrencyStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Chapter).WithMany(p => p.ExamBlueprintChapters)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EBC_Chapters");

            entity.HasOne(d => d.ExamBlueprint).WithMany(p => p.ExamBlueprintChapters)
                .HasForeignKey(d => d.ExamBlueprintId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EBC_Blueprints");
        });

        modelBuilder.Entity<GroupAnswer>(entity =>
        {
            entity.HasKey(e => e.GroupAnswerId).HasName("PK__GroupAns__2DBBC7BFE34032CE");

            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<InputType>(entity =>
        {
            entity.HasKey(e => e.InputTypeId).HasName("PK__InputTyp__CA63BB5A30FB4D39");

            entity.ToTable("InputType");

            entity.Property(e => e.GroupType).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Regex).HasMaxLength(400);
        });

        modelBuilder.Entity<Paper>(entity =>
        {
            entity.HasKey(e => e.PaperId).HasName("PK__Papers__AB86120B5D64404D");

            entity.HasOne(d => d.Exam).WithMany(p => p.Papers)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Papers_Exams");

            entity.HasMany(d => d.Questions).WithMany(p => p.Papers)
                .UsingEntity<Dictionary<string, object>>(
                    "PaperQuestion",
                    r => r.HasOne<Question>().WithMany()
                        .HasForeignKey("QuestionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PQ_Questions"),
                    l => l.HasOne<Paper>().WithMany()
                        .HasForeignKey("PaperId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PQ_Papers"),
                    j =>
                    {
                        j.HasKey("PaperId", "QuestionId").HasName("PK__PaperQue__7B5A14F1C7FAE5C6");
                        j.ToTable("PaperQuestion");
                    });
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__Question__0DC06FAC6EDD0695");

            entity.Property(e => e.ConcurrencyStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.QuestionType).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Chapter).WithMany(p => p.Questions)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Questions_Chapters");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Questions)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Questions_Users");
        });

        modelBuilder.Entity<QuestionAnswer>(entity =>
        {
            entity.HasKey(e => e.QuestionAnswersId).HasName("PK__Question__D3118BEF6D94B6A3");

            entity.Property(e => e.ConcurrencyStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.GroupAnswer).WithMany(p => p.QuestionAnswers)
                .HasForeignKey(d => d.GroupAnswerId)
                .HasConstraintName("FK_QuestionAnswers_GroupAnswers");

            entity.HasOne(d => d.InputType).WithMany(p => p.QuestionAnswers)
                .HasForeignKey(d => d.InputTypeId)
                .HasConstraintName("FK_QuestionAnswers_InputType");

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuestionAnswers_Questions");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A386BC1F9");

            entity.Property(e => e.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<StudentAnswer>(entity =>
        {
            entity.HasKey(e => e.StudentAnswersId).HasName("PK__StudentA__8E6B6BCBB2B070F2");

            entity.Property(e => e.ConcurrencyStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.QuestionAnswers).WithMany(p => p.StudentAnswers)
                .HasForeignKey(d => d.QuestionAnswersId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentAnswers_QuestionAnswers");

            entity.HasOne(d => d.Submission).WithMany(p => p.StudentAnswers)
                .HasForeignKey(d => d.SubmissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentAnswers_Submissions");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subjects__AC1BA3A876E30AC2");

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId).HasName("PK__Submissi__449EE125B5237A22");

            entity.Property(e => e.ConcurrencyStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.TotalPoints).HasColumnType("decimal(5, 3)");

            entity.HasOne(d => d.Paper).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.PaperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submissions_Papers");

            entity.HasOne(d => d.Student).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submissions_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C5E03B37F");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534DFBA28D3").IsUnique();

            entity.Property(e => e.ConcurrencyStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(60)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SecurityStamp).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.StudentId)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
