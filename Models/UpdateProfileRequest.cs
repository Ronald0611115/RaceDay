
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class UpdateProfileRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;
    }
}
