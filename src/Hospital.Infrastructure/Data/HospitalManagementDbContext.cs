using System;
using System.Collections.Generic;
using Hospital.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Data;

public partial class HospitalManagementDbContext : DbContext
{
    public HospitalManagementDbContext(DbContextOptions<HospitalManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AiSummaryAudit> AiSummaryAudits { get; set; }

    public virtual DbSet<Bill> Bills { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Treatment> Treatments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiSummaryAudit>(entity =>
        {
            entity.HasIndex(e => new { e.PatientId, e.RequestedAt }, "IX_AiSummaryAudits_PatientId_RequestedAt").IsDescending(false, true);
            entity.Property(e => e.FailureCode).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.Outcome).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.RequestedAt).HasPrecision(0).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasIndex(e => e.AppointmentDateTime, "IX_Appointments_AppointmentDateTime");

            entity.HasIndex(e => e.DoctorId, "IX_Appointments_DoctorId");

            entity.HasIndex(e => new { e.DoctorId, e.AppointmentDateTime, e.Status }, "IX_Appointments_DoctorId_AppointmentDateTime_Status");

            entity.HasIndex(e => e.PatientId, "IX_Appointments_PatientId");

            entity.HasIndex(e => e.Status, "IX_Appointments_Status");

            entity.HasIndex(e => new { e.AppointmentId, e.PatientId }, "UQ_Appointments_AppointmentPatient").IsUnique();

            entity.HasIndex(e => new { e.AppointmentId, e.PatientId, e.DoctorId }, "UQ_Appointments_AppointmentPatientDoctor").IsUnique();

            entity.HasIndex(e => new { e.DoctorId, e.AppointmentDateTime }, "UQ_Appointments_DoctorId_AppointmentDateTime").IsUnique();

            entity.Property(e => e.AppointmentDateTime).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DoctorResponseNote).HasMaxLength(1000);
            entity.Property(e => e.DurationMinutes).HasDefaultValue((short)30);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasPrincipalKey(p => new { p.DoctorId, p.DepartmentId })
                .HasForeignKey(d => new { d.DoctorId, d.DepartmentId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Doctors_DoctorDepartment");
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasIndex(e => e.PatientId, "IX_Bills_PatientId");

            entity.HasIndex(e => e.Status, "IX_Bills_Status");

            entity.HasIndex(e => e.AppointmentId, "UQ_Bills_AppointmentId").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.GeneratedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PaidAt).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.GeneratedByDoctor).WithMany(p => p.Bills).HasForeignKey(d => d.GeneratedByDoctorId);

            entity.HasOne(d => d.Appointment).WithMany(p => p.Bills)
                .HasPrincipalKey(p => new { p.AppointmentId, p.PatientId })
                .HasForeignKey(d => new { d.AppointmentId, d.PatientId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bills_Appointments_AppointmentPatient");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(e => e.DepartmentCode, "UQ_Departments_DepartmentCode").IsUnique();

            entity.HasIndex(e => e.Name, "UQ_Departments_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DepartmentCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasIndex(e => e.DepartmentId, "IX_Doctors_DepartmentId");

            entity.HasIndex(e => new { e.DoctorId, e.DepartmentId }, "UQ_Doctors_DoctorDepartment").IsUnique();

            entity.HasIndex(e => e.LicenseNumber, "UQ_Doctors_LicenseNumber").IsUnique();

            entity.HasIndex(e => e.UserId, "UQ_Doctors_UserId").IsUnique();

            entity.Property(e => e.ConsultationFee).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.LicenseNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Specialization).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Department).WithMany(p => p.Doctors)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithOne(p => p.Doctor)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("Feedback");

            entity.HasIndex(e => e.AppointmentId, "UQ_Feedback_AppointmentId").IsUnique();

            entity.Property(e => e.Comments).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Feedbacks)
                .HasPrincipalKey(p => new { p.AppointmentId, p.PatientId })
                .HasForeignKey(d => new { d.AppointmentId, d.PatientId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_Appointments_AppointmentPatient");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_Notifications_UserId");

            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt }, "IX_Notifications_UserId_IsRead_CreatedAt").IsDescending(false, false, true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReadAt).HasPrecision(0);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(e => e.MedicalRecordNumber, "UQ_Patients_MedicalRecordNumber").IsUnique();

            entity.HasIndex(e => e.UserId, "UQ_Patients_UserId").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EmergencyContactName).HasMaxLength(200);
            entity.Property(e => e.EmergencyContactPhone)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.Gender)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.MedicalRecordNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.User).WithOne(p => p.Patient)
                .HasForeignKey<Patient>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasIndex(e => e.DepartmentId, "IX_Staff_DepartmentId");

            entity.HasIndex(e => e.EmployeeNumber, "UQ_Staff_EmployeeNumber").IsUnique();

            entity.HasIndex(e => e.UserId, "UQ_Staff_UserId").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EmployeeNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.JobTitle).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Department).WithMany(p => p.Staff).HasForeignKey(d => d.DepartmentId);

            entity.HasOne(d => d.User).WithOne(p => p.Staff)
                .HasForeignKey<Staff>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Treatment>(entity =>
        {
            entity.HasIndex(e => e.DoctorId, "IX_Treatments_DoctorId");

            entity.HasIndex(e => new { e.PatientId, e.TreatmentDateTime }, "IX_Treatments_PatientId_TreatmentDateTime").IsDescending(false, true);

            entity.HasIndex(e => e.AppointmentId, "UQ_Treatments_AppointmentId").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Diagnosis).HasMaxLength(1000);
            entity.Property(e => e.TreatmentDateTime)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Appointment).WithMany(p => p.Treatments)
                .HasPrincipalKey(p => new { p.AppointmentId, p.PatientId, p.DoctorId })
                .HasForeignKey(d => new { d.AppointmentId, d.PatientId, d.DoctorId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Treatments_Appointments_AppointmentPatientDoctor");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
