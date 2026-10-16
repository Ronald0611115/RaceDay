using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class CategoryRequest
    {
        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0, 100000)]
        public decimal EntryFee { get; set; }

        [Range(1, 100000)]
        public int MaxParticipants { get; set; }
    }
}
