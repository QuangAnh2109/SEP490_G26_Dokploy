using Microsoft.EntityFrameworkCore;

namespace Backend.Models;

public partial class MtcaSep490G26Context
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exam>(entity =>
        {
            entity.Property(e => e.Status)
                .ValueGeneratedNever();
        });

        modelBuilder.Entity<Semester>().Property(e => e.ConcurrencyStamp).IsRowVersion();
        modelBuilder.Entity<Subject>().Property(e => e.ConcurrencyStamp).IsRowVersion();
        modelBuilder.Entity<Chapter>().Property(e => e.ConcurrencyStamp).IsRowVersion();

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.MustChangePassword)
                .HasDefaultValue(false);
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.Property(e => e.GradingStatus)
                .HasDefaultValue((byte)0);

            entity.Property(e => e.GradingError)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<StudentAnswer>(entity =>
        {
            entity.Property(e => e.PointsEarned)
                .HasColumnType("decimal(5, 2)");

            entity.HasIndex(e => new { e.SubmissionId, e.IsCorrect })
                .HasDatabaseName("IX_StudentAnswers_Submission_IsCorrect");
        });
    }
}
