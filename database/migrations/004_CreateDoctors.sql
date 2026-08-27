:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Doctors
(
    DoctorId int IDENTITY(1, 1) NOT NULL,
    UserId int NOT NULL,
    DepartmentId int NOT NULL,
    LicenseNumber varchar(50) NOT NULL,
    FirstName nvarchar(100) NOT NULL,
    LastName nvarchar(100) NOT NULL,
    Specialization nvarchar(150) NOT NULL,
    PhoneNumber varchar(30) NULL,
    ConsultationFee decimal(12, 2) NOT NULL CONSTRAINT DF_Doctors_ConsultationFee DEFAULT (0),
    IsActive bit NOT NULL CONSTRAINT DF_Doctors_IsActive DEFAULT (1),
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Doctors_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt datetime2(0) NULL,
    CONSTRAINT PK_Doctors PRIMARY KEY CLUSTERED (DoctorId),
    CONSTRAINT UQ_Doctors_UserId UNIQUE (UserId),
    CONSTRAINT UQ_Doctors_LicenseNumber UNIQUE (LicenseNumber),
    CONSTRAINT UQ_Doctors_DoctorDepartment UNIQUE (DoctorId, DepartmentId),
    CONSTRAINT FK_Doctors_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Doctors_Departments_DepartmentId FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (DepartmentId),
    CONSTRAINT CK_Doctors_FirstName CHECK (LEN(LTRIM(RTRIM(FirstName))) > 0),
    CONSTRAINT CK_Doctors_LastName CHECK (LEN(LTRIM(RTRIM(LastName))) > 0),
    CONSTRAINT CK_Doctors_Specialization CHECK (LEN(LTRIM(RTRIM(Specialization))) > 0),
    CONSTRAINT CK_Doctors_ConsultationFee CHECK (ConsultationFee >= 0)
);

COMMIT TRANSACTION;
GO

CREATE INDEX IX_Doctors_DepartmentId ON dbo.Doctors (DepartmentId);
GO
