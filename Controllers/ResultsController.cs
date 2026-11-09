using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Filters;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Captures a race result for an enrolment.</summary>
    /// <remarks>Organisers only, and only for events they created. One result per enrolment.</remarks>
    /// <param name="id">The enrolment's ID.</param>
    /// <param name="dto">Finish time (hh:mm:ss), position, and status (Finished, DNF, DNS or DSQ).</param>
    /// <response code="201">Result created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No enrolment with that ID.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("api/enrolments/{id}/result")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int id, ResultDto dto)
    {
        var enrolment = await _db.EventEnrolments
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (enrolment == null) return NotFound("Enrolment not found.");

        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        if (enrolment.Category!.Event!.OrganiserID != userId)
            return StatusCode(403, "You can only capture results for your own events.");

        if (await _db.Results.AnyAsync(r => r.EnrolmentID == id))
            return Conflict("A result already exists for this enrolment. Use PUT to update it.");

        var result = new Result
        {
            EnrolmentID = id,
            FinishTime = dto.FinishTime,
            Position = dto.Position,
            Status = dto.Status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(201, new
        {
            result.ResultID,
            result.EnrolmentID,
            result.FinishTime,
            result.Position,
            result.Status
        });
    }

    /// <summary>Updates an existing result.</summary>
    /// <remarks>Organisers only, and only for events they created.</remarks>
    /// <param name="id">The result's ID.</param>
    /// <param name="dto">The new finish time, position and status.</param>
    /// <response code="200">The updated result.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No result with that ID.</response>
    [HttpPut("api/results/{id}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, ResultDto dto)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(r => r.ResultID == id);
        if (result == null) return NotFound();

        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        if (result.Enrolment!.Category!.Event!.OrganiserID != userId)
            return StatusCode(403, "You can only update results for your own events.");

        result.FinishTime = dto.FinishTime;
        result.Position = dto.Position;
        result.Status = dto.Status;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            result.ResultID,
            result.EnrolmentID,
            result.FinishTime,
            result.Position,
            result.Status
        });
    }

    /// <summary>Shows the logged-in Participant's own results history.</summary>
    /// <remarks>Participants only. You only ever see your own results.</remarks>
    /// <response code="200">Array of results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("api/results/me")]
    [SessionAuth("Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        int userId = HttpContext.Session.GetInt32("UserID")!.Value;

        var list = await _db.Results
            .Where(r => r.Enrolment!.ParticipantID == userId)
            .Select(r => new
            {
                r.ResultID,
                Event = r.Enrolment!.Category!.Event!.EventName,
                EventDate = r.Enrolment.Category.Event.EventDate,
                Category = r.Enrolment.Category.CategoryName,
                r.FinishTime,
                r.Position,
                r.Status
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Public leaderboard for an event.</summary>
    /// <remarks>Public, no login needed. Sorted by position, with unplaced results last.</remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="200">Array of results.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("api/events/{eventId}/results")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound("Event not found.");

        var list = await _db.Results
            .Where(r => r.Enrolment!.Category!.EventID == eventId)
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .Select(r => new
            {
                Category = r.Enrolment!.Category!.CategoryName,
                Participant = r.Enrolment.Participant!.FullName,
                r.FinishTime,
                r.Position,
                r.Status
            })
            .ToListAsync();

        return Ok(list);
    }
}