using Microsoft.AspNetCore.Mvc;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Filters;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/users")]
[SessionAuth]   // every endpoint in here needs a login
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <remarks>Any logged-in user (Organiser or Participant). The user is found from the session.</remarks>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.UserID,
            Name = user.FullName,
            user.Email,
            user.Phone,
            Role = HttpContext.Session.GetString("Role")
        });
    }

    /// <summary>Updates the logged-in user's name and phone.</summary>
    /// <remarks>Any logged-in user. Email and role cannot be changed here.</remarks>
    /// <param name="dto">New name and an optional phone number.</param>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
    {
        int userId = HttpContext.Session.GetInt32("UserID")!.Value;
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.FullName = dto.Name;
        user.Phone = dto.Phone;
        await _db.SaveChangesAsync();

        return Ok(new { user.UserID, Name = user.FullName, user.Email, user.Phone });
    }
}