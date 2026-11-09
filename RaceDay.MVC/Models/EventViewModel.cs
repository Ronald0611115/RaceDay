namespace RaceDay.MVC.Models
{
    public class EventViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public int OrganiserId { get; set; }

        public string Location { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? BannerImageUrl { get; set; }
    }
}