:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Patients
(
    PatientId int IDENTITY(1, 1) NOT NULL,
    UserId int NOT NULL,
    MedicalRecordNumber varchar(30) NOT NULL,
    FirstName nvarchar(100) NOT NULL,
    LastName nvarchar(100) NOT NULL,
    DateOfBirth date NOT NULL,
    Gender varchar(20) NULL,
    PhoneNumber varchar(30) NULL,
    Address nvarchar(500) NULL,
    EmergencyContactName nvarchar(200) NULL,
    EmergencyContactPhone varchar(30) NULL,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Patients_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt datetime2(0) NULL,
    CONSTRAINT PK_Patients PRIMARY KEY CLUSTERED (PatientId),
    CONSTRAINT UQ_Patients_UserId UNIQUE (UserId),
    CONSTRAINT UQ_Patients_MedicalRecordNumber UNIQUE (MedicalRecordNumber),
    CONSTRAINT FK_Patients_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Patients_FirstName CHECK (LEN(LTRIM(RTRIM(FirstName))) > 0),
    CONSTRAINT CK_Patients_LastName CHECK (LEN(LTRIM(RTRIM(LastName))) > 0),
    CONSTRAINT CK_Patients_DateOfBirth CHECK (DateOfBirth <= CAST(SYSUTCDATETIME() AS date)),
    CONSTRAINT CK_Patients_Gender CHECK (Gender IS NULL OR Gender IN ('Female', 'Male', 'NonBinary', 'Undisclosed'))
);

COMMIT TRANSACTION;
GO
