:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Treatments
(
    TreatmentId int IDENTITY(1, 1) NOT NULL,
    AppointmentId int NOT NULL,
    PatientId int NOT NULL,
    DoctorId int NOT NULL,
    Diagnosis nvarchar(1000) NULL,
    Prescription nvarchar(max) NULL,
    ProgressNotes nvarchar(max) NULL,
    TreatmentNotes nvarchar(max) NULL,
    TreatmentDateTime datetime2(0) NOT NULL CONSTRAINT DF_Treatments_TreatmentDateTime DEFAULT (SYSUTCDATETIME()),
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Treatments_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt datetime2(0) NULL,
    CONSTRAINT PK_Treatments PRIMARY KEY CLUSTERED (TreatmentId),
    CONSTRAINT UQ_Treatments_AppointmentId UNIQUE (AppointmentId),
    CONSTRAINT FK_Treatments_Appointments_AppointmentPatientDoctor FOREIGN KEY (AppointmentId, PatientId, DoctorId) REFERENCES dbo.Appointments (AppointmentId, PatientId, DoctorId)
);

COMMIT TRANSACTION;
GO

CREATE INDEX IX_Treatments_PatientId_TreatmentDateTime ON dbo.Treatments (PatientId, TreatmentDateTime DESC);
CREATE INDEX IX_Treatments_DoctorId ON dbo.Treatments (DoctorId);
GO
