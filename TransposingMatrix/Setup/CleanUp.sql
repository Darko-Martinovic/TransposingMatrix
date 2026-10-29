-------------------------------------------------------------------------
-- Removes MATRIX.Transposing and MATRIX.Help from the current database,
-- version 1 or 2, and the trust the server keeps for them once no other
-- database uses them. Run it as a sysadmin to remove that trust.
-------------------------------------------------------------------------
SET NOCOUNT ON;
GO
-------------------------------------------------------------------------
-- Drop the stored procedure, the helper function and the assembly
-------------------------------------------------------------------------
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
GO
-------------------------------------------------------------------------
-- Drop the schema, unless it holds tables of yours, such as saved results
-------------------------------------------------------------------------
IF SCHEMA_ID(N'MATRIX') IS NOT NULL
BEGIN
    IF EXISTS (SELECT * FROM sys.objects WHERE schema_id = SCHEMA_ID(N'MATRIX'))
       OR EXISTS (SELECT * FROM sys.types WHERE schema_id = SCHEMA_ID(N'MATRIX'))
        PRINT N'The schema MATRIX still holds objects, so it stays.';
    ELSE
        EXEC sys.sp_executesql N'DROP SCHEMA [MATRIX];';
END;
GO
-------------------------------------------------------------------------
-- Remove the server's trust once no database uses the assembly:
-- version 2's trusted assemblies, version 1's key and login
-------------------------------------------------------------------------
IF IS_SRVROLEMEMBER(N'sysadmin') = 0
BEGIN
    PRINT N'Only a sysadmin can remove the server''s trust in the assembly: run CleanUp.sql as one to finish.';
    RETURN;
END;

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

-- sys.trusted_assemblies came with SQL Server 2017
IF ISNULL(CAST(SERVERPROPERTY('ProductMajorVersion') AS int), 0) >= 14
BEGIN
    DECLARE @stale varbinary(64);
    DECLARE @staleSql nvarchar(max) = N'
        SELECT t.hash FROM sys.trusted_assemblies AS t
        WHERE t.description LIKE N''simpletalk.sqlclr.matrix,%''';
    DECLARE @trusted TABLE (hash varbinary(64));
    INSERT INTO @trusted EXEC sys.sp_executesql @staleSql;
    DECLARE stale CURSOR LOCAL FAST_FORWARD FOR
        SELECT t.hash FROM @trusted AS t WHERE NOT EXISTS (SELECT * FROM @used AS u WHERE u.hash = t.hash);
    OPEN stale;
    FETCH NEXT FROM stale INTO @stale;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC sys.sp_executesql N'EXEC sys.sp_drop_trusted_assembly @hash = @hash;', N'@hash varbinary(64)', @hash = @stale;
        FETCH NEXT FROM stale INTO @stale;
    END;
    CLOSE stale;
    DEALLOCATE stale;
END;

IF EXISTS (SELECT * FROM @used WHERE clr_name NOT LIKE N'%publickeytoken=null%')
    PRINT N'Version 1 is still installed in another database, so its key askTransposingMatrix and login loginTransposingMatrix stay.';
ELSE
BEGIN
    IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'loginTransposingMatrix')
        DROP LOGIN [loginTransposingMatrix];
    IF EXISTS (SELECT * FROM master.sys.asymmetric_keys WHERE name = N'askTransposingMatrix')
        EXEC master.sys.sp_executesql N'DROP ASYMMETRIC KEY [askTransposingMatrix];';
END;
GO
