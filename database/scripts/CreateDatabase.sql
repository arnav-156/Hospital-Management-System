:setvar DatabaseName "HospitalManagementDb"

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    DECLARE @createDatabaseSql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(N'$(DatabaseName)') + N';';
    EXECUTE sys.sp_executesql @createDatabaseSql;
END;
GO
