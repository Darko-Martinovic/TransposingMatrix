## :white_check_mark: To transpose query results 

```diff
-EXEC MATRIX.TRANSPOSING @query = N'SELECT * FROM SYS.DATABASES';
```

## :white_check_mark: To save transposing query results in a temporary or permanent table.
```diff
+The table will be created inside the stored procedure, and after that, you have to drop the table manually. 
+There is no need to create a temporary or a permanent table first. 
+The whole task is accomplished inside the stored procedure. 
+The account that executes stored procedure has to have "CREATE TABLE permission."
```
```diff
-EXEC MATRIX.TRANSPOSING  @query = N'SELECT * FROM sys.databases', @tableName = N'##tempTable';
```
     
```diff
+---The same result as in the first query
```
    

```diff
-SELECT * FROM ##tempTable;
```

## :white_check_mark: To choose transposing column

```diff
+--The first column - the database name
```
```diff
-EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases' ,@Rco = 0;
```
```diff				   
+--The second column - the database ID
```
```diff
-EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases',@Rco = 1;
```

## :white_check_mark: To filter before transposing
```diff
-EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases WHERE database_id >= @id1 AND database_id <= @id2;',
-                        @Params = N'@id1 int=1,@Id2 int=4';
```     

## :white_check_mark: To transpose with generic header ( key, value, value1 and so on )
```diff
-EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases;',@KeyValueOption = 1;
```

## :white_check_mark: To transpose with custom header
```diff
-EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases;'
-					  ,@KeyValueOption = 1
-					  ,@ColumnMapping = N'Database name,Sys database master'
```					  

