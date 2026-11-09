using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Models;
using RaceDay.Controllers;
using RaceDay.DTOs;
using RaceDay.Models;
using Xunit;

namespace RaceDay.Tests;

public class EnrolmentTests
{
    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var db = TestHelpers.CreateDb();
        var (_, runner, _, cat) = TestHelpers.SeedBasics(db);
        var controller = TestHelpers.WithSession(new EnrolmentsController(db), runner.UserID, "Participant");

        var result = await controller.Enrol(new EnrolDto { CategoryId = cat.CategoryID });

        Assert.Equal(201, Assert.IsType<ObjectResult>(result).StatusCode);
        var saved = db.EventEnrolments.Single();
        Assert.Equal(runner.UserID, saved.ParticipantID);
        Assert.Equal(cat.CategoryID, saved.CategoryID);
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        var db = TestHelpers.CreateDb();
        var (_, runner, _, cat) = TestHelpers.SeedBasics(db);
        var controller = TestHelpers.WithSession(new EnrolmentsController(db), runner.UserID, "Participant");
        var dto = new EnrolDto { CategoryId = cat.CategoryID };

        await controller.Enrol(dto);
        var result = await controller.Enrol(dto);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Enrol_MissingCategory_Returns400()
    {
        var db = TestHelpers.CreateDb();
        var (_, runner, _, _) = TestHelpers.SeedBasics(db);
        var controller = TestHelpers.WithSession(new EnrolmentsController(db), runner.UserID, "Participant");

        var result = await controller.Enrol(new EnrolDto { CategoryId = 9999 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment_Returns403()
    {
        var db = TestHelpers.CreateDb();
        var (_, runner, _, cat) = TestHelpers.SeedBasics(db);
        var enrolment = new EventEnrolment { ParticipantID = runner.UserID, CategoryID = cat.CategoryID };
        db.EventEnrolments.Add(enrolment);
        db.SaveChanges();
        var controller = TestHelpers.WithSession(new EnrolmentsController(db), 999, "Participant"); // someone else

        var result = await controller.Cancel(enrolment.EnrolmentID);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent_Returns403()
    {
        var db = TestHelpers.CreateDb();
        var (_, _, ev, _) = TestHelpers.SeedBasics(db);
        var controller = TestHelpers.WithSession(new EnrolmentsController(db), 999, "Organiser");

        var result = await controller.GetForEvent(ev.EventID);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }
}