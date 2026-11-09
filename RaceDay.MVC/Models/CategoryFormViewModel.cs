
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class CategoryFormViewModel
    {
        public int CategoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int EventId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0, 1000000)]
        [Display(Name = "Entry Fee (ZAR)")]
        public decimal EntryFee { get; set; }

        [Range(1, 100000)]
        [Display(Name = "Maximum Participants")]
        public int MaxParticipants { get; set; } = 100;
    }
}
