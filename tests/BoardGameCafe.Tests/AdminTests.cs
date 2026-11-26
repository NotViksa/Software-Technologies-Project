
using System.Reflection;
using BoardGameCafe.Web.Application.Common;
using BoardGameCafe.Web.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameCafe.Tests;

public class AdminTests
{
    [Fact]
    public void AdminController_RequiresManagerRole()
    {
        var attribute = typeof(AdminController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(Roles.Manager, attribute.Roles);
    }

    [Fact]
    public void StaffReservations_RequiresStaffRole()
    {
        var attribute = typeof(StaffReservationsController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(Roles.Staff, attribute.Roles);
    }

    [Theory]
    [InlineData("CreateZone")]
    [InlineData("UpdateZone")]
    [InlineData("CreateTable")]
    [InlineData("UpdateTable")]
    [InlineData("UpdateUserRole")]
    [InlineData("Cafe")]
    public void AdminPostActions_RequireAntiForgeryToken(
        string methodName)
    {
        var method = typeof(AdminController)
            .GetMethods()
            .Single(x =>
                x.Name == methodName &&
                x.GetCustomAttribute<HttpPostAttribute>() != null);

        Assert.NotNull(
            method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }
}
