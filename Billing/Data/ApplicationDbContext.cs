using Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace Billing.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public DbSet<UserCoin> UserCoins { get; set; }

        public ApplicationDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                    new User { Id = 1, Name = "boris", Rating = 5000 },
                    new User { Id = 2, Name = "maria", Rating = 1000 },
                    new User { Id = 3, Name = "oleg", Rating = 800 }
            );
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase(databaseName: "BillingDb");
    }
}
