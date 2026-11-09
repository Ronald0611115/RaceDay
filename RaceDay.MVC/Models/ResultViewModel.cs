
namespace RaceDay.MVC.Models
{
    public class ResultViewModel
    {
        public int ResultId { get; set; }
        public string? FinishTime { get; set; }
        public int Position { get; set; }
        public DateTime RecordedAt { get; set; }
        public int EnrolmentId { get; set; }
    }
}
