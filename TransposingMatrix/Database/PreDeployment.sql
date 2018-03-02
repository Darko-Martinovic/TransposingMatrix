--------------------------------------------------------------------------------------
--Create schema if not exists
--------------------------------------------------------------------------------------
IF NOT EXISTS
(
    SELECT schema_name
    FROM information_schema.schemata
    WHERE schema_name = 'MATRIX'
)
    BEGIN
        EXEC sp_executesql
             N'CREATE SCHEMA MATRIX';
    END;
---------------------------------------------------------------------------------------
--- Only for SQL Server 2017+
-----------------------------------------------
IF SERVERPROPERTY('productversion') >= '14' AND
  SUBSTRING(CAST(SERVERPROPERTY('productversion') as nvarchar(10)),1,1) != '9'

BEGIN
--------------------------------------------------------------------------------------
--Create asymetric key 
--
--Replace the path 'D:\VS2017_PROJECTS\TransposingMatrix\TransposingMatrix\' with your path
--
--------------------------------------------------------------------------------------
    IF
    (
	   SELECT COUNT(*)
	   FROM master.sys.asymmetric_keys
	   WHERE name LIKE 'askTransposingMatrix%'
    ) = 0
    BEGIN
       EXEC sp_executesql
				    N'USE MASTER;
				    CREATE ASYMMETRIC KEY [askTransposingMatrix]
				    FROM FILE = ''D:\VS2017_PROJECTS\TransposingMatrix\TransposingMatrix\askTransposingMatrix.snk''
				    ENCRYPTION BY PASSWORD = ''SimpleTalk''';
    END;
    IF NOT EXISTS
    (
        SELECT loginname
	   FROM master.dbo.syslogins
	   WHERE name = 'loginTransposingMatrix'
    )
    BEGIN
	   DECLARE @sqlStatement AS NVARCHAR(1000);
	   SELECT @SqlStatement = 'CREATE LOGIN [loginTransposingMatrix] FROM ASYMMETRIC KEY askTransposingMatrix';
	   EXEC sp_executesql  @SqlStatement;
	   EXEC sp_executesql
                      N'USE MASTER;
                      GRANT UNSAFE ASSEMBLY TO [loginTransposingMatrix];';
    END;
END;