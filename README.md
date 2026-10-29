# TransposingMatrix

`MATRIX.Transposing` is a SQLCLR stored procedure that transposes the result of any query or stored procedure: its rows become columns, and its columns become rows. You don't need PIVOT, an aggregate or a list of column names.

The Red Gate Simple Talk article [An Easier Way of Transposing Query Result in SQL Server](https://www.red-gate.com/simple-talk/sql/t-sql-programming/easier-way-transposing-query-result-sql-server/) explains how it works. The code as the article shows it, for SQL Server 2005 to 2017, is the [`v1-article`](https://github.com/Darko-Martinovic/TransposingMatrix/tree/v1-article) tag. The `main` branch is version 2, for current SQL Server.

## Version 2

The procedure's name and parameters haven't changed. These have:

- **Installing.**
  - Setup.sql adds the assembly to the server's trusted assemblies (`sys.sp_add_trusted_assembly`, SQL Server 2017+).
  - It no longer needs a key file, a password or a path to edit.
  - Version 1's key file was published in this repository, and its asymmetric key trusted any assembly signed with it. Setup.sql removes that key and its login once no database uses version 1 (see [Upgrading from version 1](#upgrading-from-version-1)).
- **`@Params` is a `DECLARE` list.** SQL Server parses it, so every type and value works. Version 1 parsed it itself, and:
  - turned `bigint` values into NULL
  - padded `varchar` like `char`
  - failed on `decimal(p,s)`
  - failed on times after 12:59
- **Values keep their precision and don't depend on the server's language.**
  - `datetime2`, `datetimeoffset` and `time` show all their digits.
  - Dates are ISO 8601 and numbers have a decimal point.
  - An empty string stays empty instead of becoming NULL.
- **Errors are errors.** A mistake raises error 50000, so the client gets an error and `@@ERROR` is set. Version 1 printed a message and the call still succeeded.
- **Saving a result (`@TableName`).**
  - It no longer creates a table type for each table, and it no longer leaves one behind when saving fails.
  - Each column is sized to its longest value.
  - A local temporary table (`#name`) is refused with an explanation, because SQL Server would drop it when the procedure ends.
- **Fixed edge cases.**
  - A result of more than 1000 rows gives 1000 columns of values, not 1001.
  - An `@Rco` past the last column is a clear error.
  - Header names a table can't take are corrected.
- **The code.**
  - It is built with `dotnet build` instead of an SSDT database project.
  - Tests run every example against SQL Server 2022 and 2025.

## Requirements

- SQL Server 2017 or later, on Windows or Linux. The tests run on SQL Server 2022 and 2025 for Linux.
  - Azure SQL Managed Instance runs CLR assemblies too, but Setup.sql hasn't been tested there.
  - Azure SQL Database doesn't run CLR assemblies.
- CLR enabled on the server.
- A member of `sysadmin` to run Setup.sql, since it changes what the server trusts. Calling the procedure needs EXECUTE permission on it, and the permissions the query itself needs.

SQL Server runs CLR assemblies on .NET Framework 4.x only, even in SQL Server 2025, so the assembly targets .NET Framework 4.8. The tests and the build tool use .NET 10.

## Install

1. Enable CLR, if it isn't:

   ```sql
   EXEC sp_configure 'clr enabled', 1;
   RECONFIGURE;
   ```

2. Open [TransposingMatrix/Setup/Setup.sql](TransposingMatrix/Setup/Setup.sql) in SSMS, choose the database you want the procedure in, and run it. Running it again replaces the installed version. Setup.sql creates:
   - the schema `MATRIX`
   - the procedure `MATRIX.Transposing`
   - the function `MATRIX.Help`

## Examples

To transpose query results:

```sql
EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases';
```

To save the transposed result in a global temporary or a permanent table:

- The procedure creates the table, replacing a table of that name, and you drop it when you're done.
- The account that runs the procedure needs permission to create the table.
- A local temporary table (`#name`) can't be used: SQL Server drops it when the procedure ends.

```sql
EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases', @TableName = N'##tempTable';

-- The same result as in the first query
SELECT * FROM ##tempTable;
```

To choose the column that becomes the header:

```sql
-- The first column, the database name
EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases', @Rco = 0;

-- The second column, the database ID
EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases', @Rco = 1;
```

To filter before transposing, declare the parameters as in `DECLARE`:

```sql
EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases WHERE database_id >= @id1 AND database_id <= @id2;',
                        @Params = N'@id1 int = 1, @id2 int = 4';
```

To transpose the result of a stored procedure:

```sql
EXEC MATRIX.Transposing @Query = N'EXEC sp_who';
```

To transpose with a generic header (Key, Value, Value1 and so on):

```sql
EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases;', @KeyValueOption = 1;
```

To transpose with a custom header. Names you leave out are filled with ValueN:

```sql
EXEC MATRIX.Transposing @Query = N'SELECT * FROM sys.databases;',
                        @KeyValueOption = 1,
                        @ColumnMapping = N'Database name, Sys database master';
```

[TransposingMatrix/Test/Test.sql](TransposingMatrix/Test/Test.sql) has more examples. `SELECT * FROM MATRIX.Help(N'MATRIX.Transposing');` describes the parameters.

## Parameters

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `@Query` | nvarchar(max) | | The query, or `EXEC` and a stored procedure with its arguments, whose result is transposed. |
| `@Params` | nvarchar(4000) | NULL | The query's parameters, declared as in `DECLARE`: `@id int = 1, @name nvarchar(50) = N'Bikes'`. |
| `@Rco` | smallint | 0 | Rotate column ordinal: the column, counted from 0, whose values become the header. |
| `@KeyValueOption` | smallint | 0 | 1 for a generic header (Key, Value, Value1...), or the names in `@ColumnMapping`. |
| `@ColumnMapping` | nvarchar(4000) | NULL | The header's names with `@KeyValueOption = 1`, comma separated. Names may be in brackets. |
| `@TableName` | nvarchar(256) | NULL | A global temporary table (`##name`) or a permanent table to save the result in. |

## How values are shown

Every column of the result is `nvarchar(max)`. Values become text the same way on every server, whatever the session's language:

| Type | Shown as |
|---|---|
| date | `2020-02-29` |
| datetime | `2020-01-01 10:00:00.000` |
| smalldatetime | `2020-01-01 10:00:00` |
| datetime2(n), time(n) | all n digits of the fraction: `2020-01-01 10:00:00.1234567`, `10:11:12.5` |
| datetimeoffset(n) | `2020-01-01 10:00:00.1234567 +02:00` |
| decimal, numeric, money, float | with a decimal point: `1.50`, `12.3400`, `1.5` |
| bit | `True` or `False` |
| binary, varbinary, rowversion | hexadecimal: `0x0A0B` |
| geography, geometry, hierarchyid | WKT or the path: `POINT (1 1)`, `/1/2/` |
| NULL | NULL |

## Limitations

- **Rows.** Only the first 1000 rows are transposed, because each becomes a column and a table holds at most 1024. A message says when rows were left out.
- **Saved tables.** A table row holds at most 8060 bytes. A wide result of long values may not fit, and the procedure then says so and saves nothing.
- **Several results.** When the query returns several results, each is transposed, but only the first is saved in `@TableName`.
- **Header names.** Each name is cut to 128 characters. A repeated name gets the suffix `_N`, and an empty one becomes `ColumnN`.
- **SQL Server 2025 types.** The json and vector types are newer than .NET Framework's in-process data access, so a query that returns them fails. CAST those columns to `nvarchar(max)` in the query.
- **TRY...CATCH.** T-SQL's TRY...CATCH doesn't catch the procedure's errors; check `@@ERROR`, which is set to 50000.
- **Speed.** The first call after SQL Server starts loads the assembly and takes longer.

## Upgrading from version 1

Run Setup.sql in each database that has version 1. It drops version 1's objects there, wherever they are, and installs version 2.

Version 1 created the asymmetric key `askTransposingMatrix` in master, and the login `loginTransposingMatrix` with `UNSAFE ASSEMBLY` permission, from a key file that was published here. Anyone with that file could sign an assembly the server would then trust. Setup.sql drops the key and the login as soon as no database on the server uses version 1, and prints a message while one still does.

## Uninstall

Run [TransposingMatrix/Setup/CleanUp.sql](TransposingMatrix/Setup/CleanUp.sql) in the database, as a sysadmin. It removes:

- the procedure, the function and the assembly
- the schema, unless tables you saved are still in it
- the server's trust in the assembly, once no other database uses it (version 1's key and login included)

