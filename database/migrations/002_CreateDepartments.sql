:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Departments
(
    DepartmentId int IDENTITY(1, 1) NOT NULL,
    DepartmentCode varchar(20) NOT NULL,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(500) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT (1),
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Departments PRIMARY KEY CLUSTERED (DepartmentId),
    CONSTRAINT UQ_Departments_DepartmentCode UNIQUE (DepartmentCode),
    CONSTRAINT UQ_Departments_Name UNIQUE (Name),
    CONSTRAINT CK_Departments_DepartmentCode CHECK (LEN(LTRIM(RTRIM(DepartmentCode))) > 0),
    CONSTRAINT CK_Departments_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);

COMMIT TRANSACTION;
GO
