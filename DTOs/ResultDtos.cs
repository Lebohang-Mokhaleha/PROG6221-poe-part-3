using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

public class ResultDto
{
    public TimeSpan? FinishTime { get; set; }   // e.g. "01:45:10"

    [Range(1, 100000)]
    public int? Position { get; set; }

    [Required]
    [RegularExpression("^(Finished|DNF|DNS|DSQ)$",
        ErrorMessage = "Status must be Finished, DNF, DNS or DSQ.")]
    public string Status { get; set; } = "Finished";
}