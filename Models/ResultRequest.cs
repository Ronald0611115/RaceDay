using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class ResultRequest
    {
        [Required]
        public TimeSpan FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int Position { get; set; }
    }
}
