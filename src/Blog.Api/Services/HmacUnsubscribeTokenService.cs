using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Blog.Api.Services;

public class UnsubscribeTokenOptions
{
    public string HmacKey { get; set; } = string.Empty;
    public string? PreviousHmacKey { get; set; }
}

public class HmacUnsubscribeTokenService : IUnsubscribeTokenService
{
    private readonly byte[] _key;
    private readonly byte[]? _previousKey;

    public HmacUnsubscribeTokenService(IOptions<UnsubscribeTokenOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.HmacKey);
        if (!string.IsNullOrEmpty(options.Value.PreviousHmacKey))
            _previousKey = Convert.FromBase64String(options.Value.PreviousHmacKey);
    }

    public string GenerateToken(Guid subscriberId)
    {
        var subscriberBytes = subscriberId.ToByteArray();
        var hmac = ComputeHmac(_key, subscriberBytes);
        var combined = new byte[subscriberBytes.Length + hmac.Length];
        Buffer.BlockCopy(subscriberBytes, 0, combined, 0, subscriberBytes.Length);
        Buffer.BlockCopy(hmac, 0, combined, subscriberBytes.Length, hmac.Length);
        return Base64UrlEncode(combined);
    }

    public Guid? ValidateAndExtractSubscriberId(string token)
    {
        byte[] decoded;
        try
        {
            decoded = Base64UrlDecode(token);
        }
        catch
        {
            return null;
        }

        if (decoded.Length != 16 + 32) // Guid (16) + HMAC-SHA256 (32)
            return null;

        var subscriberBytes = decoded[..16];
        var providedHmac = decoded[16..];

        var expectedHmac = ComputeHmac(_key, subscriberBytes);
        if (CryptographicOperations.FixedTimeEquals(providedHmac, expectedHmac))
            return new Guid(subscriberBytes);

        if (_previousKey != null)
        {
            var previousHmac = ComputeHmac(_previousKey, subscriberBytes);
            if (CryptographicOperations.FixedTimeEquals(providedHmac, previousHmac))
                return new Guid(subscriberBytes);
        }

        return null;
    }

    private static byte[] ComputeHmac(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
