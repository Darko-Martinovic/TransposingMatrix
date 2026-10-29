namespace TransposingMatrix.Tests;

/// <summary>
/// Setup.sql and CleanUp.sql on a SQL Server of their own, since they change what the server
/// trusts. One test walks through installing, reinstalling and removing in order.
/// </summary>
public sealed class SetupTests : IAsyncLifetime
{
    private readonly SqlServer _server = new();

    public async ValueTask InitializeAsync()
    {
        await _server.StartAsync(enableClr: false);
        await _server.RunAsync("master", "CREATE DATABASE One; CREATE DATABASE Two;");
    }

    public ValueTask DisposeAsync() => _server.DisposeAsync();

    [Fact]
    public async Task Setup_and_clean_up_leave_the_server_as_they_found_it()
    {
        // With CLR disabled, Setup installs and says what to do
        var setup = await _server.RunScriptAsync("One", Repository.Setup);
        Assert.Empty(setup.Errors);
        Assert.Contains(setup.Messages, m => m.StartsWith("CLR is disabled on this server."));

        await _server.EnableClrAsync();
        Assert.Equal(["a", "1"], (await Transpose("One")).Table.Columns);

        // Again, and in a second database: still one trusted assembly
        Assert.Empty((await _server.RunScriptAsync("One", Repository.Setup)).Errors);
        Assert.Empty((await _server.RunScriptAsync("Two", Repository.Setup)).Errors);
        Assert.Equal(1, await TrustedAssemblies());

        // Cleaning up one database keeps the trust the other needs
        Assert.Empty((await _server.RunScriptAsync("Two", Repository.CleanUp)).Errors);
        Assert.Equal(0, await Count("Two", "SELECT COUNT(*) FROM sys.assemblies WHERE name = N'SimpleTalk.SQLCLR.Matrix'"));
        Assert.Equal(0, await Count("Two", "SELECT COUNT(*) FROM sys.schemas WHERE name = N'MATRIX'"));
        Assert.Equal(1, await TrustedAssemblies());
        Assert.Equal(["a", "1"], (await Transpose("One")).Table.Columns);

        // A table saved in the schema keeps it, and the last clean up removes the trust
        await _server.RunAsync("One", "EXEC MATRIX.Transposing @Query = N'SELECT 1 AS a, 2 AS b', @TableName = N'MATRIX.Saved';");
        var cleanUp = await _server.RunScriptAsync("One", Repository.CleanUp);
        Assert.Empty(cleanUp.Errors);
        Assert.Contains("The schema MATRIX still holds objects, so it stays.", cleanUp.Messages);
        Assert.Equal(0, await Count("One", "SELECT COUNT(*) FROM sys.assemblies WHERE name = N'SimpleTalk.SQLCLR.Matrix'"));
        Assert.Equal(0, await TrustedAssemblies());
    }

    private Task<Result> Transpose(string database) =>
        _server.RunAsync(database, "EXEC MATRIX.Transposing @Query = N'SELECT 1 AS a, 2 AS b';");

    private Task<int> TrustedAssemblies() =>
        Count("master", "SELECT COUNT(*) FROM sys.trusted_assemblies WHERE description LIKE N'simpletalk.sqlclr.matrix,%'");

    private async Task<int> Count(string database, string sql) =>
        int.Parse((await _server.RunAsync(database, sql)).Table.Rows[0][0]!);
}
