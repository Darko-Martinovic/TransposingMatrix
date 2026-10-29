using System.Security.Cryptography;
using System.Text;

namespace SetupScript;

/// <summary>
/// A hash of everything Setup.sql is built from: the assembly's sources, its project and the
/// template. Setup.sql records it, so a test can tell when Setup.sql is older than the sources.
/// Line endings don't count, so a checkout on Windows hashes as one on Linux.
/// </summary>
public static class SourceHash
{
    public static string Compute(string repositoryRoot)
    {
        var project = Path.Combine(repositoryRoot, "TransposingMatrix");
        var files = Directory.EnumerateFiles(project, "*.cs", SearchOption.AllDirectories)
            .Append(Path.Combine(project, "TransposingMatrix.csproj"))
            .Append(Path.Combine(project, "Setup", "Setup.template.sql"))
            .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/'))
            .Where(file => !file.Contains("/bin/") && !file.Contains("/obj/"))
            .Order(StringComparer.Ordinal);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files)
        {
            var text = File.ReadAllText(Path.Combine(repositoryRoot, file)).Replace("\r\n", "\n");
            hash.AppendData(Encoding.UTF8.GetBytes($"{file}\n{text}\n"));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
