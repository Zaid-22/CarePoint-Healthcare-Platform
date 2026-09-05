using CarePoint.Domain.Entities;
using CarePoint.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarePoint.Tests;

public class ReferenceDataSeedingTests
{
    [Fact]
    public async Task RepeatedSeedingPreservesDeactivationAndCustomDescription()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Specialties.Add(new Specialty { Name = "Cardiology", IsActive = false, Description = "Clinic-specific description" });
        await context.SaveChangesAsync();

        await DatabaseSeeder.SeedSpecialtiesAsync(context);
        await DatabaseSeeder.SeedSpecialtiesAsync(context);

        var cardiology = await context.Specialties.SingleAsync(s => s.Name == "Cardiology");
        Assert.False(cardiology.IsActive);
        Assert.Equal("Clinic-specific description", cardiology.Description);
        Assert.True((await context.Specialties.SingleAsync(s => s.Name == "Dermatology")).IsActive);
        Assert.Equal(24, await context.Specialties.CountAsync());
    }
}
