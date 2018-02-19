----------------------------------------------------------
--The first test
----------------------------------------------------------
EXEC MATRIX.TRANSPOSING @query='SELECT * FROM sys.databases'
GO
----------------------------------------------------------
--save the result into permanent table
----------------------------------------------------------
EXEC MATRIX.TRANSPOSING @query='SELECT * FROM sys.databases',@tablename ='MATRIX.TEMPTABLE';
GO 
----------------------------------------------------------
--save the result into temp table ( note two ## )
----------------------------------------------------------
EXEC MATRIX.TRANSPOSING @query='SELECT * FROM sys.databases',@tablename ='##tester'
GO
----------------------------------------------------------
--Test Phil's example
----------------------------------------------------------
EXEC [MATRIX].[Transposing] @Query = 'SELECT
	 *
  FROM (VALUES
  (25119, 25002, 25109, 23860, 22957, 23518, 23330, 22926, 23365, 23418, 23644),
  (5332, 5504, 5780, 6057, 6054, 6384, 6624, 6782, 7035, 7190, 7083),
  (20213, 20426, 20166, 20086, 19276, 19223, 19075, 18605, 18372, 18266, 18380),
  (6576, 6711, 6935, 7440, 7855, 8201, 8455, 8770, 9011, 9353, 9570),
  (2917, 2928, 3063, 3236, 3315, 3486, 3413, 3579, 3678, 3763, 3888),
  (24569, 25157, 26035, 25900, 26244, 27954, 28893, 30001, 30588, 31119, 32444)
  ) oilConsumption ([2005], [2006], [2007], [2008], [2009], [2010], [2011], [2012], [2013], [2014], [2015])'
			 ,@params = NULL
			 ,@rco = 1
			 ,@keyvalueoption = 1
			 ,@columnMapping = N'[Year],[Total North America],[Total S. & Cent. America],[Total Europe & Eurasia],[Total Middle East],[Total Africa],[Total Asia Pacific]';

----------------------------------------------------
--Test Adam's sp_whoisactive
----------------------------------------------------
EXEC [MATRIX].[Transposing] @Query = 'EXEC sp_whoisactive';


----------------------------------------------------
--Transfer the result into temporary table
----------------------------------------------------
--sp_configure 'Show Advanced Options', 1
--GO
--RECONFIGURE
--GO
--sp_configure 'Ad Hoc Distributed Queries', 1
--GO
--RECONFIGURE
--GO

--SELECT
--    * INTO #MyTempTable
--FROM OPENROWSET('SQLNCLI', 'Server=darko;Trusted_Connection=yes;',
--'EXEC AdventureWorks2014.[MATRIX].[Transposing] @Query = ''EXEC sp_whoisactive''
--WITH RESULT SETS ((test        sysname,test2 sysname ))
--')

--SELECT
--    *
--FROM #MyTempTable


----------------------------------------------------------
-- Column mapping is null 
----------------------------------------------------------
EXEC MATRIX.Transposing @Query = 'SELECT
	 *
  FROM (VALUES
  (25119, 25002, 25109, 23860, 22957, 23518, 23330, 22926, 23365, 23418, 23644),
  (5332, 5504, 5780, 6057, 6054, 6384, 6624, 6782, 7035, 7190, 7083),
  (20213, 20426, 20166, 20086, 19276, 19223, 19075, 18605, 18372, 18266, 18380),
  (6576, 6711, 6935, 7440, 7855, 8201, 8455, 8770, 9011, 9353, 9570),
  (2917, 2928, 3063, 3236, 3315, 3486, 3413, 3579, 3678, 3763, 3888),
  (24569, 25157, 26035, 25900, 26244, 27954, 28893, 30001, 30588, 31119, 32444)
  ) oilConsumption ([2005], [2006], [2007], [2008], [2009], [2010], [2011], [2012], [2013], [2014], [2015])'

			 ,@keyvalueoption = 1
