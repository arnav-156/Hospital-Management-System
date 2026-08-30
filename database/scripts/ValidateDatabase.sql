:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET NOCOUNT ON;
SET XACT_ABORT OFF;
GO

DECLARE @requiredTables table (TableName sysname NOT NULL PRIMARY KEY);
INSERT INTO @requiredTables (TableName)
VALUES (N'Users'), (N'Departments'), (N'Patients'), (N'Doctors'), (N'Staff'),
       (N'Appointments'), (N'Treatments'), (N'Bills'), (N'BillPayments'), (N'Notifications'), (N'Feedback'), (N'AiSummaryAudits');

IF EXISTS
(
    SELECT 1
    FROM @requiredTables AS required
    WHERE OBJECT_ID(N'dbo.' + required.TableName, N'U') IS NULL
)
    THROW 51000, 'Schema validation failed: one or more required tables are missing.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'UQ_Users_Email')
    THROW 51001, 'Schema validation failed: Users.Email uniqueness is missing.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Appointments') AND name = N'UQ_Appointments_DoctorId_AppointmentDateTime')
    THROW 51002, 'Schema validation failed: appointment double-booking constraint is missing.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Appointments') AND name = N'IX_Appointments_DoctorId_AppointmentDateTime_Status')
    THROW 51010, 'Schema validation failed: doctor/date/status appointment index is missing.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.BillPayments') AND name = N'UQ_BillPayments_BillId')
    THROW 51011, 'Schema validation failed: one payment per bill constraint is missing.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@hospital.example')
    THROW 51003, 'Seed validation failed: development administrator is missing.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Doctors) OR NOT EXISTS (SELECT 1 FROM dbo.Patients) OR NOT EXISTS (SELECT 1 FROM dbo.Staff)
    THROW 51004, 'Seed validation failed: doctor, patient, or staff data is missing.', 1;

DECLARE @patientId int = (SELECT TOP (1) PatientId FROM dbo.Patients ORDER BY PatientId);
DECLARE @doctorId int = (SELECT TOP (1) DoctorId FROM dbo.Doctors ORDER BY DoctorId);
DECLARE @departmentId int = (SELECT DepartmentId FROM dbo.Doctors WHERE DoctorId = @doctorId);
DECLARE @testEmail nvarchar(256) = N'admin@hospital.example';
DECLARE @testAppointmentDateTime datetime2(0) = '2099-01-01T09:00:00';

BEGIN TRANSACTION;

BEGIN TRY
    INSERT INTO dbo.Users (Email, PasswordHash, Role) VALUES (@testEmail, N'validation-only', 'Patient');
    THROW 51005, 'Constraint validation failed: duplicate email was accepted.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601, 2627)
        THROW;
END CATCH;

BEGIN TRY
    INSERT INTO dbo.Appointments (PatientId, DoctorId, DepartmentId, AppointmentDateTime)
    VALUES (-1, @doctorId, @departmentId, @testAppointmentDateTime);
    THROW 51006, 'Constraint validation failed: invalid patient foreign key was accepted.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 547
        THROW;
END CATCH;

INSERT INTO dbo.Appointments (PatientId, DoctorId, DepartmentId, AppointmentDateTime)
VALUES (@patientId, @doctorId, @departmentId, @testAppointmentDateTime);

BEGIN TRY
    INSERT INTO dbo.Appointments (PatientId, DoctorId, DepartmentId, AppointmentDateTime)
    VALUES (@patientId, @doctorId, @departmentId, @testAppointmentDateTime);
    THROW 51007, 'Constraint validation failed: duplicate doctor appointment slot was accepted.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601, 2627)
        THROW;
END CATCH;

ROLLBACK TRANSACTION;

SELECT TOP (20)
    appointment.AppointmentId,
    appointment.AppointmentDateTime,
    appointment.Status,
    patient.MedicalRecordNumber,
    doctor.LicenseNumber,
    department.Name AS DepartmentName
FROM dbo.Appointments AS appointment
INNER JOIN dbo.Patients AS patient ON patient.PatientId = appointment.PatientId
INNER JOIN dbo.Doctors AS doctor ON doctor.DoctorId = appointment.DoctorId
INNER JOIN dbo.Departments AS department ON department.DepartmentId = appointment.DepartmentId
ORDER BY appointment.AppointmentDateTime DESC;

PRINT 'Database validation passed.';
GO
