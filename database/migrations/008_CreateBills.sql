:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Bills
(
    BillId int IDENTITY(1, 1) NOT NULL,
    AppointmentId int NOT NULL,
    PatientId int NOT NULL,
    GeneratedByDoctorId int NULL,
    Amount decimal(12, 2) NOT NULL,
    Status varchar(20) NOT NULL CONSTRAINT DF_Bills_Status DEFAULT ('Pending'),
    Description nvarchar(1000) NULL,
    GeneratedAt datetime2(0) NOT NULL CONSTRAINT DF_Bills_GeneratedAt DEFAULT (SYSUTCDATETIME()),
    DueDate date NULL,
    PaidAt datetime2(0) NULL,
    CONSTRAINT PK_Bills PRIMARY KEY CLUSTERED (BillId),
    CONSTRAINT UQ_Bills_AppointmentId UNIQUE (AppointmentId),
    CONSTRAINT FK_Bills_Appointments_AppointmentPatient FOREIGN KEY (AppointmentId, PatientId) REFERENCES dbo.Appointments (AppointmentId, PatientId),
    CONSTRAINT FK_Bills_Doctors_GeneratedByDoctorId FOREIGN KEY (GeneratedByDoctorId) REFERENCES dbo.Doctors (DoctorId),
    CONSTRAINT CK_Bills_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Bills_Status CHECK (Status IN ('Pending', 'Paid', 'Void')),
    CONSTRAINT CK_Bills_PaidAt CHECK ((Status = 'Paid' AND PaidAt IS NOT NULL) OR (Status <> 'Paid' AND PaidAt IS NULL))
);

COMMIT TRANSACTION;
GO

CREATE INDEX IX_Bills_PatientId ON dbo.Bills (PatientId);
CREATE INDEX IX_Bills_Status ON dbo.Bills (Status);
GO
