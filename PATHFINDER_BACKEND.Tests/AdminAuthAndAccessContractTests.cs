using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.Controllers;
using Xunit;

public class AdminAuthAndAccessContractTests
{
    [Fact]
    public void AdminAuthController_HasExpectedRoute()
    {
        var routeAttr = typeof(AdminAuthController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(routeAttr);
        Assert.Equal("api/admin/auth", routeAttr!.Template);
    }

    [Fact]
    public void AdminAuthController_Login_HasExpectedRouteAndNoAuthorizeAttribute()
    {
        var loginMethod = typeof(AdminAuthController).GetMethod("Login");
        Assert.NotNull(loginMethod);

        var postAttr = loginMethod!.GetCustomAttribute<HttpPostAttribute>();
        var authorizeAttr = loginMethod.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(postAttr);
        Assert.Equal("login", postAttr!.Template);
        Assert.Null(authorizeAttr);
    }

    [Fact]
    public void AdminProtectedController_IsRestrictedToAdminRole()
    {
        var authorizeAttr = typeof(AdminProtectedController).GetCustomAttribute<AuthorizeAttribute>();
        var routeAttr = typeof(AdminProtectedController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(authorizeAttr);
        Assert.Equal("ADMIN", authorizeAttr!.Roles);
        Assert.NotNull(routeAttr);
        Assert.Equal("api/admin", routeAttr!.Template);
    }

    [Fact]
    public void StudentAndCompanyProtectedControllers_AreRoleRestricted()
    {
        var studentAuth = typeof(StudentProtectedController).GetMethod("Me")!.GetCustomAttribute<AuthorizeAttribute>();
        var companyAuth = typeof(CompanyProtectedController).GetMethod("Me")!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(studentAuth);
        Assert.Equal("STUDENT", studentAuth!.Roles);
        Assert.NotNull(companyAuth);
        Assert.Equal("COMPANY", companyAuth!.Roles);
    }
}
