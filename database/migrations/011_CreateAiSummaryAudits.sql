USE [$(DatabaseName)];
GO

CREATE TABLE dbo.AiSummaryAudits
(
    AiSummaryAuditId bigint IDENTITY(1,1) NOT NULL,
    PatientId int NOT NULL,
    DoctorId int NOT NULL,
    RequestedAt datetime2(0) NOT NULL CONSTRAINT DF_AiSummaryAudits_RequestedAt DEFAULT (SYSUTCDATETIME()),
    Outcome varchar(20) NOT NULL,
    Model nvarchar(100) NULL,
    RecordCount int NOT NULL,
    FailureCode varchar(100) NULL,
    CONSTRAINT PK_AiSummaryAudits PRIMARY KEY CLUSTERED (AiSummaryAuditId),
    CONSTRAINT FK_AiSummaryAudits_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId),
    CONSTRAINT FK_AiSummaryAudits_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Doctors (DoctorId),
    CONSTRAINT CK_AiSummaryAudits_Outcome CHECK (Outcome IN ('Generated', 'Unavailable', 'NoRecords')),
    CONSTRAINT CK_AiSummaryAudits_RecordCount CHECK (RecordCount >= 0)
);
GO

CREATE INDEX IX_AiSummaryAudits_PatientId_RequestedAt ON dbo.AiSummaryAudits (PatientId, RequestedAt DESC);
GO
