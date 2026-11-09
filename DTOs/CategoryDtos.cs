using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

public class CategoryDto
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.1, 500)]
    public decimal DistanceKm { get; set; }

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; }
}