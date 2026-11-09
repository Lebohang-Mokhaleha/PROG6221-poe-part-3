using RaceDay.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Filters;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBlobService _blobs;
    public EventsController(AppDbContext db, IBlobService blobs)
    {
        _db = db;
        _blobs = blobs;
    }


    /// <summary>Lists all upcoming events.</summary>
    /// <remarks>Public, no login needed. Only events from today onwards are returned.</remarks>
    /// <response code="200">Array of upcoming events.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events
            .Where(e => e.EventDate >= DateTime.Today)
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventID,
                e.EventName,
                e.EventDate,
                e.Location,
                e.Description,
                e.RouteMapUrl
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Gets the full details of one event.</summary>
    /// <remarks>Public, no login needed.</remarks>
    /// <param name="id">The event's ID.</param>
    /// <response code="200">The event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOne(int id)
    {
        var ev = await _db.Events
            .Where(e => e.EventID == id)
            .Select(e => new
            {
                e.EventID,
                e.EventName,
                e.EventDate,
                e.Location,
                e.Description,
                e.BannerImageUrl,
                Organiser = e.Organiser!.FullName

            })
            .FirstOrDefaultAsync();

        if (ev == null) return NotFound();
        return Ok(ev);
    }

    /// <summary>Creates a new event.</summary>
    /// <remarks>Organisers only. The event is linked to the logged-in Organiser automatically.</remarks>
    /// <param name="dto">Event name, date, location, and optional description and route map link.</param>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in, but not an Organiser.</response>
    [HttpPost]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventDto dto)
    {
        int userId = HttpContext.Session.GetInt32("UserID")!.Value;

        var ev = new Event
        {
            EventName = dto.EventName,
            EventDate = dto.EventDate,
            Location = dto.Location,
            Description = dto.Description,
            RouteMapUrl = dto.RouteMapUrl,
            OrganiserID = userId
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return StatusCode(201, new { ev.EventID, ev.EventName, ev.EventDate, ev.Location });
    }

    /// <summary>Updates an event.</summary>
    /// <remarks>Organisers only, and only the Organiser who created the event.</remarks>
    /// <param name="id">The event's ID.</param>
    /// <param name="dto">The new event details.</param>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpPut("{id}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();

        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        if (ev.OrganiserID != userId)
            return StatusCode(403, "You can only manage your own events.");

        ev.EventName = dto.EventName;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location;
        ev.Description = dto.Description;
        ev.RouteMapUrl = dto.RouteMapUrl;
        await _db.SaveChangesAsync();

        return Ok(new { ev.EventID, ev.EventName, ev.EventDate, ev.Location });
    }

    /// <summary>Deletes an event.</summary>
    /// <remarks>Organisers only, and only the Organiser who created the event.</remarks>
    /// <param name="id">The event's ID.</param>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpDelete("{id}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();

        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        if (ev.OrganiserID != userId)
            return StatusCode(403, "You can only manage your own events.");

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
    /// <summary>Organiser's own events, including past ones.</summary>
    [HttpGet("my")]
    [SessionAuth("Organiser")]
    public async Task<IActionResult> GetMyEvents()
    {
        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        var events = await _db.Events
        .Where(e => e.OrganiserID == userId)
        .OrderByDescending(e => e.EventDate)
        .Select(e => new
        {
            e.EventID,
            e.EventName,
            e.EventDate,
            e.Location,
            e.Description,
            e.RouteMapUrl,
            e.BannerImageUrl,
            e.OrganiserID,
            e.CreatedAt
        })
        .ToListAsync();
        return Ok(events);
    }
    /// <summary>Uploads a banner image for one of the organiser's events.</summary>
    [HttpPost("{id:int}/banner")]
    [SessionAuth("Organiser")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound("Event not found.");
        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        if (ev.OrganiserID != userId)
            return StatusCode(403, "You can only upload banners for your own events.");
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
        var fileName = $"event-{id}-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        ev.BannerImageUrl = await _blobs.UploadAsync(file, fileName);
        await _db.SaveChangesAsync();
        return Ok(new { bannerImageUrl = ev.BannerImageUrl });
    }

}