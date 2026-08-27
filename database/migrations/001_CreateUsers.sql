:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Users
(
    UserId int IDENTITY(1, 1) NOT NULL,
    Email nvarchar(256) NOT NULL,
    PasswordHash nvarchar(500) NOT NULL,
    Role varchar(20) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt datetime2(0) NULL,
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Patient', 'Doctor', 'Administrator')),
    CONSTRAINT CK_Users_Email CHECK (Email LIKE '%_@_%._%')
);

COMMIT TRANSACTION;
GO
