using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Models;
using RaceDay.Controllers;
using RaceDay.DTOs;
using RaceDay.Models;
using Xunit;

namespace RaceDay.Tests;

public class ResultTests
{
    private static ResultDto SampleResult() => new()
    {
        FinishTime = new TimeSpan(0, 45, 10),
        Position = 1,
        Status = "Finished"
    };

    [Fact]
    public async Task Organiser_CanCaptureResult()
    {
        var db = TestHelpers.CreateDb();
        var (org, runner, _, cat) = TestHelpers.SeedBasics(db);
        var enrolment = new EventEnrolment { ParticipantID = runner.UserID, CategoryID = cat.CategoryID };
        db.EventEnrolments.Add(enrolment);
        db.SaveChanges();
        var controller = TestHelpers.WithSession(new ResultsController(db), org.UserID, "Organiser");

        var result = await controller.Capture(enrolment.EnrolmentID, SampleResult());

        Assert.Equal(201, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(1, db.Results.Single().Position);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var db = TestHelpers.CreateDb();
        var (org, runner, _, cat) = TestHelpers.SeedBasics(db);
        var enrolment = new EventEnrolment { ParticipantID = runner.UserID, CategoryID = cat.CategoryID };
        db.EventEnrolments.Add(enrolment);
        db.SaveChanges();
        var controller = TestHelpers.WithSession(new ResultsController(db), org.UserID, "Organiser");

        await controller.Capture(enrolment.EnrolmentID, SampleResult());
        var result = await controller.Capture(enrolment.EnrolmentID, SampleResult());

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent_Returns403()
    {
        var db = TestHelpers.CreateDb();
        var (_, runner, _, cat) = TestHelpers.SeedBasics(db);
        var enrolment = new EventEnrolment { ParticipantID = runner.UserID, CategoryID = cat.CategoryID };
        db.EventEnrolments.Add(enrolment);
        db.SaveChanges();
        var controller = TestHelpers.WithSession(new ResultsController(db), 999, "Organiser");

        var result = await controller.Capture(enrolment.EnrolmentID, SampleResult());

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Participant_SeesOnlyTheirOwnResults()
    {
        var db = TestHelpers.CreateDb();
        var (_, runner, _, cat) = TestHelpers.SeedBasics(db);
        var enrolment = new EventEnrolment { ParticipantID = runner.UserID, CategoryID = cat.CategoryID };
        db.EventEnrolments.Add(enrolment);
        db.SaveChanges();
        db.Results.Add(new Result { EnrolmentID = enrolment.EnrolmentID, Position = 3, Status = "Finished" });
        db.SaveChanges();

        var mine = TestHelpers.WithSession(new ResultsController(db), runner.UserID, "Participant");
        var other = TestHelpers.WithSession(new ResultsController(db), 999, "Participant");

        var myList = (IEnumerable<object>)Assert.IsType<OkObjectResult>(await mine.GetMine()).Value!;
        var otherList = (IEnumerable<object>)Assert.IsType<OkObjectResult>(await other.GetMine()).Value!;

        Assert.Single(myList);
        Assert.Empty(otherList);
    }
}