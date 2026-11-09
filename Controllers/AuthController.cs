using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Registers a new Participant account.</summary>
    /// <remarks>
    /// The role is always Participant. Organisers are created by the system, not through this endpoint.
    /// </remarks>
    /// <param name="dto">Name, email and a password of at least 6 characters.</param>
    /// <response code="201">Account created. Returns the user (no password).</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">Email already in use.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict("Email already in use.");

        var user = new user
        {
            FullName = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleID = 2   // Participant, always set by the server
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(201, new
        {
            user.UserID,
            Name = user.FullName,
            user.Email,
            Role = "Participant"
        });
    }

    /// <summary>Logs in and starts a session.</summary>
    /// <remarks>The session stores the user's ID and role for all later requests.</remarks>
    /// <param name="dto">Email and password.</param>
    /// <response code="200">Logged in. Returns the user and their role.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        HttpContext.Session.SetInt32("UserID", user.UserID);
        HttpContext.Session.SetString("Role", user.Role!.RoleName);

        return Ok(new
        {
            user.UserID,
            Name = user.FullName,
            user.Email,
            Role = user.Role.RoleName
        });
    }

    /// <summary>Logs out and clears the session.</summary>
    /// <remarks>Safe to call even if you are not logged in.</remarks>
    /// <response code="204">Session cleared.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}