## Building and testing

You need the .NET 10 SDK. The tests also need Docker, or Podman with its Docker API, since Testcontainers starts SQL Server in a container.

```bash
dotnet build TransposingMatrix.slnx

# SQL Server 2025; set MSSQL_IMAGE=mcr.microsoft.com/mssql/server:2022-latest for 2022
dotnet test --solution TransposingMatrix.slnx
```

The tests install the committed Setup.sql, the file readers run. After changing the assembly's code or the template, generate Setup.sql again:

```bash
dotnet run -c Release --project tools/SetupScript
```

Setup.sql records a hash of the sources it was built from, and a test fails when the sources have changed since.

| Path | What it is |
|---|---|
| `TransposingMatrix/` | The assembly (.NET Framework 4.8): the procedure, the help function and their helpers |
| `TransposingMatrix/Setup/Setup.template.sql` | The T-SQL of Setup.sql, without the assembly |
| `TransposingMatrix/Setup/Setup.sql` | Generated: the template with the assembly and its hash |
| `TransposingMatrix/Setup/CleanUp.sql` | Removes the installation |
| `TransposingMatrix/Test/Test.sql` | Examples to run in SSMS |
| `tools/SetupScript/` | Generates Setup.sql (.NET 10) |
| `tests/TransposingMatrix.Tests/` | xUnit tests against SQL Server in a container (.NET 10) |
