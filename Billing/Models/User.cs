namespace Billing.Models
{
    /// <summary>
    /// Модель для сохранения в БД данных о пользователе
    /// Добавил сюда рейтинг и количество монет у пользователя
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int Rating { get; set; }

        public long Amount { get; set; }
    }
}
