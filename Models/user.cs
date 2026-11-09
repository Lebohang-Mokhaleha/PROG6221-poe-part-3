using System.Data;

namespace RaceDay.Models
{
    public class user
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Phone { get; set; }          // ? means it can be empty
        public int RoleID { get; set; }
        public role? Role { get; set; }             // links to the Roles table
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

