using FluentValidation;
using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.DTOs.Patients;

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
        RuleFor(x => x.SpecialtyIds)
            .NotEmpty()
            .When(x => x.Role == "Doctor")
            .WithMessage("At least one specialty is required for doctors.");
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

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
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

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentDto>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.DoctorProfileId).NotEmpty();
        RuleFor(x => x.AppointmentDate).NotEmpty();
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage("Start time must be before end time.");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class UpdateAppointmentStatusValidator : AbstractValidator<UpdateAppointmentStatusDto>
{
    public UpdateAppointmentStatusValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.CancellationReason).MaximumLength(500);
    }
}

public class CancelAppointmentValidator : AbstractValidator<CancelAppointmentDto>
{
    public CancelAppointmentValidator() =>
        RuleFor(x => x.CancellationReason).MaximumLength(500);
}

public class RescheduleAppointmentValidator : AbstractValidator<RescheduleAppointmentDto>
{
    public RescheduleAppointmentValidator()
    {
        RuleFor(x => x.NewAppointmentDate).NotEmpty();
        RuleFor(x => x.NewStartTime).LessThan(x => x.NewEndTime)
            .WithMessage("Start time must be before end time.");
    }
}

public class CreateDoctorValidator : AbstractValidator<CreateDoctorDto>
{
    public CreateDoctorValidator()
    {
        RuleFor(x => x.ConsultationFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SpecialtyIds).NotEmpty().WithMessage("At least one specialty is required.");
        RuleFor(x => x.Bio).MaximumLength(2000);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.Gender).MaximumLength(20);
    }
}

public class UpdateDoctorValidator : AbstractValidator<UpdateDoctorDto>
{
    public UpdateDoctorValidator()
    {
        RuleFor(x => x.ConsultationFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SpecialtyIds).NotEmpty().WithMessage("At least one specialty is required.");
        RuleFor(x => x.Bio).MaximumLength(2000);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.Gender).MaximumLength(20);
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
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.Treatment).MaximumLength(4000);
    }
}

public class UpdateMedicalRecordValidator : AbstractValidator<UpdateMedicalRecordDto>
{
    public UpdateMedicalRecordValidator()
    {
        RuleFor(x => x.Diagnosis).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.Treatment).MaximumLength(4000);
        RuleFor(x => x.ChangeReason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

public class CreatePrescriptionValidator : AbstractValidator<CreatePrescriptionDto>
{
    public CreatePrescriptionValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one medication is required.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MedicationName).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.Dosage).NotEmpty().MaximumLength(100);
            item.RuleFor(i => i.Frequency).NotEmpty().MaximumLength(100);
            item.RuleFor(i => i.Duration).MaximumLength(100);
            item.RuleFor(i => i.Instructions).MaximumLength(1000);
        });
    }
}

public class CreateSpecialtyValidator : AbstractValidator<CreateSpecialtyDto>
{
    public CreateSpecialtyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateClinicValidator : AbstractValidator<CreateClinicDto>
{
    public CreateClinicValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.City).MaximumLength(100);
    }
}

public class UpdatePatientValidator : AbstractValidator<UpdatePatientDto>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.BloodType).MaximumLength(10);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.Gender).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.EmergencyContact).MaximumLength(100);
    }
}
