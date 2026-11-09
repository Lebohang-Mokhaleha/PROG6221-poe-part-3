using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Tests;

// A fake session that stores data in a dictionary
public class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();
    public string Id => "test";
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _store.Keys;
    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken c = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken c = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}

public static class TestHelpers
{
    // A brand new empty database for every test
    public static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();   // also adds the 2 seeded roles
        return db;
    }

    // Gives a controller a session, optionally "logged in" as someone
    public static T WithSession<T>(T controller, int? userId = null, string? role = null)
        where T : ControllerBase
    {
        var http = new DefaultHttpContext { Session = new FakeSession() };
        if (userId != null) http.Session.SetInt32("UserID", userId.Value);
        if (role != null) http.Session.SetString("Role", role);
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    // Adds an organiser, a participant, an event and a category
    public static (User organiser, User participant, Event ev, Category cat) SeedBasics(AppDbContext db)
    {
        var organiser = new User { FullName = "Org One", Email = "org@test.com", PasswordHash = "x", RoleID = 1 };
        var participant = new User { FullName = "Runner One", Email = "run@test.com", PasswordHash = "x", RoleID = 2 };
        db.Users.AddRange(organiser, participant);
        db.SaveChanges();

        var ev = new Event
        {
            EventName = "Test Race",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            OrganiserID = organiser.UserID
        };
        db.Events.Add(ev);
        db.SaveChanges();

        var cat = new Category
        {
            EventID = ev.EventID,
            CategoryName = "10km",
            DistanceKm = 10,
            EntryFee = 100,
            MaxParticipants = 5
        };
        db.Categories.Add(cat);
        db.SaveChanges();

        return (organiser, participant, ev, cat);
    }
}