namespace Billing.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int Rating { get; set; }

        public long Amount { get; set; }
    }
}
