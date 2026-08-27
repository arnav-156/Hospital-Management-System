:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Appointments
(
    AppointmentId int IDENTITY(1, 1) NOT NULL,
    PatientId int NOT NULL,
    DoctorId int NOT NULL,
    DepartmentId int NOT NULL,
    AppointmentDateTime datetime2(0) NOT NULL,
    DurationMinutes smallint NOT NULL CONSTRAINT DF_Appointments_DurationMinutes DEFAULT (30),
    Status varchar(20) NOT NULL CONSTRAINT DF_Appointments_Status DEFAULT ('Pending'),
    Reason nvarchar(1000) NULL,
    DoctorResponseNote nvarchar(1000) NULL,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Appointments_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt datetime2(0) NULL,
    CONSTRAINT PK_Appointments PRIMARY KEY CLUSTERED (AppointmentId),
    CONSTRAINT UQ_Appointments_DoctorId_AppointmentDateTime UNIQUE (DoctorId, AppointmentDateTime),
    CONSTRAINT UQ_Appointments_AppointmentPatient UNIQUE (AppointmentId, PatientId),
    CONSTRAINT UQ_Appointments_AppointmentPatientDoctor UNIQUE (AppointmentId, PatientId, DoctorId),
    CONSTRAINT FK_Appointments_Patients_PatientId FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId),
    CONSTRAINT FK_Appointments_Doctors_DoctorDepartment FOREIGN KEY (DoctorId, DepartmentId) REFERENCES dbo.Doctors (DoctorId, DepartmentId),
    CONSTRAINT CK_Appointments_DurationMinutes CHECK (DurationMinutes BETWEEN 5 AND 480),
    CONSTRAINT CK_Appointments_Status CHECK (Status IN ('Pending', 'Accepted', 'Rejected', 'Completed', 'Cancelled'))
);

COMMIT TRANSACTION;
GO

CREATE INDEX IX_Appointments_PatientId ON dbo.Appointments (PatientId);
CREATE INDEX IX_Appointments_DoctorId ON dbo.Appointments (DoctorId);
CREATE INDEX IX_Appointments_AppointmentDateTime ON dbo.Appointments (AppointmentDateTime);
CREATE INDEX IX_Appointments_Status ON dbo.Appointments (Status);
CREATE INDEX IX_Appointments_DoctorId_AppointmentDateTime_Status ON dbo.Appointments (DoctorId, AppointmentDateTime, Status);
GO
