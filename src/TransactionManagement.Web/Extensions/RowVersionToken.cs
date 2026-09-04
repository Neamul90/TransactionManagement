namespace TransactionManagement.Web.Extensions;

/// <summary>
/// Converts the binary concurrency token to and from the Base64 form carried by the HTML form.
/// Parsing is defensive: a tampered or truncated token yields an empty array, which the
/// command validator then rejects with a friendly message instead of throwing.
/// </summary>
public static class RowVersionToken
{
    public static string ToToken(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    public static byte[] FromToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return [];
        }

        Span<byte> buffer = stackalloc byte[64];

        return Convert.TryFromBase64String(token, buffer, out var bytesWritten)
            ? buffer[..bytesWritten].ToArray()
            : [];
    }
}
