
namespace RaceDay.API.Models
{
    public class EventRoute
    {
        public int RouteId { get; set; }

        public decimal DistanceKm { get; set; }

        public string? RouteDescription { get; set; }

        public string? MapUrl { get; set; }

        public int CategoryId { get; set; }
    }
}
