:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Feedback
(
    FeedbackId int IDENTITY(1, 1) NOT NULL,
    AppointmentId int NOT NULL,
    PatientId int NOT NULL,
    Rating tinyint NOT NULL,
    Comments nvarchar(2000) NULL,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Feedback_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Feedback PRIMARY KEY CLUSTERED (FeedbackId),
    CONSTRAINT UQ_Feedback_AppointmentId UNIQUE (AppointmentId),
    CONSTRAINT FK_Feedback_Appointments_AppointmentPatient FOREIGN KEY (AppointmentId, PatientId) REFERENCES dbo.Appointments (AppointmentId, PatientId),
    CONSTRAINT CK_Feedback_Rating CHECK (Rating BETWEEN 1 AND 5)
);

COMMIT TRANSACTION;
GO
