using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Filters;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EnrolmentsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Enrols the logged-in Participant into a category.</summary>
    /// <remarks>Participants only. Fails if the category doesn't exist, is full, or you are already enrolled.</remarks>
    /// <param name="dto">The ID of the category to join.</param>
    /// <response code="201">Enrolment created.</response>
    /// <response code="400">Category doesn't exist or is full.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    /// <response code="409">Already enrolled in this category.</response>
    [HttpPost("api/enrolments")]
    [SessionAuth("Participant")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolDto dto)
    {
        int userId = HttpContext.Session.GetInt32("UserID")!.Value;

        var category = await _db.Categories.FindAsync(dto.CategoryId);
        if (category == null)
            return BadRequest("Category does not exist.");

        if (await _db.EventEnrolments.AnyAsync(e =>
                e.ParticipantID == userId && e.CategoryID == dto.CategoryId))
            return Conflict("You are already enrolled in this category.");

        int taken = await _db.EventEnrolments.CountAsync(e => e.CategoryID == dto.CategoryId);
        if (taken >= category.MaxParticipants)
            return BadRequest("This category is full.");

        var enrolment = new EventEnrolment
        {
            ParticipantID = userId,
            CategoryID = dto.CategoryId
        };

        _db.EventEnrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return StatusCode(201, new
        {
            enrolment.EnrolmentID,
            enrolment.CategoryID,
            enrolment.EnrolmentDate,
            enrolment.Status
        });
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <remarks>Participants only. You only ever see your own enrolments.</remarks>
    /// <response code="200">Array of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("api/enrolments/me")]
    [SessionAuth("Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        int userId = HttpContext.Session.GetInt32("UserID")!.Value;

        var list = await _db.EventEnrolments
            .Where(e => e.ParticipantID == userId)
            .Select(e => new
            {
                e.EnrolmentID,
                e.Status,
                Participant = e.Participant!.FullName,
                ParticipantEmail = e.Participant.Email,
                Category = e.Category!.CategoryName,
                Result = _db.Results
 .Where(r => r.EnrolmentID == e.EnrolmentID)
 .Select(r => new { r.ResultID, r.FinishTime, r.Position, r.Status })
 .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Lists everyone enrolled in an event.</summary>
    /// <remarks>Organisers only, and only for events they created.</remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="200">Array of enrolments with participant details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("api/events/{eventId}/enrolments")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");

        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        if (ev.OrganiserID != userId)
            return StatusCode(403, "You can only view enrolments for your own events.");

        var list = await _db.EventEnrolments
            .Where(e => e.Category!.EventID == eventId)
            .Select(e => new
            {
                e.EnrolmentID,
                e.Status,
                Participant = e.Participant!.FullName,
                ParticipantEmail = e.Participant.Email,
                Category = e.Category!.CategoryName
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
    /// <remarks>Participants only, and only their own enrolments.</remarks>
    /// <param name="id">The enrolment's ID.</param>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or not your enrolment.</response>
    /// <response code="404">No enrolment with that ID.</response>
    [HttpDelete("api/enrolments/{id}")]
    [SessionAuth("Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.EventEnrolments.FindAsync(id);
        if (enrolment == null) return NotFound();

        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        if (enrolment.ParticipantID != userId)
            return StatusCode(403, "You can only cancel your own enrolments.");

        _db.EventEnrolments.Remove(enrolment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}