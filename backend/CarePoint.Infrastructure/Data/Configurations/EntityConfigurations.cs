using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CarePoint.Domain.Entities;
using CarePoint.Infrastructure.Identity;

namespace CarePoint.Infrastructure.Data.Configurations;

public class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.UserId).IsUnique();
        builder.Property(p => p.BloodType).HasMaxLength(10);
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.Gender).HasMaxLength(20);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.EmergencyContact).HasMaxLength(100);
    }
}

public class DoctorProfileConfiguration : IEntityTypeConfiguration<DoctorProfile>
{
    public void Configure(EntityTypeBuilder<DoctorProfile> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.UserId).IsUnique();
        builder.Property(d => d.ConsultationFee).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Bio).HasMaxLength(2000);
        builder.Property(d => d.PhoneNumber).HasMaxLength(20);
        builder.Property(d => d.Gender).HasMaxLength(20);
        builder.Property(d => d.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
    }
}

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.Name).IsUnique();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(500);
    }
}

public class DoctorSpecialtyConfiguration : IEntityTypeConfiguration<DoctorSpecialty>
{
    public void Configure(EntityTypeBuilder<DoctorSpecialty> builder)
    {
        builder.HasKey(ds => ds.Id);
        builder.HasIndex(ds => new { ds.DoctorProfileId, ds.SpecialtyId }).IsUnique();

        builder.HasOne(ds => ds.DoctorProfile)
            .WithMany(d => d.DoctorSpecialties)
            .HasForeignKey(ds => ds.DoctorProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ds => ds.Specialty)
            .WithMany(s => s.DoctorSpecialties)
            .HasForeignKey(ds => ds.SpecialtyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.PhoneNumber).HasMaxLength(20);
        builder.Property(c => c.City).HasMaxLength(100);
    }
}

public class ClinicDoctorConfiguration : IEntityTypeConfiguration<ClinicDoctor>
{
    public void Configure(EntityTypeBuilder<ClinicDoctor> builder)
    {
        builder.HasKey(cd => cd.Id);
        builder.HasIndex(cd => new { cd.ClinicId, cd.DoctorProfileId }).IsUnique();

        builder.HasOne(cd => cd.Clinic)
            .WithMany(c => c.ClinicDoctors)
            .HasForeignKey(cd => cd.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cd => cd.DoctorProfile)
            .WithMany(d => d.ClinicDoctors)
            .HasForeignKey(cd => cd.DoctorProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DoctorAvailabilityConfiguration : IEntityTypeConfiguration<DoctorAvailability>
{
    public void Configure(EntityTypeBuilder<DoctorAvailability> builder)
    {
        builder.HasKey(da => da.Id);
        builder.HasIndex(da => new { da.DoctorProfileId, da.DayOfWeek, da.StartTime }).IsUnique();
        builder.Property(da => da.DayOfWeek).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(da => da.DoctorProfile)
            .WithMany(d => d.Availabilities)
            .HasForeignKey(da => da.DoctorProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.DoctorProfileId, a.AppointmentDate, a.StartTime });
        builder.HasIndex(a => new { a.PatientProfileId, a.AppointmentDate });
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.Property(a => a.CancellationReason).HasMaxLength(500);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasOne(a => a.PatientProfile)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.DoctorProfile)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.HasKey(mr => mr.Id);
        // 1:1 relationship with Appointment
        builder.HasIndex(mr => mr.AppointmentId).IsUnique();
        builder.Property(mr => mr.Diagnosis).IsRequired().HasMaxLength(2000);
        builder.Property(mr => mr.Notes).HasMaxLength(4000);
        builder.Property(mr => mr.Treatment).HasMaxLength(4000);

        builder.HasOne(mr => mr.Appointment)
            .WithOne(a => a.MedicalRecord)
            .HasForeignKey<MedicalRecord>(mr => mr.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.HasOne(p => p.Appointment)
            .WithMany(a => a.Prescriptions)
            .HasForeignKey(p => p.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.DoctorProfile)
            .WithMany()
            .HasForeignKey(p => p.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PatientProfile)
            .WithMany()
            .HasForeignKey(p => p.PatientProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.HasKey(pi => pi.Id);
        builder.Property(pi => pi.MedicationName).IsRequired().HasMaxLength(200);
        builder.Property(pi => pi.Dosage).IsRequired().HasMaxLength(100);
        builder.Property(pi => pi.Frequency).IsRequired().HasMaxLength(100);
        builder.Property(pi => pi.Duration).HasMaxLength(100);
        builder.Property(pi => pi.Instructions).HasMaxLength(1000);

        builder.HasOne(pi => pi.Prescription)
            .WithMany(p => p.Items)
            .HasForeignKey(pi => pi.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MedicalDocumentConfiguration : IEntityTypeConfiguration<MedicalDocument>
{
    public void Configure(EntityTypeBuilder<MedicalDocument> builder)
    {
        builder.HasKey(md => md.Id);
        builder.Property(md => md.FileName).IsRequired().HasMaxLength(500);
        builder.Property(md => md.FileUrl).IsRequired().HasMaxLength(1000);
        builder.Property(md => md.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(md => md.DocumentType).HasMaxLength(100);

        builder.HasOne(md => md.PatientProfile)
            .WithMany(p => p.MedicalDocuments)
            .HasForeignKey(md => md.PatientProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(md => md.Appointment)
            .WithMany(a => a.MedicalDocuments)
            .HasForeignKey(md => md.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);
        builder.HasIndex(rt => rt.TokenHash).IsUnique();
        builder.HasIndex(rt => rt.UserId);
        builder.HasIndex(rt => rt.FamilyId);
        builder.Property(rt => rt.FamilyId).HasDefaultValueSql("NEWID()");
        builder.Property(rt => rt.TokenHash).HasColumnName("Token").IsRequired().HasMaxLength(64);
        builder.Property(rt => rt.ReplacedByTokenHash).HasColumnName("ReplacedByToken").HasMaxLength(64);
        builder.Property(rt => rt.CreatedByIp).HasMaxLength(50);
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasOne(u => u.PatientProfile)
            .WithOne()
            .HasForeignKey<PatientProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.DoctorProfile)
            .WithOne()
            .HasForeignKey<DoctorProfile>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Notifications)
            .WithOne()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
