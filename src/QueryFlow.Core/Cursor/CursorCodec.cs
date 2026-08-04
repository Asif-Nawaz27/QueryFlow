using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QueryFlow.Abstractions.Exceptions;

namespace QueryFlow.Core.Cursor;

/// <summary>
/// Encodes/decodes opaque, HMAC-signed cursor tokens. A cursor carries the sort key values of
/// the last row seen (see <see cref="CursorPayload"/>); signing makes it tamper-evident so
/// clients cannot forge a cursor to page into data they were not shown, and base64url encoding
/// keeps it URL-safe as a single query string value.
/// </summary>
internal static class CursorCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Encode(CursorPayload payload, byte[] key)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var signature = HMACSHA256.HashData(key, json);

        var body = Base64UrlEncode(json);
        var sig = Base64UrlEncode(signature);
        return $"{body}.{sig}";
    }

    public static CursorPayload Decode(string cursor, byte[] key)
    {
        var parts = cursor.Split('.', 2);
        if (parts.Length != 2)
        {
            throw new InvalidCursorException("Cursor is malformed.");
        }

        byte[] json;
        byte[] signature;
        try
        {
            json = Base64UrlDecode(parts[0]);
            signature = Base64UrlDecode(parts[1]);
        }
        catch (FormatException ex)
        {
            throw new InvalidCursorException("Cursor is not valid base64url.", ex);
        }

        var expectedSignature = HMACSHA256.HashData(key, json);
        if (!CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
        {
            throw new InvalidCursorException("Cursor signature verification failed.");
        }

        try
        {
            return JsonSerializer.Deserialize<CursorPayload>(json, JsonOptions)
                   ?? throw new InvalidCursorException("Cursor payload was empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidCursorException("Cursor payload could not be parsed.", ex);
        }
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        var padding = (4 - padded.Length % 4) % 4;
        padded += new string('=', padding);
        return Convert.FromBase64String(padded);
    }
}
