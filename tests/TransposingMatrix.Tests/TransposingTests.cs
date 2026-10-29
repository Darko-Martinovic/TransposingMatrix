using System.Text.RegularExpressions;

namespace TransposingMatrix.Tests;

/// <summary>
/// Starts SQL Server and installs the committed Setup.sql in a database, once for all the tests.
/// </summary>
public sealed class MatrixFixture : IAsyncLifetime
{
    private const string Database = "TransposeTest";

    public SqlServer Server { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await Server.StartAsync();
        await Server.RunAsync("master", $"CREATE DATABASE {Database};");
        var setup = await Server.RunScriptAsync(Database, Repository.Setup);
        Assert.True(setup.Errors.Count == 0, string.Join("\n", setup.Errors.Select(e => e.Message)));
    }

    public Task<Result> RunAsync(string sql) => Server.RunAsync(Database, sql);

    public ValueTask DisposeAsync() => Server.DisposeAsync();
}

public sealed class TransposingTests(MatrixFixture matrix) : IClassFixture<MatrixFixture>
{
    private const string Databases = "SELECT name, database_id FROM sys.databases WHERE database_id <= 4 ORDER BY database_id";

    // The article's example of Phil Factor's: oil consumption by region and year
    private const string OilConsumption = """
        SELECT * FROM (VALUES
          (25119, 25002, 25109, 23860, 22957, 23518, 23330, 22926, 23365, 23418, 23644),
          (5332, 5504, 5780, 6057, 6054, 6384, 6624, 6782, 7035, 7190, 7083),
          (20213, 20426, 20166, 20086, 19276, 19223, 19075, 18605, 18372, 18266, 18380),
          (6576, 6711, 6935, 7440, 7855, 8201, 8455, 8770, 9011, 9353, 9570),
          (2917, 2928, 3063, 3236, 3315, 3486, 3413, 3579, 3678, 3763, 3888),
          (24569, 25157, 26035, 25900, 26244, 27954, 28893, 30001, 30588, 31119, 32444)
        ) oilConsumption ([2005], [2006], [2007], [2008], [2009], [2010], [2011], [2012], [2013], [2014], [2015])
        """;

    [Fact]
    public async Task The_first_column_becomes_the_header()
    {
        var result = await matrix.RunAsync($"EXEC MATRIX.Transposing @Query = N'{Databases}';");

        Assert.Equal(["name", "master", "tempdb", "model", "msdb"], result.Table.Columns);
        Assert.Equal(["database_id", "1", "2", "3", "4"], Assert.Single(result.Table.Rows));
    }

    [Fact]
    public async Task Rco_chooses_the_column_that_becomes_the_header()
    {
        var result = await matrix.RunAsync($"EXEC MATRIX.Transposing @Query = N'{Databases}', @Rco = 1;");

        Assert.Equal(["database_id", "1", "2", "3", "4"], result.Table.Columns);
        Assert.Equal(["name", "master", "tempdb", "model", "msdb"], Assert.Single(result.Table.Rows));
    }

    [Fact]
    public async Task Params_filter_before_transposing()
    {
        var result = await matrix.RunAsync("""
            EXEC MATRIX.Transposing
                 @Query = N'SELECT name, database_id FROM sys.databases WHERE database_id >= @id1 AND database_id <= @id2 ORDER BY database_id;',
                 @Params = N'@id1 int=2,@Id2 int=3';
            """);

        Assert.Equal(["name", "tempdb", "model"], result.Table.Columns);
    }

