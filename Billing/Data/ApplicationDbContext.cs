using Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace Billing.Data
{
    /// <summary>
    /// Класс для контекста БД. 
    /// База создается в памяти приложения и инициализируется тремя тестовыми пользователями при каждом запуске приложения
    /// Хранение в памяте приложения пока программа работает. В дальнейшем можно перенасторить на настояющие БД.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public DbSet<UserCoin> UserCoins { get; set; }

        public ApplicationDbContext()
        {
            //Создает БД если её нету
            Database.EnsureCreated();
        }
        // Инициализирует базу данных тестовыми пользователями
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                    new User { Id = 1, Name = "boris", Rating = 5000 },
                    new User { Id = 2, Name = "maria", Rating = 1000 },
                    new User { Id = 3, Name = "oleg", Rating = 800 }
            );
        }
        //UseInMemoryDatabase это значит что база будет создаваться в памяти приложения
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase(databaseName: "BillingDb");
    }
}
