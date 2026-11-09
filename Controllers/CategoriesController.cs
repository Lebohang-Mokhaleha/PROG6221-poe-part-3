using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Filters;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Lists all categories for one event.</summary>
    /// <remarks>Public, no login needed.</remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="200">Array of categories.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("api/events/{eventId}/categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound("Event not found.");

        var categories = await _db.Categories
            .Where(c => c.EventID == eventId)
            .Select(c => new
            {
                c.CategoryID,
                c.CategoryName,
                c.DistanceKm,
                c.EntryFee,
                c.MaxParticipants
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Adds a category to an event.</summary>
    /// <remarks>Organisers only, and only for events they created.</remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <param name="dto">Category name, distance in km, entry fee and max participants.</param>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpPost("api/events/{eventId}/categories")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryDto dto)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");

        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        if (ev.OrganiserID != userId)
            return StatusCode(403, "You can only manage your own events.");

        var category = new Category
        {
            EventID = eventId,
            CategoryName = dto.CategoryName,
            DistanceKm = dto.DistanceKm,
            EntryFee = dto.EntryFee,
            MaxParticipants = dto.MaxParticipants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(201, new
        {
            category.CategoryID,
            category.EventID,
            category.CategoryName,
            category.DistanceKm,
            category.EntryFee,
            category.MaxParticipants
        });
    }

    /// <summary>Updates a category.</summary>
    /// <remarks>Organisers only, and only for events they created.</remarks>
    /// <param name="id">The category's ID.</param>
    /// <param name="dto">The new category details.</param>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No category with that ID.</response>
    [HttpPut("api/categories/{id}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryDto dto)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null) return NotFound();

        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        if (category.Event!.OrganiserID != userId)
            return StatusCode(403, "You can only manage your own events.");

        category.CategoryName = dto.CategoryName;
        category.DistanceKm = dto.DistanceKm;
        category.EntryFee = dto.EntryFee;
        category.MaxParticipants = dto.MaxParticipants;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            category.CategoryID,
            category.CategoryName,
            category.DistanceKm,
            category.EntryFee,
            category.MaxParticipants
        });
    }

    /// <summary>Deletes a category.</summary>
    /// <remarks>Organisers only, and only for events they created.</remarks>
    /// <param name="id">The category's ID.</param>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No category with that ID.</response>
    [HttpDelete("api/categories/{id}")]
    [SessionAuth("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null) return NotFound();

        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        if (category.Event!.OrganiserID != userId)
            return StatusCode(403, "You can only manage your own events.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}