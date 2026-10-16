
namespace RaceDay.API.Models
{
    public class Result
    {
        public int ResultId { get; set; }

        public TimeSpan FinishTime { get; set; }

        public int Position { get; set; }

        public DateTime RecordedAt { get; set; }

        public int EnrolmentId { get; set; }
    }
}
