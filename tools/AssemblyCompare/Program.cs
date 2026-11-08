using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: AssemblyCompare <accepted.dll> <candidate.dll>");
    return 2;
}

static string Blob(MetadataReader reader, BlobHandle handle) =>
    Convert.ToHexString(reader.GetBlobBytes(handle));

static string String(MetadataReader reader, StringHandle handle) =>
    reader.GetString(handle);

static SortedDictionary<string, string> Snapshot(string path)
{
    using var stream = File.OpenRead(path);
    using var pe = new PEReader(stream);
    if (!pe.HasMetadata) throw new InvalidDataException($"No managed metadata: {path}");
    var reader = pe.GetMetadataReader();
    var result = new SortedDictionary<string, string>(StringComparer.Ordinal);

    var assembly = reader.GetAssemblyDefinition();
    result["assembly"] = $"{String(reader, assembly.Name)}|{assembly.Version}|{assembly.Flags}";

    foreach (var handle in reader.AssemblyReferences)
    {
        var item = reader.GetAssemblyReference(handle);
        var key = $"reference:{String(reader, item.Name)}";
        result[key] = $"{item.Version}|{item.Flags}|{Blob(reader, item.PublicKeyOrToken)}";
    }

    foreach (var typeHandle in reader.TypeDefinitions)
    {
        var type = reader.GetTypeDefinition(typeHandle);
        var typeName = $"{String(reader, type.Namespace)}.{String(reader, type.Name)}";
        result[$"type:{typeName}"] = $"{type.Attributes}|extends:{type.BaseType.Kind}";

        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            result[$"field:{typeName}.{String(reader, field.Name)}:{Blob(reader, field.Signature)}"] = field.Attributes.ToString();
        }

        foreach (var methodHandle in type.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);
            var key = $"method:{typeName}.{String(reader, method.Name)}:{Blob(reader, method.Signature)}";
            var il = method.RelativeVirtualAddress == 0
                ? Array.Empty<byte>()
                : (pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes() ?? Array.Empty<byte>()).ToArray();
            result[key] = $"{method.Attributes}|{method.ImplAttributes}|{Convert.ToHexString(SHA256.HashData(il))}|{il.Length}";
        }
    }

    return result;
}

var left = Snapshot(args[0]);
var right = Snapshot(args[1]);
var keys = left.Keys.Union(right.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal);
var differences = 0;

foreach (var key in keys)
{
    left.TryGetValue(key, out var leftValue);
    right.TryGetValue(key, out var rightValue);
    if (!StringComparer.Ordinal.Equals(leftValue, rightValue))
    {
        differences++;
        Console.WriteLine($"DIFF {key}\n  accepted: {leftValue ?? "<missing>"}\n  candidate: {rightValue ?? "<missing>"}");
    }
}

Console.WriteLine($"Compared {left.Count} accepted and {right.Count} candidate records; differences: {differences}.");
return differences == 0 ? 0 : 1;
