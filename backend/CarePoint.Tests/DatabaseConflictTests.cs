using System.Net;
using System.Text.Json;
using CarePoint.API.Middleware;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarePoint.Tests;

public class DatabaseConflictTests
{
    [Fact]
    public async Task UnknownDatabaseUpdateFailureReturnsServerErrorWithoutLeakingDatabaseDetails()
    {
        const string databaseDetail = "Violation of UNIQUE KEY constraint IX_Specialties_Name";
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new DbUpdateException(databaseDetail),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("An unexpected error occurred.",
            response.RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain(databaseDetail, response.RootElement.ToString());
    }

    [Fact]
    public async Task ConcurrencyFailureStillReturnsConflictWithoutLeakingDatabaseDetails()
    {
        const string databaseDetail = "The row was updated by another transaction.";
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new DbUpdateConcurrencyException(databaseDetail),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
        Assert.Equal("This record was changed by another request. Reload it and try again.",
            response.RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain(databaseDetail, response.RootElement.ToString());
    }

    [Fact]
    public async Task SpecialtyCreateRejectsAnExistingNormalizedNameBeforeSaving()
    {
        await using var context = CreateContext();
        context.Specialties.Add(new Specialty { Name = "Cardiology" });
        await context.SaveChangesAsync();
        var service = new SpecialtyService(context);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            new CreateSpecialtyDto { Name = "  Cardiology  " }));

        Assert.Equal("A specialty with this name already exists.", exception.Message);
        Assert.Single(context.Specialties);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"database-conflict-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
