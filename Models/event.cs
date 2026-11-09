using RaceDay.Models;
namespace RaceDay.Api.Models;
public class Event
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RouteMapUrl { get; set; }
    public string? BannerImageUrl { get; set; } // ← ADDED
    public int OrganiserID { get; set; }
    public user? Organiser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}