using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

public class RegisterDto   // notice: no Role field
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
public class UpdateProfileDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }
}