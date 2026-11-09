using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Controllers;
using RaceDay.DTOs;
using Xunit;

namespace RaceDay.Tests;

public class AuthTests
{
    [Fact]
    public async Task Register_NewEmail_Returns201_AndAlwaysCreatesParticipant()
    {
        var db = TestHelpers.CreateDb();
        var controller = TestHelpers.WithSession(new AuthController(db));

        var result = await controller.Register(new RegisterDto
        {
            Name = "New User",
            Email = "new@test.com",
            Password = "Pass123!"
        });

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, obj.StatusCode);

        var saved = db.Users.Single(u => u.Email == "new@test.com");
        Assert.Equal(2, saved.RoleID);                  // 2 = Participant
        Assert.NotEqual("Pass123!", saved.PasswordHash); // password is hashed
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var db = TestHelpers.CreateDb();
        var controller = TestHelpers.WithSession(new AuthController(db));
        var dto = new RegisterDto { Name = "A", Email = "dup@test.com", Password = "Pass123!" };

        await controller.Register(dto);
        var result = await controller.Register(dto);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Login_CorrectPassword_Returns200_AndStoresSession()
    {
        var db = TestHelpers.CreateDb();
        var controller = TestHelpers.WithSession(new AuthController(db));
        await controller.Register(new RegisterDto { Name = "A", Email = "a@test.com", Password = "Pass123!" });

        var result = await controller.Login(new LoginDto { Email = "a@test.com", Password = "Pass123!" });

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(controller.HttpContext.Session.GetInt32("UserID"));
        Assert.Equal("Participant", controller.HttpContext.Session.GetString("Role"));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var db = TestHelpers.CreateDb();
        var controller = TestHelpers.WithSession(new AuthController(db));
        await controller.Register(new RegisterDto { Name = "A", Email = "a@test.com", Password = "Pass123!" });

        var result = await controller.Login(new LoginDto { Email = "a@test.com", Password = "WrongPass" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}