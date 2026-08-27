using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.IntegrationTests;

public sealed class DatabaseFirstCrudTests
{
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__HospitalManagementDb") ?? "Server=localhost,1433;Database=HospitalManagementDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True";

    [Fact]
    public async Task CanReadWriteUpdateAndDeleteEveryCoreEntity()
    {
        var options = new DbContextOptionsBuilder<HospitalManagementDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = new HospitalManagementDbContext(options);
        Assert.True(await context.Database.CanConnectAsync());

        await using var transaction = await context.Database.BeginTransactionAsync();
        var identifier = Guid.NewGuid().ToString("N");

        var department = new Department
        {
            DepartmentCode = $"T{identifier[..8]}",
            Name = $"EF Test {identifier}",
            Description = "Created by the database-first integration test.",
            IsActive = true,
        };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var patientUser = CreateUser($"patient.{identifier}@example.test", "Patient");
        var doctorUser = CreateUser($"doctor.{identifier}@example.test", "Doctor");
        var staffUser = CreateUser($"staff.{identifier}@example.test", "Administrator");
        context.Users.AddRange(patientUser, doctorUser, staffUser);
        await context.SaveChangesAsync();

        var patient = new Patient
        {
            UserId = patientUser.UserId,
            MedicalRecordNumber = $"MRN-{identifier[..20]}",
            FirstName = "Database",
            LastName = "Patient",
            DateOfBirth = new DateOnly(1990, 1, 1),
        };
        var doctor = new Doctor
        {
            UserId = doctorUser.UserId,
            DepartmentId = department.DepartmentId,
            LicenseNumber = $"LIC-{identifier}",
            FirstName = "Database",
            LastName = "Doctor",
            Specialization = "Integration Testing",
            ConsultationFee = 100m,
            IsActive = true,
        };
        var staff = new Staff
        {
            UserId = staffUser.UserId,
            DepartmentId = department.DepartmentId,
            EmployeeNumber = $"EMP-{identifier[..20]}",
            FirstName = "Database",
            LastName = "Staff",
            JobTitle = "Test Coordinator",
            IsActive = true,
        };
        context.AddRange(patient, doctor, staff);
        await context.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.PatientId,
            DoctorId = doctor.DoctorId,
            DepartmentId = department.DepartmentId,
            AppointmentDateTime = DateTime.UtcNow.AddDays(7).AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond)),
            DurationMinutes = 30,
            Status = "Pending",
            Reason = "EF Core database-first test",
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var treatment = new Treatment
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = patient.PatientId,
            DoctorId = doctor.DoctorId,
            Diagnosis = "Test diagnosis",
            TreatmentNotes = "Created by integration test.",
        };
        var bill = new Bill
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = patient.PatientId,
            GeneratedByDoctorId = doctor.DoctorId,
            Amount = 100m,
            Status = "Pending",
            Description = "Test bill",
        };
        var notification = new Notification
        {
            UserId = patientUser.UserId,
            NotificationType = "Test",
            Message = "EF Core database-first test notification.",
            IsRead = false,
        };
        var feedback = new Feedback
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = patient.PatientId,
            Rating = 5,
            Comments = "Test feedback",
        };
        context.AddRange(treatment, bill, notification, feedback);
        await context.SaveChangesAsync();

        Assert.Equal(12, await CountCreatedEntitiesAsync(context, patientUser, doctorUser, staffUser, department, patient, doctor, staff, appointment, treatment, bill, notification, feedback));

        department.Description = "Updated department";
        patient.Address = "Updated address";
        doctor.PhoneNumber = "555-0199";
        staff.JobTitle = "Updated coordinator";
        appointment.Status = "Accepted";
        treatment.ProgressNotes = "Updated progress";
        bill.Description = "Updated bill";
        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        feedback.Comments = "Updated feedback";
        await context.SaveChangesAsync();

        Assert.Equal("Accepted", await context.Appointments.Where(item => item.AppointmentId == appointment.AppointmentId).Select(item => item.Status).SingleAsync());
        Assert.Equal("Updated progress", await context.Treatments.Where(item => item.TreatmentId == treatment.TreatmentId).Select(item => item.ProgressNotes).SingleAsync());
        Assert.Equal("Updated bill", await context.Bills.Where(item => item.BillId == bill.BillId).Select(item => item.Description).SingleAsync());
        Assert.True(await context.Notifications.Where(item => item.NotificationId == notification.NotificationId).Select(item => item.IsRead).SingleAsync());

        context.RemoveRange(feedback, notification, bill, treatment, appointment, staff, patient, doctor, patientUser, doctorUser, staffUser, department);
        await context.SaveChangesAsync();

        Assert.Equal(0, await CountCreatedEntitiesAsync(context, patientUser, doctorUser, staffUser, department, patient, doctor, staff, appointment, treatment, bill, notification, feedback));
    }

    private static User CreateUser(string email, string role) => new()
    {
        Email = email,
        PasswordHash = "integration-test-only",
        Role = role,
        IsActive = true,
    };

    private static async Task<int> CountCreatedEntitiesAsync(
        HospitalManagementDbContext context,
        User patientUser,
        User doctorUser,
        User staffUser,
        Department department,
        Patient patient,
        Doctor doctor,
        Staff staff,
        Appointment appointment,
        Treatment treatment,
        Bill bill,
        Notification notification,
        Feedback feedback)
    {
        var counts = new[]
        {
            await context.Users.CountAsync(item => item.UserId == patientUser.UserId || item.UserId == doctorUser.UserId || item.UserId == staffUser.UserId),
            await context.Departments.CountAsync(item => item.DepartmentId == department.DepartmentId),
            await context.Patients.CountAsync(item => item.PatientId == patient.PatientId),
            await context.Doctors.CountAsync(item => item.DoctorId == doctor.DoctorId),
            await context.Staff.CountAsync(item => item.StaffId == staff.StaffId),
            await context.Appointments.CountAsync(item => item.AppointmentId == appointment.AppointmentId),
            await context.Treatments.CountAsync(item => item.TreatmentId == treatment.TreatmentId),
            await context.Bills.CountAsync(item => item.BillId == bill.BillId),
            await context.Notifications.CountAsync(item => item.NotificationId == notification.NotificationId),
            await context.Feedbacks.CountAsync(item => item.FeedbackId == feedback.FeedbackId),
        };

        return counts.Sum();
    }
}
