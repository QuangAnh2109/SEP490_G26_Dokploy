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

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.MustChangePassword)
                .HasDefaultValue(false);
        });
    }
}
