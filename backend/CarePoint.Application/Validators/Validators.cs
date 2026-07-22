using FluentValidation;
using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.DTOs.Medical;

namespace CarePoint.Application.Validators;

public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Passwords do not match.");
        RuleFor(x => x.Role).Must(r => r == "Patient" || r == "Doctor").WithMessage("Role must be 'Patient' or 'Doctor'.");
    }
}

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentDto>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.DoctorProfileId).NotEmpty();
        RuleFor(x => x.AppointmentDate).GreaterThan(DateTime.UtcNow.Date).WithMessage("Cannot book appointments in the past.");
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage("Start time must be before end time.");
    }
}

public class CreateDoctorValidator : AbstractValidator<CreateDoctorDto>
{
    public CreateDoctorValidator()
    {
        RuleFor(x => x.ConsultationFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SpecialtyIds).NotEmpty().WithMessage("At least one specialty is required.");
    }
}

public class CreateAvailabilityValidator : AbstractValidator<CreateAvailabilityDto>
{
    public CreateAvailabilityValidator()
    {
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage("Start time must be before end time.");
        RuleFor(x => x.SlotDurationMinutes).InclusiveBetween(10, 120);
    }
}

public class CreateMedicalRecordValidator : AbstractValidator<CreateMedicalRecordDto>
{
    public CreateMedicalRecordValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Diagnosis).NotEmpty().MaximumLength(2000);
    }
}

public class CreatePrescriptionValidator : AbstractValidator<CreatePrescriptionDto>
{
    public CreatePrescriptionValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one medication is required.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MedicationName).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.Dosage).NotEmpty().MaximumLength(100);
            item.RuleFor(i => i.Frequency).NotEmpty().MaximumLength(100);
        });
    }
}

public class CreateSpecialtyValidator : AbstractValidator<CreateSpecialtyDto>
{
    public CreateSpecialtyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateClinicValidator : AbstractValidator<CreateClinicDto>
{
    public CreateClinicValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
