using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Infrastructure.Identity;

namespace CarePoint.Infrastructure.Data;

/// <summary>
/// Seeds default roles, admin user, clinical specialties, clinics, sample approved doctors, and sample patients.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Seed roles
        string[] roles = { "Admin", "Doctor", "Patient" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed default admin
        const string adminEmail = "admin@carepoint.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Admin",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // 3. Seed Clinical Specialties
        var seededSpecialties = await SeedSpecialtiesAsync(dbContext);

        // 4. Seed Clinics
        var seededClinics = await SeedClinicsAsync(dbContext);

        // 5. Seed Sample Doctors with Specialties and Availability
        await SeedDoctorsAsync(userManager, dbContext, seededSpecialties, seededClinics);

        // 6. Seed Sample Patient
        await SeedPatientAsync(userManager, dbContext);
    }

    public static async Task<Dictionary<string, Specialty>> SeedSpecialtiesAsync(ApplicationDbContext context)
    {
        var defaultSpecialties = new (string Name, string Description)[]
        {
            ("Cardiology", "Heart and cardiovascular system health, hypertension, and heart disease management."),
            ("Dermatology", "Skin, hair, nail conditions, cosmetic procedures, and dermatological care."),
            ("Neurology", "Brain, spinal cord, nerve disorders, chronic headaches, and neurological care."),
            ("Pediatrics", "Child and adolescent healthcare, growth monitoring, and pediatric wellness."),
            ("Orthopedics", "Bones, joints, ligaments, tendons, and musculoskeletal care & surgery."),
            ("General Medicine", "Comprehensive adult medical care, preventative health, and primary care management."),
            ("Psychiatry", "Mental health, behavioral conditions, therapy, and psychiatric management."),
            ("Ophthalmology", "Eye care, vision testing, eye surgery, and ophthalmic treatments."),
            ("Obstetrics & Gynecology", "Women's reproductive health, pregnancy care, and gynecological wellness."),
            ("ENT / Otolaryngology", "Ear, nose, throat, head, and neck medical & surgical care."),
            ("Endocrinology", "Hormonal imbalances, diabetes care, and thyroid disorder management."),
            ("Oncology", "Cancer diagnosis, tumor care, chemotherapy, and oncology treatment."),
            ("Gastroenterology", "Digestive system disorders, liver health, stomach and intestinal medical care."),
            ("Pulmonology", "Lung disease, asthma, respiratory health, and chronic pulmonary conditions."),
            ("Nephrology", "Kidney function, renal health, hypertension, and dialysis management."),
            ("Urology", "Urinary tract disorders, male reproductive system, and urological health."),
            ("Rheumatology", "Arthritis, autoimmune diseases, joint pain, and connective tissue disorders."),
            ("Radiology", "Diagnostic imaging, MRI, CT scans, ultrasound, and radiologic consultation."),
            ("Anesthesiology", "Pain management, preoperative care, and procedural anesthesia."),
            ("Hematology", "Blood disorders, anemia, coagulation, and lymphatic health."),
            ("Emergency Medicine", "Acute care, urgent medical conditions, and trauma response."),
            ("Dental Care", "Oral health, teeth cleaning, restorative dentistry, and gum care."),
            ("Physical Therapy", "Rehabilitation, movement therapy, injury recovery, and physical wellness."),
            ("Nutrition & Dietetics", "Dietary management, nutritional counseling, and metabolic wellness.")
        };

        var specialtyMap = new Dictionary<string, Specialty>();

        foreach (var spec in defaultSpecialties)
        {
            var existing = await context.Specialties.FirstOrDefaultAsync(s => s.Name == spec.Name);
            if (existing == null)
            {
                existing = new Specialty
                {
                    Name = spec.Name,
                    Description = spec.Description,
                    IsActive = true
                };
                context.Specialties.Add(existing);
            }
            else
            {
                // Ensure active and description updated if missing
                if (string.IsNullOrEmpty(existing.Description) && !string.IsNullOrEmpty(spec.Description))
                {
                    existing.Description = spec.Description;
                }
                existing.IsActive = true;
            }
            specialtyMap[spec.Name] = existing;
        }

        await context.SaveChangesAsync();
        return specialtyMap;
    }

    private static async Task<List<Clinic>> SeedClinicsAsync(ApplicationDbContext context)
    {
        var defaultClinics = new[]
        {
            new Clinic { Name = "CarePoint Central Hospital", Address = "100 Medical Center Blvd", City = "Amman", PhoneNumber = "+962 6 500 1000", IsActive = true },
            new Clinic { Name = "Westside Health & Care Center", Address = "45 West Avenue, Suite 200", City = "Amman", PhoneNumber = "+962 6 500 2000", IsActive = true },
            new Clinic { Name = "Metro Specialist Clinic", Address = "88 Grand Plaza Way", City = "Zarqa", PhoneNumber = "+962 5 300 1000", IsActive = true }
        };

        var clinics = new List<Clinic>();

        foreach (var c in defaultClinics)
        {
            var existing = await context.Clinics.FirstOrDefaultAsync(x => x.Name == c.Name);
            if (existing == null)
            {
                context.Clinics.Add(c);
                clinics.Add(c);
            }
            else
            {
                clinics.Add(existing);
            }
        }

        await context.SaveChangesAsync();
        return clinics;
    }

    private static async Task SeedDoctorsAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        Dictionary<string, Specialty> specialtyMap,
        List<Clinic> clinics)
    {
        var sampleDoctors = new[]
        {
            new
            {
                Email = "dr.smith@carepoint.com",
                FirstName = "Sarah",
                LastName = "Smith",
                Bio = "Board-certified Cardiologist with over 12 years of experience in cardiovascular care and preventive cardiology.",
                Fee = 75.00m,
                Phone = "+962 7 9100 1111",
                Gender = "Female",
                Picture = "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=300&q=80",
                Specialties = new[] { "Cardiology", "General Medicine" },
                ClinicIdx = 0
            },
            new
            {
                Email = "dr.chen@carepoint.com",
                FirstName = "Michael",
                LastName = "Chen",
                Bio = "Dermatologist specializing in clinical dermatology, skin health, and aesthetic procedure care.",
                Fee = 65.00m,
                Phone = "+962 7 9200 2222",
                Gender = "Male",
                Picture = "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=300&q=80",
                Specialties = new[] { "Dermatology" },
                ClinicIdx = 1
            },
            new
            {
                Email = "dr.patel@carepoint.com",
                FirstName = "Aisha",
                LastName = "Patel",
                Bio = "Pediatrician dedicated to compassionate child healthcare, growth monitoring, and adolescent medicine.",
                Fee = 55.00m,
                Phone = "+962 7 9300 3333",
                Gender = "Female",
                Picture = "https://images.unsplash.com/photo-1594824813566-88855ce78905?auto=format&fit=crop&w=300&q=80",
                Specialties = new[] { "Pediatrics", "General Medicine" },
                ClinicIdx = 0
            },
            new
            {
                Email = "dr.wilson@carepoint.com",
                FirstName = "James",
                LastName = "Wilson",
                Bio = "Orthopedic surgeon focused on sports injuries, joint replacement, and rehabilitation therapy.",
                Fee = 90.00m,
                Phone = "+962 7 9400 4444",
                Gender = "Male",
                Picture = "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&w=300&q=80",
                Specialties = new[] { "Orthopedics" },
                ClinicIdx = 2
            },
            new
            {
                Email = "dr.rostova@carepoint.com",
                FirstName = "Elena",
                LastName = "Rostova",
                Bio = "Neurologist specializing in movement disorders, migraine therapy, and neuro-diagnostic consultation.",
                Fee = 85.00m,
                Phone = "+962 7 9500 5555",
                Gender = "Female",
                Picture = "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=300&q=80",
                Specialties = new[] { "Neurology" },
                ClinicIdx = 1
            }
        };

        foreach (var docInfo in sampleDoctors)
        {
            var user = await userManager.FindByEmailAsync(docInfo.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = docInfo.Email,
                    Email = docInfo.Email,
                    FirstName = docInfo.FirstName,
                    LastName = docInfo.LastName,
                    EmailConfirmed = true
                };

                var createRes = await userManager.CreateAsync(user, "Doctor@123!");
                if (createRes.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Doctor");
                }
            }

            var profile = await context.DoctorProfiles
                .Include(d => d.DoctorSpecialties)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (profile == null)
            {
                profile = new DoctorProfile
                {
                    UserId = user.Id,
                    Bio = docInfo.Bio,
                    ConsultationFee = docInfo.Fee,
                    PhoneNumber = docInfo.Phone,
                    Gender = docInfo.Gender,
                    ProfilePictureUrl = docInfo.Picture,
                    ApprovalStatus = DoctorApprovalStatus.Approved
                };

                context.DoctorProfiles.Add(profile);
                await context.SaveChangesAsync();

                // Add Doctor Specialties
                foreach (var specName in docInfo.Specialties)
                {
                    if (specialtyMap.TryGetValue(specName, out var specialty))
                    {
                        context.DoctorSpecialties.Add(new DoctorSpecialty
                        {
                            DoctorProfileId = profile.Id,
                            SpecialtyId = specialty.Id
                        });
                    }
                }

                // Add Clinic Link
                if (clinics.Count > docInfo.ClinicIdx)
                {
                    context.ClinicDoctors.Add(new ClinicDoctor
                    {
                        DoctorProfileId = profile.Id,
                        ClinicId = clinics[docInfo.ClinicIdx].Id
                    });
                }

                // Add Doctor Weekly Availability (Sunday through Saturday, 9:00 AM - 17:00 PM)
                var days = Enum.GetValues<DayOfWeek>();
                foreach (var day in days)
                {
                    context.DoctorAvailabilities.Add(new DoctorAvailability
                    {
                        DoctorProfileId = profile.Id,
                        DayOfWeek = day,
                        StartTime = new TimeOnly(9, 0),
                        EndTime = new TimeOnly(17, 0),
                        SlotDurationMinutes = 30
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedPatientAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        const string patientEmail = "patient@carepoint.com";
        var patientUser = await userManager.FindByEmailAsync(patientEmail);
        if (patientUser == null)
        {
            patientUser = new ApplicationUser
            {
                UserName = patientEmail,
                Email = patientEmail,
                FirstName = "John",
                LastName = "Doe",
                EmailConfirmed = true
            };

            var createRes = await userManager.CreateAsync(patientUser, "Patient@123!");
            if (createRes.Succeeded)
            {
                await userManager.AddToRoleAsync(patientUser, "Patient");

                var patientProfile = new PatientProfile
                {
                    UserId = patientUser.Id,
                    DateOfBirth = new DateTime(1990, 5, 15),
                    Gender = "Male",
                    BloodType = "O+",
                    Address = "12 Rainbow Street",
                    PhoneNumber = "+962 7 9000 0000",
                    EmergencyContact = "Jane Doe (+962 7 9000 0000)"
                };

                context.PatientProfiles.Add(patientProfile);
                await context.SaveChangesAsync();
            }
        }
    }
}
