
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class EventFormViewModel
    {
        public int EventId { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = "Run";

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; }
            = DateTime.Today.AddDays(7);

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;
    }
}
