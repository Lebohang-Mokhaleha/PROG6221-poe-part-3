using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Filters;

public class SessionAuthAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public SessionAuthAttribute(string? role = null)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.Session.GetInt32("UserID");
        var role = context.HttpContext.Session.GetString("Role");

        // Not logged in
        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult("Please log in first.");
            return;
        }

        // Logged in, but wrong role
        if (_role != null && role != _role)
        {
            context.Result = new ObjectResult("You don't have permission to do this.")
            {
                StatusCode = 403
            };
        }
    }
}