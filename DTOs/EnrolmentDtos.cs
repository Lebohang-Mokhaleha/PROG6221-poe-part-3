using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

public class EnrolDto
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}