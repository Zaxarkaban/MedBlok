using System;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace DocumentGenerator.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<OrderClause> OrderClauses { get; set; }
        public DbSet<Doctor> Doctors { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocumentGenerator", "OrderClauses.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderClause>().HasKey(oc => oc.Id);
            modelBuilder.Entity<Doctor>().HasKey(d => d.Id);
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.OrderClause)
                .WithMany()
                .HasForeignKey(d => d.ClauseId);
        }
    }

    public class OrderClause
    {
        public int Id { get; set; }
        public string ClauseText { get; set; }
    }

    public class Doctor
    {
        public int Id { get; set; }
        public string DoctorName { get; set; }
        public int ClauseId { get; set; }
        public OrderClause OrderClause { get; set; }
    }
}