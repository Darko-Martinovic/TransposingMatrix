--------------------------------------------------------------------------------------
-- Installs MATRIX.Transposing and MATRIX.Help in the current database, replacing an
-- earlier version. Run it in the database you want them in, as a sysadmin: it adds the
-- assembly to the server's trusted assemblies.
--
-- Needs SQL Server 2017 or later, on Windows or Linux. The version for SQL Server 2005
-- to 2016 is the v1-article tag of the repository.
--
-- Setup.sql is generated from Setup.template.sql by tools/SetupScript; edit the template.
-- Sources: $(SourceHash)
--------------------------------------------------------------------------------------
SET NOCOUNT ON;
GO
--------------------------------------------------------------------------------------
-- Stop here, installing nothing, if the server can't run the assembly
--------------------------------------------------------------------------------------
IF CAST(SERVERPROPERTY('EngineEdition') AS int) = 5
BEGIN
    RAISERROR(N'Azure SQL Database doesn''t run CLR assemblies. MATRIX.Transposing needs SQL Server 2017 or later, or Azure SQL Managed Instance.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF ISNULL(CAST(SERVERPROPERTY('ProductMajorVersion') AS int), 0) < 14
BEGIN
    RAISERROR(N'MATRIX.Transposing needs SQL Server 2017 or later. For SQL Server 2005 to 2016, use the v1-article tag of the repository.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF IS_SRVROLEMEMBER(N'sysadmin') = 0
BEGIN
    RAISERROR(N'Setup.sql adds the assembly to the server''s trusted assemblies, which needs a member of sysadmin.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF (SELECT value_in_use FROM sys.configurations WHERE name = N'clr enabled') = 0
    PRINT N'CLR is disabled on this server. MATRIX.Transposing is installed, but runs only after: EXEC sp_configure ''clr enabled'', 1; RECONFIGURE;';
GO
--------------------------------------------------------------------------------------
-- Remove an earlier version from this database, wherever its objects are
--------------------------------------------------------------------------------------
DECLARE @drop nvarchar(max) =
(
    SELECT STRING_AGG(CAST(N'DROP ' + CASE o.type WHEN 'PC' THEN N'PROCEDURE ' ELSE N'FUNCTION ' END
                      + QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' AS nvarchar(max)), NCHAR(10))
    FROM sys.assembly_modules AS m
         JOIN sys.objects AS o ON o.object_id = m.object_id
         JOIN sys.assemblies AS a ON a.assembly_id = m.assembly_id
    WHERE a.name = N'SimpleTalk.SQLCLR.Matrix'
);
IF @drop IS NOT NULL
    EXEC sys.sp_executesql @drop;
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = N'SimpleTalk.SQLCLR.Matrix')
    DROP ASSEMBLY [SimpleTalk.SQLCLR.Matrix];
IF SCHEMA_ID(N'MATRIX') IS NULL
    EXEC sys.sp_executesql N'CREATE SCHEMA MATRIX';
GO
--------------------------------------------------------------------------------------
-- Trust this build of the assembly. SQL Server 2017's "clr strict security" loads only
-- trusted assemblies, and a trusted assembly needs no key, certificate or login.
--
-- Trust that earlier builds needed and no database uses any more is removed:
--   * the hashes of earlier builds of this version
--   * version 1's asymmetric key askTransposingMatrix and login loginTransposingMatrix,
--     made from a key file published in the repository, so they trusted any assembly
--     signed with it
--------------------------------------------------------------------------------------
DECLARE @hash varbinary(64) = $(AssemblyHash);

DECLARE @used TABLE (hash varbinary(64), clr_name nvarchar(4000));
DECLARE @db sysname, @sql nvarchar(max);
DECLARE databases CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.databases WHERE state = 0 AND HAS_DBACCESS(name) = 1;
OPEN databases;
FETCH NEXT FROM databases INTO @db;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'SELECT HASHBYTES(''SHA2_512'', f.content), a.clr_name
                 FROM ' + QUOTENAME(@db) + N'.sys.assemblies AS a
                      JOIN ' + QUOTENAME(@db) + N'.sys.assembly_files AS f ON f.assembly_id = a.assembly_id AND f.file_id = 1
                 WHERE a.name = N''SimpleTalk.SQLCLR.Matrix''';
    INSERT INTO @used EXEC sys.sp_executesql @sql;
    FETCH NEXT FROM databases INTO @db;
END;
CLOSE databases;
DEALLOCATE databases;

DECLARE @stale varbinary(64);
DECLARE stale CURSOR LOCAL FAST_FORWARD FOR
    SELECT t.hash
    FROM sys.trusted_assemblies AS t
    WHERE t.description LIKE N'simpletalk.sqlclr.matrix,%'
          AND t.hash <> @hash
          AND NOT EXISTS (SELECT * FROM @used AS u WHERE u.hash = t.hash);
OPEN stale;
FETCH NEXT FROM stale INTO @stale;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sys.sp_drop_trusted_assembly @hash = @stale;
    FETCH NEXT FROM stale INTO @stale;
END;
CLOSE stale;
DEALLOCATE stale;

IF NOT EXISTS (SELECT * FROM sys.trusted_assemblies WHERE hash = @hash)
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'$(AssemblyDescription)';

IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'loginTransposingMatrix')
   OR EXISTS (SELECT * FROM master.sys.asymmetric_keys WHERE name = N'askTransposingMatrix')
BEGIN
    IF EXISTS (SELECT * FROM @used WHERE clr_name NOT LIKE N'%publickeytoken=null%')
        PRINT N'Version 1 is still installed in another database, so its key askTransposingMatrix and login loginTransposingMatrix stay. They trust any assembly signed with a key file anyone can download: run Setup.sql or CleanUp.sql in that database too.';
    ELSE
    BEGIN
        IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'loginTransposingMatrix')
            DROP LOGIN [loginTransposingMatrix];
        IF EXISTS (SELECT * FROM master.sys.asymmetric_keys WHERE name = N'askTransposingMatrix')
            EXEC master.sys.sp_executesql N'DROP ASYMMETRIC KEY [askTransposingMatrix];';
        PRINT N'Removed version 1''s key askTransposingMatrix and login loginTransposingMatrix.';
    END;
END;
GO
--------------------------------------------------------------------------------------
-- The assembly
--------------------------------------------------------------------------------------
CREATE ASSEMBLY [SimpleTalk.SQLCLR.Matrix]
    AUTHORIZATION [dbo]
    FROM $(AssemblyBits)
    WITH PERMISSION_SET = SAFE;
GO
CREATE PROCEDURE [MATRIX].[Transposing]
    @Query [nvarchar](max),
    @Params [nvarchar](4000) = NULL,
    @Rco [smallint] = 0,
    @KeyValueOption [smallint] = 0,
    @ColumnMapping [nvarchar](4000) = NULL,
    @TableName [nvarchar](256) = NULL
AS EXTERNAL NAME [SimpleTalk.SQLCLR.Matrix].[TransposingMatrix.StoredProcedures].[Transposing];
GO
CREATE FUNCTION [MATRIX].[Help] (@ProcedureName [nvarchar](max))
RETURNS TABLE
(
    [ParameterName] [nvarchar](100) NULL,
    [ParameterDescription] [nvarchar](max) NULL
)
AS EXTERNAL NAME [SimpleTalk.SQLCLR.Matrix].[TransposingMatrix.UserDefinedFunctions].[MatrixHelp];
GO
SET NOEXEC OFF;
GO
