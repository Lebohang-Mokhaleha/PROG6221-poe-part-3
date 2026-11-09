using RaceDay.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class EventEnrolment
{
    [Key]
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public user? Participant { get; set; }
    public int CategoryID { get; set; }
    public Category? Category { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Confirmed";
}