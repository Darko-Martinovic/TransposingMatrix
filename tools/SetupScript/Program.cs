using System.Reflection;
using System.Security.Cryptography;

using SetupScript;

// Setup.sql carries the assembly as a binary literal, with the hash SQL Server trusts it by
var metadata = typeof(SourceHash).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
    .ToDictionary(a => a.Key, a => a.Value!);
var root = metadata["RepositoryRoot"];
var dll = metadata["MatrixAssembly"];

var bits = File.ReadAllBytes(dll);
// The name SQL Server shows in sys.assemblies.clr_name, which trusted assemblies are described by
var description = AssemblyName.GetAssemblyName(dll).FullName.ToLowerInvariant() + ", processorarchitecture=msil";

var setup = Path.Combine(root, "TransposingMatrix", "Setup");
var script = File.ReadAllText(Path.Combine(setup, "Setup.template.sql"))
    .Replace("$(SourceHash)", SourceHash.Compute(root))
    .Replace("$(AssemblyHash)", "0x" + Convert.ToHexString(SHA512.HashData(bits)))
    .Replace("$(AssemblyDescription)", description)
    .Replace("$(AssemblyBits)", "0x" + Convert.ToHexString(bits));

File.WriteAllText(Path.Combine(setup, "Setup.sql"), script);
Console.WriteLine($"Wrote Setup.sql with {description}");
