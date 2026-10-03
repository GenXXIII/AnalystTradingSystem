SET NOCOUNT ON;

IF DB_ID(N'XauAi') IS NULL
BEGIN
    CREATE DATABASE [XauAi];
END;
GO

DECLARE @ApplicationPassword nvarchar(128) = N'$(DATABASE_APP_PASSWORD)';
DECLARE @LoginSql nvarchar(max);

IF SUSER_ID(N'xauai_app') IS NULL
BEGIN
    SET @LoginSql = N'CREATE LOGIN [xauai_app] WITH PASSWORD = '
        + QUOTENAME(@ApplicationPassword, '''')
        + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
END
ELSE
BEGIN
    SET @LoginSql = N'ALTER LOGIN [xauai_app] WITH PASSWORD = '
        + QUOTENAME(@ApplicationPassword, '''')
        + N';';
END;

EXEC sys.sp_executesql @LoginSql;
GO

USE [XauAi];
GO

IF DATABASE_PRINCIPAL_ID(N'xauai_app') IS NULL
BEGIN
    CREATE USER [xauai_app] FOR LOGIN [xauai_app];
END;
GO

IF IS_ROLEMEMBER(N'db_datareader', N'xauai_app') <> 1
BEGIN
    ALTER ROLE [db_datareader] ADD MEMBER [xauai_app];
END;

IF IS_ROLEMEMBER(N'db_datawriter', N'xauai_app') <> 1
BEGIN
    ALTER ROLE [db_datawriter] ADD MEMBER [xauai_app];
END;
GO
