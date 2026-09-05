using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using CarePoint.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarePoint.Tests;

public class DoctorSlotGenerationTests
{
    [Theory]
    [InlineData(23, 0, 23, 59, 30, 1)]
    [InlineData(22, 0, 23, 59, 60, 1)]
    [InlineData(9, 0, 10, 0, 30, 2)]
    [InlineData(23, 45, 23, 59, 30, 0)]
    public async Task SlotsStayWithinTheSameDayAndExcludePartialSlots(
        int startHour, int startMinute, int endHour, int endMinute, int duration, int expectedCount)
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var doctor = new DoctorProfile
        {
            UserId = "doctor", ApprovalStatus = DoctorApprovalStatus.Approved,
            LicenseNumber = "TEST-123", LicensingAuthority = "Test authority",
            LicenseExpiryDate = new DateTime(2027, 1, 1), CredentialsReviewedAt = new DateTime(2026, 1, 1)
        };
        var start = new TimeOnly(startHour, startMinute);
        var end = new TimeOnly(endHour, endMinute);
        context.Users.Add(new ApplicationUser { Id = doctor.UserId });
        context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfile = doctor, DayOfWeek = DayOfWeek.Monday,
            StartTime = start, EndTime = end, SlotDurationMinutes = duration
        });
        await context.SaveChangesAsync();
        var service = new DoctorService(context, null!, null!, new FixedClock(), null!, NullLogger<DoctorService>.Instance);

        var slots = await service.GetAvailableSlotsAsync(doctor.Id, new DateTime(2026, 8, 10));

        Assert.Equal(expectedCount, slots.Count);
        Assert.All(slots, slot =>
        {
            Assert.True(slot.StartTime >= start && slot.EndTime <= end);
            Assert.Equal(TimeSpan.FromMinutes(duration), slot.EndTime - slot.StartTime);
            Assert.True(slot.IsAvailable);
        });
    }

    private sealed class FixedClock : IClinicClock
    {
        public DateTime UtcNow => new(2026, 8, 1);
        public DateTime LocalNow => new(2026, 8, 1);
    }
}
