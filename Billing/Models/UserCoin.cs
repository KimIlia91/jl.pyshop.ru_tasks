namespace Billing.Models
{
    /// <summary>
    /// Моделя для хранения в БД монет 
    /// также есть поле UserId которое указывает на владельца монеты
    /// </summary>
    public class UserCoin
    {
        public int Id { get; set; }

        public string History { get; set; } = null!;

        public int UserId { get; set; }
    }
}
