using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

public class EventDto
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public string? RouteMapUrl { get; set; }
}