
namespace RaceDay.API.Models
{
    public class Event
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsCancelled { get; set; } = false;

        public int OrganiserId { get; set; }
    }
}
