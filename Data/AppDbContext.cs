using Microsoft.EntityFrameworkCore;
using SchoolMvc.Models;

namespace SchoolMvc.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() { }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        public DbSet<AppUser> Users { get; set; }
        public DbSet<UserEncryptionMethod> UserEncryptionMethods { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();
            
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}