    // Version 1 parsed the declarations itself, and got these wrong
    [Theory]
    [InlineData("@v bigint = 5000000000", "5000000000")]
    [InlineData("@v varchar(10) = 'abc'", "abc")]
    [InlineData("@v decimal(10,2) = 1.5", "1.50")]
    [InlineData("@v datetime = '2020-01-01 13:45:00'", "2020-01-01 13:45:00.000")]
    [InlineData("@v datetime2(3) = '2020-01-01 13:45:00.123'", "2020-01-01 13:45:00.123")]
    [InlineData("@v nvarchar(20) = N'a, b'", "a, b")]
    [InlineData("@v tinyint = 255", "255")]
    [InlineData("@v uniqueidentifier = '6F9619FF-8B86-D011-B42D-00C04FC964FF'", "6f9619ff-8b86-d011-b42d-00c04fc964ff")]
    public async Task Params_take_any_type_SQL_Server_declares(string declaration, string expected)
    {
        var result = await matrix.RunAsync($"EXEC MATRIX.Transposing @Query = N'SELECT @v AS v', @Params = N'{declaration.Replace("'", "''")}', @KeyValueOption = 1;");

        Assert.Equal(["v", expected], result.Table.Row("v"));
    }

    [Fact]
    public async Task Params_take_several_declarations()
    {
        var result = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N'SELECT @a AS a, @b AS b', @Params = N'@a int = 1, @b nvarchar(5) = N''x''', @KeyValueOption = 1;");

        Assert.Equal(["a", "1"], result.Table.Row("a"));
        Assert.Equal(["b", "x"], result.Table.Row("b"));
    }

    [Theory]
    [InlineData("CAST('2020-01-01 10:00:00.1234567' AS datetime2)", "2020-01-01 10:00:00.1234567")]
    [InlineData("CAST('2020-01-01 10:00:00.12' AS datetime2(2))", "2020-01-01 10:00:00.12")]
    [InlineData("CAST('2020-01-01 10:00:00.1234567 +02:00' AS datetimeoffset)", "2020-01-01 10:00:00.1234567 +02:00")]
    [InlineData("CAST('2020-01-01 10:00:00' AS datetime)", "2020-01-01 10:00:00.000")]
    [InlineData("CAST('2020-01-01 10:00' AS smalldatetime)", "2020-01-01 10:00:00")]
    [InlineData("CAST('2020-02-29' AS date)", "2020-02-29")]
    [InlineData("CAST('10:11:12.5' AS time(1))", "10:11:12.5")]
    [InlineData("CAST(1.5 AS float)", "1.5")]
    [InlineData("CAST(1.25 AS decimal(5, 3))", "1.250")]
    [InlineData("CAST(12.34 AS money)", "12.3400")]
    [InlineData("CAST(1 AS bit)", "True")]
    [InlineData("CAST(0x0A0B AS varbinary(2))", "0x0A0B")]
    [InlineData("CAST(N'<a>1</a>' AS xml)", "<a>1</a>")]
    [InlineData("CAST(3.25 AS sql_variant)", "3.25")]
    [InlineData("geography::Point(1, 1, 4326)", "POINT (1 1)")]
    [InlineData("CAST('/1/2/' AS hierarchyid)", "/1/2/")]
    [InlineData("N''", "")]
    public async Task Values_keep_their_precision_as_text(string expression, string expected)
    {
        var result = await matrix.RunAsync($"EXEC MATRIX.Transposing @Query = N'SELECT {expression.Replace("'", "''")} AS v', @KeyValueOption = 1;");

        Assert.Equal(["v", expected], result.Table.Row("v"));
    }

    [Fact]
    public async Task Values_are_the_same_in_any_language()
    {
        // .NET takes its culture from the session's language
        var result = await matrix.RunAsync("""
            SET LANGUAGE German;
            EXEC MATRIX.Transposing @Query = N'SELECT CAST(1.5 AS decimal(3, 1)) AS d, CAST(2.5 AS float) AS f, SYSDATETIMEOFFSET() AS o', @KeyValueOption = 1;
            """);

        Assert.Equal("1.5", result.Table.Row("d")[1]);
        Assert.Equal("2.5", result.Table.Row("f")[1]);
        Assert.Matches(@"^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d\.\d{7} [+-]\d\d:\d\d$", result.Table.Row("o")[1]);
    }

    [Fact]
    public async Task Null_stays_null()
    {
        var result = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N'SELECT CAST(NULL AS int) AS v', @KeyValueOption = 1;");

        Assert.Equal(["v", null], result.Table.Row("v"));
    }

    [Fact]
    public async Task Key_value_option_gives_a_generic_header()
    {
        var result = await matrix.RunAsync($"EXEC MATRIX.Transposing @Query = N'{Databases}', @KeyValueOption = 1;");

        Assert.Equal(["Key", "Value", "Value1", "Value2", "Value3"], result.Table.Columns);
        Assert.Equal(["name", "master", "tempdb", "model", "msdb"], result.Table.Row("name"));
        Assert.Equal(["database_id", "1", "2", "3", "4"], result.Table.Row("database_id"));
    }

    [Fact]
    public async Task Column_mapping_names_the_header_and_generic_names_fill_the_rest()
    {
        var result = await matrix.RunAsync($"EXEC MATRIX.Transposing @Query = N'{Databases}', @KeyValueOption = 1, @ColumnMapping = N'Database name, Sys database master';");

        Assert.Equal(["Database name", "Sys database master", "Value2", "Value3", "Value4"], result.Table.Columns);
    }

    [Fact]
    public async Task Column_mapping_takes_bracketed_names()
    {
        var result = await matrix.RunAsync($"""
            EXEC MATRIX.Transposing @Query = N'{OilConsumption}', @Rco = 1, @KeyValueOption = 1,
                 @ColumnMapping = N'[Year],[Total North America],[Total S. & Cent. America],[Total Europe & Eurasia],[Total Middle East],[Total Africa],[Total Asia Pacific]';
            """);

        Assert.Equal(["Year", "Total North America", "Total S. & Cent. America", "Total Europe & Eurasia", "Total Middle East", "Total Africa", "Total Asia Pacific"], result.Table.Columns);
        Assert.Equal(["2005", "25119", "5332", "20213", "6576", "2917", "24569"], result.Table.Row("2005"));
        Assert.Equal(11, result.Table.Rows.Count);
    }

    [Fact]
    public async Task A_stored_procedure_is_transposed()
    {
        var result = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N'EXEC sp_who';");

        Assert.Equal("spid", result.Table.Columns[0]);
        Assert.Contains(result.Table.Rows, row => row[0] == "loginame");
    }

    [Fact]
    public async Task Every_result_is_transposed()
    {
        var result = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N'SELECT 1 AS a, 2 AS b; SET NOCOUNT ON; SELECT 3 AS c, 4 AS d;';");

        Assert.Equal(2, result.Tables.Count);
        Assert.Equal(["a", "1"], result.Tables[0].Columns);
        Assert.Equal(["c", "3"], result.Tables[1].Columns);
    }

    [Fact]
    public async Task The_header_is_made_of_names_a_table_accepts()
    {
        var result = await matrix.RunAsync("""
            EXEC MATRIX.Transposing @Query = N'SELECT k, v FROM (VALUES (N''K'', 1), (N''x'', 2), (N''x'', 3), (N'''', 4), (REPLICATE(N''n'', 200), 5), (NULL, 6)) AS t (k, v) ORDER BY v';
            """);

        // Repeated names get the suffix _N, empty ones are ColumnN, long ones are cut to 128 characters
        Assert.Equal(["k", "K_0", "x", "x_2", "Column5", new string('n', 128), "Column7"], result.Table.Columns);
    }

    [Fact]
    public async Task The_result_is_saved_in_a_global_temporary_table()
    {
        var result = await matrix.RunAsync($"""
            EXEC MATRIX.Transposing @Query = N'{Databases}', @TableName = N'##saved';
            SELECT * FROM ##saved;
            SELECT c.name, TYPE_NAME(c.user_type_id) AS type, c.max_length FROM tempdb.sys.columns AS c WHERE c.object_id = OBJECT_ID(N'tempdb..##saved') ORDER BY c.column_id;
            SELECT COUNT(*) FROM sys.types WHERE schema_id = SCHEMA_ID(N'MATRIX');
            DROP TABLE ##saved;
            """);

        Assert.Equal(result.Tables[0], result.Tables[1], TableComparer.Instance);
        // Each column is as long as its longest value: database_id in name, 1 in master
        Assert.Equal(["name", "nvarchar", "22"], result.Tables[2].Row("name"));
        Assert.Equal(["master", "nvarchar", "2"], result.Tables[2].Row("master"));
        // Version 1 left a table type behind for each table it saved in
        Assert.Equal("0", result.Tables[3].Rows[0][0]);
    }

    [Fact]
    public async Task A_saved_table_is_replaced()
    {
        var result = await matrix.RunAsync($"""
            EXEC MATRIX.Transposing @Query = N'SELECT 1 AS a, 2 AS b', @TableName = N'dbo.Saved';
            EXEC MATRIX.Transposing @Query = N'{Databases}', @TableName = N'dbo.Saved';
            SELECT * FROM dbo.Saved;
            DROP TABLE dbo.Saved;
            """);

        Assert.Equal(["name", "master", "tempdb", "model", "msdb"], result.Tables[^1].Columns);
    }

    [Fact]
    public async Task Only_the_first_result_is_saved()
    {
        var result = await matrix.RunAsync("""
            EXEC MATRIX.Transposing @Query = N'SELECT 1 AS a, 2 AS b; SELECT 3 AS c, 4 AS d;', @TableName = N'##first';
            SELECT * FROM ##first;
            DROP TABLE ##first;
            """);

        Assert.Contains("Only the first result is saved in ##first; result 2 is not.", result.Messages);
        Assert.Equal(["a", "1"], result.Tables[^1].Columns);
    }

    [Fact]
    public async Task A_local_temporary_table_is_refused()
    {
        var result = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N'SELECT 1 AS a, 2 AS b', @TableName = N'#local';");

        var error = Assert.Single(result.Errors);
        Assert.Contains("Use a global temporary table (##local)", error.Message);
        Assert.Empty(result.Tables);
    }

    [Fact]
    public async Task Only_the_first_thousand_rows_are_transposed()
    {
        var result = await matrix.RunAsync("""
            EXEC MATRIX.Transposing @Query = N'SELECT TOP (1500) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n, 1 AS x FROM sys.all_columns AS a CROSS JOIN sys.all_columns AS b', @TableName = N'##wide';
            SELECT COUNT(*) FROM tempdb.sys.columns WHERE object_id = OBJECT_ID(N'tempdb..##wide');
            DROP TABLE ##wide;
            """);

        Assert.Contains("The result has more than 1000 rows: only the first 1000 are transposed.", result.Messages);
        // The header and 1000 values; version 1 made it 1001
        Assert.Equal(1001, result.Tables[0].Columns.Count);
        Assert.Equal("1000", result.Tables[0].Columns[^1]);
        Assert.Equal("1001", result.Tables[1].Rows[0][0]);
    }

    [Fact]
    public async Task A_result_too_wide_for_a_table_row_is_not_saved()
    {
        var result = await matrix.RunAsync("""
            EXEC MATRIX.Transposing @Query = N'SELECT TOP (1000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n, N''ten chars.'' AS x FROM sys.all_columns', @TableName = N'##tooWide';
            SELECT OBJECT_ID(N'tempdb..##tooWide');
            """);

        var error = Assert.Single(result.Errors);
        Assert.StartsWith("The transposed result is too wide to save in a table: Cannot create a row of size", error.Message);
        Assert.Null(result.Tables[^1].Rows[0][0]);
    }

    [Fact]
    public async Task An_rco_beyond_the_columns_is_an_error()
    {
        var result = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N'SELECT 1 AS a, 2 AS b', @Rco = 2; SELECT @@ERROR;");

        var error = Assert.Single(result.Errors);
        Assert.Equal("@Rco is 2, but the result's columns are numbered 0 to 1.", error.Message);
        Assert.Equal("50000", result.Tables[^1].Rows[0][0]);
    }

    [Fact]
    public async Task A_failing_query_is_one_error()
    {
        var result = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N'SELECT * FROM dbo.NoSuchTable';");

        var error = Assert.Single(result.Errors);
        Assert.Equal(50000, error.Number);
        Assert.Equal("MATRIX.Transposing failed: Invalid object name 'dbo.NoSuchTable'.", error.Message);
    }

    [Fact]
    public async Task A_query_is_required()
    {
        var result = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N' ';");

        Assert.Equal("@Query is required.", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public async Task Null_options_are_their_defaults()
    {
        var result = await matrix.RunAsync($"EXEC MATRIX.Transposing @Query = N'{Databases}', @Rco = NULL, @KeyValueOption = NULL;");

        Assert.Equal(["name", "master", "tempdb", "model", "msdb"], result.Table.Columns);
    }

    [Theory]
    [InlineData("MATRIX.Transposing")]
    [InlineData("[MATRIX].[Transposing]")]
    [InlineData("transposing")]
    public async Task Help_describes_the_parameters(string name)
    {
        var result = await matrix.RunAsync($"SELECT * FROM MATRIX.Help(N'{name}');");

        Assert.Equal(["ParameterName", "ParameterDescription"], result.Table.Columns);
        Assert.Equal(["@Query", "@Params", "@Rco", "@KeyValueOption", "@ColumnMapping", "@TableName"], result.Table.Rows.Select(row => row[0]));
    }

    [Fact]
    public async Task Help_names_an_unknown_procedure()
    {
        var result = await matrix.RunAsync("SELECT * FROM MATRIX.Help(N'dbo.Other');");

        Assert.Equal(["Error", "The procedure or the function dbo.Other does not exist!"], Assert.Single(result.Table.Rows));
    }

    [Fact]
    public async Task Json_columns_are_cast_to_text()
    {
        Assert.SkipWhen(matrix.Server.MajorVersion < 17, "json is a type of SQL Server 2025");

        var failed = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N'SELECT CAST(N''{\"a\":1}'' AS json) AS j', @KeyValueOption = 1;");
        var cast = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N'SELECT CAST(CAST(N''{\"a\":1}'' AS json) AS nvarchar(max)) AS j', @KeyValueOption = 1;");

        Assert.EndsWith("If the query returns json or vector columns, CAST them to nvarchar(max).", Assert.Single(failed.Errors).Message);
        Assert.Equal(["j", "{\"a\":1}"], cast.Table.Row("j"));
    }

    [Fact]
    public async Task Vector_columns_are_cast_to_text()
    {
        Assert.SkipWhen(matrix.Server.MajorVersion < 17, "vector is a type of SQL Server 2025");

        var result = await matrix.RunAsync("EXEC MATRIX.Transposing @Query = N'SELECT CAST(CAST(''[1,2,3]'' AS vector(3)) AS nvarchar(max)) AS v', @KeyValueOption = 1;");

        Assert.StartsWith("[1", result.Table.Row("v")[1]);
    }

    [Fact]
    public void Setup_sql_is_built_from_the_current_sources()
    {
        var recorded = Regex.Match(Repository.Setup, "^-- Sources: ([0-9a-f]{64})$", RegexOptions.Multiline).Groups[1].Value;

        Assert.True(recorded == SetupScript.SourceHash.Compute(Repository.Root),
            "Setup.sql is older than the sources: dotnet run -c Release --project tools/SetupScript");
    }

    private sealed class TableComparer : IEqualityComparer<Table>
    {
        public static readonly TableComparer Instance = new();

        public bool Equals(Table? x, Table? y) =>
            x is not null && y is not null && x.Columns.SequenceEqual(y.Columns)
            && x.Rows.Count == y.Rows.Count && x.Rows.Zip(y.Rows).All(pair => pair.First.SequenceEqual(pair.Second));

        public int GetHashCode(Table obj) => obj.Columns.Count;
    }
}
