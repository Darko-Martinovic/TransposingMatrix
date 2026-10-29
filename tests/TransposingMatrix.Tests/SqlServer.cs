using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.Data.SqlClient;

using Testcontainers.MsSql;

namespace TransposingMatrix.Tests;

/// <summary>What a batch returned: its results, its messages and its errors.</summary>
public sealed record Result(IReadOnlyList<Table> Tables, IReadOnlyList<string> Messages, IReadOnlyList<SqlError> Errors)
{
    public Table Table => Assert.Single(Tables);
}

public sealed record Table(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string?>> Rows)
{
    /// <summary>The row that starts with this value: a column of the query, when transposed.</summary>
    public IReadOnlyList<string?> Row(string first) => Rows.Single(row => row[0] == first);
}

/// <summary>
/// SQL Server in a container: 2025 unless MSSQL_IMAGE names another image, as CI does for 2022.
/// CLR is enabled, as Setup.sql expects.
/// </summary>
public sealed partial class SqlServer : IAsyncDisposable
{
    private static readonly string Image =
        Environment.GetEnvironmentVariable("MSSQL_IMAGE") ?? "mcr.microsoft.com/mssql/server:2025-latest";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();

    public int MajorVersion { get; private set; }

    public async Task StartAsync(bool enableClr = true)
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception e)
        {
            // Show why SQL Server didn't start, not only that it didn't
            var (stdout, stderr) = await _container.GetLogsAsync();
            throw new InvalidOperationException($"SQL Server didn't start:\n{stdout}\n{stderr}", e);
        }

        if (enableClr)
            await EnableClrAsync();
        var version = await RunAsync("master", "SELECT CAST(SERVERPROPERTY('ProductMajorVersion') AS nvarchar(10));");
        MajorVersion = int.Parse(version.Table.Rows[0][0]!);
    }

    public Task EnableClrAsync() =>
        RunAsync("master", "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'clr enabled', 1; RECONFIGURE;");

    /// <summary>
    /// Runs a batch. Errors don't throw: they are collected, with the messages, as a client
    /// sees them.
    /// </summary>
    public async Task<Result> RunAsync(string database, string sql)
    {
        var messages = new List<string>();
        var errors = new List<SqlError>();
        var tables = new List<Table>();

        await using var connection = new SqlConnection(ConnectionString(database)) { FireInfoMessageEventOnUserErrors = true };
        connection.InfoMessage += (_, e) =>
        {
            foreach (SqlError error in e.Errors)
            {
                if (error.Class > 10)
                    errors.Add(error);
                else
                    messages.Add(error.Message);
            }
        };
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        await using var reader = await command.ExecuteReaderAsync();
        do
        {
            if (reader.FieldCount == 0)
                continue;

            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
            var rows = new List<IReadOnlyList<string?>>();
            while (await reader.ReadAsync())
                rows.Add(Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i))).ToList());
            tables.Add(new Table(columns, rows));
        } while (await reader.NextResultAsync());

        return new Result(tables, messages, errors);
    }

    /// <summary>Runs a script of batches separated by GO, as SSMS and sqlcmd do.</summary>
    public async Task<Result> RunScriptAsync(string database, string script)
    {
        var messages = new List<string>();
        var errors = new List<SqlError>();
        foreach (var batch in Go().Split(script).Where(b => b.Trim().Length > 0))
        {
            var result = await RunAsync(database, batch);
            messages.AddRange(result.Messages);
            errors.AddRange(result.Errors);
        }

        return new Result([], messages, errors);
    }

    public string ConnectionString(string database) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = database }.ToString();

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex Go();
}

public static class Repository
{
    public static string Root { get; } = typeof(Repository).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(a => a.Key == "RepositoryRoot").Value!;

    public static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    public static string Setup => Read("TransposingMatrix/Setup/Setup.sql");

    public static string CleanUp => Read("TransposingMatrix/Setup/CleanUp.sql");
}
