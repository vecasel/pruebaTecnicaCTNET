using Microsoft.EntityFrameworkCore;
using SacRiosDesiertoApi.Models;

namespace SacRiosDesiertoApi.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<DocumentType> DocumentTypes { get; set; } = default!;
        public DbSet<Client> Clients { get; set; } = default!;
        public DbSet<Purchase> Purchases { get; set; } = default!;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique (DocumentTypeId, DocumentNumber)
            modelBuilder.Entity<Client>()
                .HasIndex(c => new { c.DocumentTypeId, c.DocumentNumber })
                .IsUnique();

            // Relaciones
            modelBuilder.Entity<DocumentType>()
                .HasMany(d => d.Clients)
                .WithOne(c => c.DocumentType)
                .HasForeignKey(c => c.DocumentTypeId);

            modelBuilder.Entity<Client>()
                .HasMany(c => c.Purchases)
                .WithOne(p => p.Client)
                .HasForeignKey(p => p.ClientId);
        }
    }
}
