--Drop the stored procedure
IF EXISTS
(
    SELECT *
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[MATRIX].[Transposing]')
          AND type = 'PC'
)
    BEGIN
        DROP PROCEDURE [MATRIX].[Transposing];
END;
--Drop helper function 
IF EXISTS
(
    SELECT *
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[Matrix].[Help]')
          AND type IN(N'FN', N'IF', N'TF', N'FS', N'FT')
)
    DROP FUNCTION [MATRIX].[Help];
--Drop schema
IF EXISTS
(
    SELECT *
    FROM sys.schemas
    WHERE name = N'MATRIX'
)
    BEGIN
        EXEC ('DROP SCHEMA [MATRIX] ');
END;
GO
IF EXISTS
(
    SELECT *
    FROM sys.assemblies
    WHERE name = N'SimpleTalk.SQLCLR.Matrix'
)
    BEGIN
        DROP ASSEMBLY [SimpleTalk.SQLCLR.Matrix];
END;
GO
IF SERVERPROPERTY('productversion') >= '14'
    BEGIN

IF
(
    SELECT COUNT(*)
    FROM master.sys.asymmetric_keys
    WHERE name = 'askTransposingMatrix'
) = 0
            BEGIN
                EXEC sp_executesql
N'USE MASTER;
DROP ASYMMETRIC KEY [askTransposingMatrix]';
            END;
        IF EXISTS
(
    SELECT loginname
    FROM master.dbo.syslogins
    WHERE name = 'loginTransposingMatrix'
)
            BEGIN
                DECLARE @sqlStatement AS NVARCHAR(1000);
                SELECT @SqlStatement = 'DROP LOGIN [loginTransposingMatrix]';
                EXEC sp_executesql
                     @SqlStatement;
            END;
END