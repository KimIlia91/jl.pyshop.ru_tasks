namespace Billing.Models
{
    public class UserCoin
    {
        public int Id { get; set; }

        public string History { get; set; } = null!;

        public int UserId { get; set; }
    }
}
