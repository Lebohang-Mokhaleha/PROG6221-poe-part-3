using Microsoft.AspNetCore.Mvc;
using RaceDay.Controllers;
using RaceDay.DTOs;
using Xunit;

namespace RaceDay.Tests;

public class EventTests
{
    private static EventDto SampleDto() => new()
    {
        EventName = "New Event",
        EventDate = DateTime.Today.AddDays(10),
        Location = "Joburg"
    };

    [Fact]
    public async Task Organiser_CanCreateEvent_AndOwnsIt()
    {
        var db = TestHelpers.CreateDb();
        var (org, _, _, _) = TestHelpers.SeedBasics(db);
        var controller = TestHelpers.WithSession(new EventsController(db), org.UserID, "Organiser");

        var result = await controller.Create(SampleDto());

        Assert.Equal(201, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(org.UserID, db.Events.Single(e => e.EventName == "New Event").OrganiserID);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var db = TestHelpers.CreateDb();
        var (org, _, ev, _) = TestHelpers.SeedBasics(db);
        var controller = TestHelpers.WithSession(new EventsController(db), org.UserID, "Organiser");

        var result = await controller.Update(ev.EventID, SampleDto());

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("New Event", db.Events.Find(ev.EventID)!.EventName);
    }

    [Fact]
    public async Task Organiser_CannotUpdateSomeoneElsesEvent_Returns403()
    {
        var db = TestHelpers.CreateDb();
        var (_, _, ev, _) = TestHelpers.SeedBasics(db);
        var controller = TestHelpers.WithSession(new EventsController(db), 999, "Organiser"); // different organiser

        var result = await controller.Update(ev.EventID, SampleDto());

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var db = TestHelpers.CreateDb();
        var (org, _, ev, cat) = TestHelpers.SeedBasics(db);
        db.Categories.Remove(cat);   // remove the category first
        db.SaveChanges();
        var controller = TestHelpers.WithSession(new EventsController(db), org.UserID, "Organiser");

        var result = await controller.Delete(ev.EventID);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task UpdateMissingEvent_Returns404()
    {
        var db = TestHelpers.CreateDb();
        var (org, _, _, _) = TestHelpers.SeedBasics(db);
        var controller = TestHelpers.WithSession(new EventsController(db), org.UserID, "Organiser");

        var result = await controller.Update(9999, SampleDto());

        Assert.IsType<NotFoundResult>(result);
    }
}