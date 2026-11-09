namespace RaceDay.Api.Models;

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public EventEnrolment? Enrolment { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";   // was missing in your SQL
    public DateTime CapturedAt { get; set; } = DateTime.Now;
}