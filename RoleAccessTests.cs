using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Filters;
using Xunit;

namespace RaceDay.Tests;

public class RoleAccessTests
{
    private static AuthorizationFilterContext MakeContext(int? userId, string? role)
    {
        var http = new DefaultHttpContext { Session = new FakeSession() };
        if (userId != null) http.Session.SetInt32("UserID", userId.Value);
        if (role != null) http.Session.SetString("Role", role);

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void NotLoggedIn_Returns401()
    {
        var ctx = MakeContext(null, null);
        new SessionAuthAttribute("Organiser").OnAuthorization(ctx);

        Assert.IsType<UnauthorizedObjectResult>(ctx.Result);
    }

    [Fact]
    public void Participant_OnOrganiserEndpoint_Returns403()
    {
        var ctx = MakeContext(1, "Participant");
        new SessionAuthAttribute("Organiser").OnAuthorization(ctx);

        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public void Organiser_OnParticipantEndpoint_Returns403()
    {
        var ctx = MakeContext(1, "Organiser");
        new SessionAuthAttribute("Participant").OnAuthorization(ctx);

        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public void Organiser_OnOrganiserEndpoint_IsAllowed()
    {
        var ctx = MakeContext(1, "Organiser");
        new SessionAuthAttribute("Organiser").OnAuthorization(ctx);

        Assert.Null(ctx.Result);   // no result = request carries on
    }

    [Fact]
    public void AnyLoggedInUser_OnOpenEndpoint_IsAllowed()
    {
        var ctx = MakeContext(1, "Participant");
        new SessionAuthAttribute().OnAuthorization(ctx);

        Assert.Null(ctx.Result);
    }
}