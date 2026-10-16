
namespace RaceDay.API.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal EntryFee { get; set; }

        public int MaxParticipants { get; set; }

        public int EventId { get; set; }
    }
}
