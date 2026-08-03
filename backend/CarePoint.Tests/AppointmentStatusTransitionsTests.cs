using CarePoint.Domain.Common;
using CarePoint.Domain.Enums;

namespace CarePoint.Tests;

public class AppointmentStatusTransitionsTests
{
    [Theory]
    [InlineData(AppointmentStatus.Pending, AppointmentStatus.Accepted)]
    [InlineData(AppointmentStatus.Pending, AppointmentStatus.Rejected)]
    [InlineData(AppointmentStatus.Accepted, AppointmentStatus.InProgress)]
    [InlineData(AppointmentStatus.Accepted, AppointmentStatus.Completed)]
    [InlineData(AppointmentStatus.InProgress, AppointmentStatus.Completed)]
    public void DoctorTransitions_AllowExpectedMoves(AppointmentStatus current, AppointmentStatus requested)
    {
        Assert.True(AppointmentStatusTransitions.CanDoctorTransition(current, requested));
    }

    [Theory]
    [InlineData(AppointmentStatus.Cancelled, AppointmentStatus.Completed)]
    [InlineData(AppointmentStatus.Rejected, AppointmentStatus.InProgress)]
    [InlineData(AppointmentStatus.Completed, AppointmentStatus.Pending)]
    [InlineData(AppointmentStatus.NoShow, AppointmentStatus.Accepted)]
    public void TerminalStates_CannotBeReopened(AppointmentStatus current, AppointmentStatus requested)
    {
        Assert.False(AppointmentStatusTransitions.CanDoctorTransition(current, requested));
        Assert.False(AppointmentStatusTransitions.CanAdminTransition(current, requested));
    }

    [Theory]
    [InlineData(AppointmentStatus.Pending, AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.Accepted, AppointmentStatus.NoShow)]
    [InlineData(AppointmentStatus.InProgress, AppointmentStatus.Cancelled)]
    public void AdminTransitions_AllowOperationalOverrides(AppointmentStatus current, AppointmentStatus requested)
    {
        Assert.True(AppointmentStatusTransitions.CanAdminTransition(current, requested));
    }
}
