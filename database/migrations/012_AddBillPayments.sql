:setvar DatabaseName "HospitalManagementDb"

USE [$(DatabaseName)];
ALTER TABLE dbo.Bills
ADD
    VoidedAt datetime2(0) NULL,
    VoidedByDoctorId int NULL,
    VoidReason nvarchar(500) NULL;
GO

USE [$(DatabaseName)];
ALTER TABLE dbo.Bills
ADD CONSTRAINT FK_Bills_Doctors_VoidedByDoctorId
    FOREIGN KEY (VoidedByDoctorId) REFERENCES dbo.Doctors (DoctorId);
GO

USE [$(DatabaseName)];
ALTER TABLE dbo.Bills
ADD CONSTRAINT CK_Bills_VoidDetails
    CHECK
    (
        (Status = 'Void' AND VoidedAt IS NOT NULL AND VoidedByDoctorId IS NOT NULL AND VoidReason IS NOT NULL)
        OR
        (Status <> 'Void' AND VoidedAt IS NULL AND VoidedByDoctorId IS NULL AND VoidReason IS NULL)
    );
GO

USE [$(DatabaseName)];
CREATE TABLE dbo.BillPayments
(
    PaymentId int IDENTITY(1, 1) NOT NULL,
    BillId int NOT NULL,
    RecordedByPatientId int NOT NULL,
    Amount decimal(12, 2) NOT NULL,
    PaymentMethod varchar(20) NOT NULL,
    ReferenceNumber nvarchar(100) NULL,
    RecordedAt datetime2(0) NOT NULL CONSTRAINT DF_BillPayments_RecordedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_BillPayments PRIMARY KEY CLUSTERED (PaymentId),
    CONSTRAINT UQ_BillPayments_BillId UNIQUE (BillId),
    CONSTRAINT FK_BillPayments_Bills_BillId FOREIGN KEY (BillId) REFERENCES dbo.Bills (BillId),
    CONSTRAINT FK_BillPayments_Patients_RecordedByPatientId FOREIGN KEY (RecordedByPatientId) REFERENCES dbo.Patients (PatientId),
    CONSTRAINT CK_BillPayments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_BillPayments_Method CHECK (PaymentMethod IN ('Cash', 'Card', 'UPI', 'Insurance', 'Other'))
);
GO

USE [$(DatabaseName)];
CREATE INDEX IX_BillPayments_BillId_RecordedAt ON dbo.BillPayments (BillId, RecordedAt DESC);
GO
