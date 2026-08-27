:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Staff
(
    StaffId int IDENTITY(1, 1) NOT NULL,
    UserId int NOT NULL,
    DepartmentId int NULL,
    EmployeeNumber varchar(30) NOT NULL,
    FirstName nvarchar(100) NOT NULL,
    LastName nvarchar(100) NOT NULL,
    JobTitle nvarchar(100) NOT NULL,
    PhoneNumber varchar(30) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Staff_IsActive DEFAULT (1),
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Staff_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt datetime2(0) NULL,
    CONSTRAINT PK_Staff PRIMARY KEY CLUSTERED (StaffId),
    CONSTRAINT UQ_Staff_UserId UNIQUE (UserId),
    CONSTRAINT UQ_Staff_EmployeeNumber UNIQUE (EmployeeNumber),
    CONSTRAINT FK_Staff_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Staff_Departments_DepartmentId FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (DepartmentId),
    CONSTRAINT CK_Staff_FirstName CHECK (LEN(LTRIM(RTRIM(FirstName))) > 0),
    CONSTRAINT CK_Staff_LastName CHECK (LEN(LTRIM(RTRIM(LastName))) > 0),
    CONSTRAINT CK_Staff_JobTitle CHECK (LEN(LTRIM(RTRIM(JobTitle))) > 0)
);

COMMIT TRANSACTION;
GO

CREATE INDEX IX_Staff_DepartmentId ON dbo.Staff (DepartmentId);
GO
