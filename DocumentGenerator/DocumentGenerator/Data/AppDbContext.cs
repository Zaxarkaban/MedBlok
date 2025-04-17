using Microsoft.EntityFrameworkCore;
using DocumentGenerator.Data.Entities;

namespace DocumentGenerator.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<OrderClause> OrderClauses { get; set; }
        public DbSet<Doctor> Doctors { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderClause>()
                .HasKey(oc => oc.Id);

            modelBuilder.Entity<Doctor>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.OrderClause)
                .WithMany()
                .HasForeignKey(d => d.ClauseId);
        }
    }
}