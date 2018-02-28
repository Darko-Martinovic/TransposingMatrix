## To transpose query results
EXEC MATRIX.TRANSPOSING
     @query = 'SELECT * FROM SYS.DATABASES';

## To save transposing query results in temporary or permanent table.
EXEC MATRIX.TRANSPOSING
     @query = 'SELECT * FROM sys.databases',
     @tableName = N'##tempTable';
     
---The same result as in the first query

SELECT *
FROM ##tempTable;


