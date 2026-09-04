using BackTemplate.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackTemplate.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.Property(b => b.Title).IsRequired().HasMaxLength(300);
            entity.HasOne(b => b.Author)
                  .WithMany(a => a.Books)
                  .HasForeignKey(b => b.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(320);
            entity.Property(u => u.PasswordHash).IsRequired();
        });
    }
}
