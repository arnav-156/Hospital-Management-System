:setvar DatabaseName "HospitalManagementDb"

:r database\scripts\CreateDatabase.sql
:r database\migrations\001_CreateUsers.sql
:r database\migrations\002_CreateDepartments.sql
:r database\migrations\003_CreatePatients.sql
:r database\migrations\004_CreateDoctors.sql
:r database\migrations\005_CreateStaff.sql
:r database\migrations\006_CreateAppointments.sql
:r database\migrations\007_CreateTreatments.sql
:r database\migrations\008_CreateBills.sql
:r database\migrations\009_CreateNotifications.sql
:r database\migrations\010_CreateFeedback.sql
:r database\migrations\011_CreateAiSummaryAudits.sql
:r database\seed\001_DevelopmentSeedData.sql
:r database\scripts\ValidateDatabase.sql
