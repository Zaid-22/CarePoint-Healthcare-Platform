using System.Reflection;
using CarePoint.API.Controllers;
using CarePoint.Application.DTOs.Medical;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace CarePoint.Tests;

public class ApiRouteAuthorizationTests
{
    [Fact]
    public void PrescriptionUpdate_IsExposedOnlyToDoctors()
    {
        AssertAuthorizedRoute<PrescriptionsController, HttpPutAttribute>(
            nameof(PrescriptionsController.Update), "{id:guid}", "Doctor");

        var bodyParameter = typeof(PrescriptionsController)
            .GetMethod(nameof(PrescriptionsController.Update))!
            .GetParameters()
            .Single(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() != null);
        Assert.Equal(typeof(UpdatePrescriptionDto), bodyParameter.ParameterType);
        Assert.Null(typeof(UpdatePrescriptionDto).GetProperty(nameof(CreatePrescriptionDto.AppointmentId)));
    }

    [Fact]
    public void PrescriptionRevisionHistoryIsAuthenticated()
    {
        var controllerAuthorization = typeof(PrescriptionsController)
            .GetCustomAttribute<AuthorizeAttribute>();
        var route = typeof(PrescriptionsController)
            .GetMethod(nameof(PrescriptionsController.GetRevisions))!
            .GetCustomAttribute<HttpGetAttribute>();

        Assert.NotNull(controllerAuthorization);
        Assert.Equal("{id:guid}/revisions", route?.Template);
    }

    [Theory]
    [InlineData(nameof(ClinicsController.Update), "PUT")]
    [InlineData(nameof(ClinicsController.Delete), "DELETE")]
    public void ClinicMutations_AreExposedOnlyToAdmins(string methodName, string verb)
    {
        var method = typeof(ClinicsController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Controller action {methodName} was not found.");
        var authorize = method.GetCustomAttribute<AuthorizeAttribute>();
        var route = method.GetCustomAttributes<HttpMethodAttribute>().Single();

        Assert.Equal("Admin", authorize?.Roles);
        Assert.Contains(verb, route.HttpMethods);
        Assert.Equal("{id:guid}", route.Template);
    }

    [Fact]
    public void DoctorClinicAssignments_AreExposedOnlyToAdmins()
    {
        AssertAuthorizedRoute<DoctorsController, HttpPutAttribute>(
            nameof(DoctorsController.AssignClinics), "{id:guid}/clinics", "Admin");
    }

    [Fact]
    public void AdminUserManagement_IsProtectedAtTheControllerBoundary()
    {
        var controllerType = typeof(AdminUsersController);
        var controllerAuthorization = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        var controllerRoute = controllerType.GetCustomAttribute<RouteAttribute>();
        var mutation = controllerType.GetMethod(nameof(AdminUsersController.SetDisabled))
            ?? throw new InvalidOperationException("Admin user state action was not found.");

        Assert.Equal("Admin", controllerAuthorization?.Roles);
        Assert.Equal("api/admin/users", controllerRoute?.Template);
        Assert.Equal("{id}/disabled", mutation.GetCustomAttribute<HttpPutAttribute>()?.Template);
    }

    private static void AssertAuthorizedRoute<TController, TRouteAttribute>(
        string methodName, string template, string role)
        where TRouteAttribute : HttpMethodAttribute
    {
        var method = typeof(TController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Controller action {methodName} was not found.");

        Assert.Equal(role, method.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.Equal(template, method.GetCustomAttribute<TRouteAttribute>()?.Template);
    }
}
