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
------------------------!Replace password with more appropriate for your situation.!!!!!!!!!!!!!!!!!!!!!!!----------------------------------
--------------------------------------------------------------------------------------
DECLARE @path nvarchar(MAX) 
DECLARE @password nvarchar(128) 
DECLARE @tsqlToEval as nvarchar(max) 

--Should be separated because of SQL2005 support 

SET @path =  N'D:\VS2017_PROJECTS\TransposingMatrix\TransposingMatrix\askTransposingMatrix.snk';
SET @password =  N'S#im@ple1Tal0k';
SET @tsqlToEval =  N'USE MASTER;' + CHAR(13) + 
				    'CREATE ASYMMETRIC KEY [askTransposingMatrix]' + char(13 ) + 
				    'FROM FILE = ''' + @path + '''
				    ENCRYPTION BY PASSWORD = '''+ @password+ '''';

--PRINT @tsqlToEval


    IF
    (
	   SELECT COUNT(*)
	   FROM master.sys.asymmetric_keys
	   WHERE name LIKE 'askTransposingMatrix%'
    ) = 0
    BEGIN
       EXEC sp_executesql @tsqlToEval;
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