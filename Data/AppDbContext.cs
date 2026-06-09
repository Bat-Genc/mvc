using Microsoft.EntityFrameworkCore;
using SchoolMvc.Models;

namespace SchoolMvc.Data
{
    public class AppDbContext : DbContext
    {
        // Конструктор без параметри (за дизайн време)
        public AppDbContext() { }
        
        // Конструктор с опции (за runtime)
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        public DbSet<AppUser> Users { get; set; }
        public DbSet<UserEncryptionMethod> UserEncryptionMethods { get; set; }
        public DbSet<EncryptionHistory> EncryptionHistories { get; set; }
        public DbSet<EncryptionLog> EncryptionLogs { get; set; }
        public DbSet<DecryptionKey> DecryptionKeys { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();
            
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = "Server=localhost;Database=CipherXDB;User Id=sa;Password=MyP@ssw0rd2024;TrustServerCertificate=true;";
                optionsBuilder.UseSqlServer(connectionString);
            }
        }
    }
}