namespace Po.PoDropSquare.Core.Utilities;

/// <summary>
/// Shared utility for generating ETags following DRY principle
/// </summary>
public static class ETagGenerator
{
    /// <summary>
    /// Generates an ETag from one or more values
    /// </summary>
    /// <param name="values">Values to include in the ETag hash</param>
    /// <returns>ETag string in quoted format</returns>
    public static string Generate(params object?[] values)
    {
        var content = string.Join("-", values.Select(v => v?.ToString() ?? ""));
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return $"\"{Convert.ToHexString(hash)[..16]}\"";
    }
}
