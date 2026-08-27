:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Notifications
(
    NotificationId int IDENTITY(1, 1) NOT NULL,
    UserId int NOT NULL,
    NotificationType varchar(50) NOT NULL,
    Message nvarchar(1000) NOT NULL,
    IsRead bit NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT (0),
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ReadAt datetime2(0) NULL,
    CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (NotificationId),
    CONSTRAINT FK_Notifications_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Notifications_NotificationType CHECK (LEN(LTRIM(RTRIM(NotificationType))) > 0),
    CONSTRAINT CK_Notifications_Message CHECK (LEN(LTRIM(RTRIM(Message))) > 0),
    CONSTRAINT CK_Notifications_ReadAt CHECK ((IsRead = 1 AND ReadAt IS NOT NULL) OR (IsRead = 0 AND ReadAt IS NULL))
);

COMMIT TRANSACTION;
GO

CREATE INDEX IX_Notifications_UserId ON dbo.Notifications (UserId);
CREATE INDEX IX_Notifications_UserId_IsRead_CreatedAt ON dbo.Notifications (UserId, IsRead, CreatedAt DESC);
GO
