
namespace RaceDay.MVC.Models
{
    public class EnrolmentViewModel
    {
        public int EnrolmentId { get; set; }

        public int CategoryId { get; set; }

        public int ParticipantId { get; set; }

        public DateTime EnrolmentDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public CategoryViewModel? Category { get; set; }

        // Display properties populated by MVC
        public string EventName { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public int EventId { get; set; }
    }
}
