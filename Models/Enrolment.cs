
namespace RaceDay.API.Models
{
    public class Enrolment
    {
        public int EnrolmentId { get; set; }

        public DateTime EnrolmentDate { get; set; }

        public string Status { get; set; } = "Confirmed";

        public int ParticipantId { get; set; }

        public int CategoryId { get; set; }
    }
}
