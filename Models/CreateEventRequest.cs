
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class CreateEventRequest
    {
        [Required]
        [StringLength(100)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$",
        ErrorMessage = "Event type must be Run, Walk, or Cycle.")]
        public string EventType { get; set; } = string.Empty;


        [Required]
        public DateTime? EventDate { get; set; }

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
