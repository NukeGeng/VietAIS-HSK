using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace VietAisHsk.Api.Infrastructure;

public interface IWorkerCallbackAuthenticator
{
    bool IsConfigured { get; }

    bool TryVerify(HttpRequest request, ReadOnlySpan<byte> payload, out string failureReason);
}

public sealed class HmacWorkerCallbackAuthenticator : IWorkerCallbackAuthenticator
{
    private const string TimestampHeader = "X-VietAIS-Worker-Timestamp";
    private const string SignatureHeader = "X-VietAIS-Worker-Signature";
    private readonly byte[] secret;
    private readonly TimeSpan maxClockSkew;

    public HmacWorkerCallbackAuthenticator(string? sharedSecret, int maxClockSkewSeconds = 300)
    {
        secret = string.IsNullOrWhiteSpace(sharedSecret) ? [] : Encoding.UTF8.GetBytes(sharedSecret.Trim());
        maxClockSkew = TimeSpan.FromSeconds(Math.Clamp(maxClockSkewSeconds, 30, 3600));
    }

    public bool IsConfigured => secret.Length > 0;

    public bool TryVerify(HttpRequest request, ReadOnlySpan<byte> payload, out string failureReason)
    {
        failureReason = string.Empty;
        if (!IsConfigured)
        {
            failureReason = "Worker callback secret chưa được cấu hình.";
            return false;
        }

        if (!request.Headers.TryGetValue(TimestampHeader, out var timestampHeader)
            || !long.TryParse(timestampHeader.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestamp))
        {
            failureReason = "Thiếu timestamp hợp lệ.";
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs((double)now - timestamp) > maxClockSkew.TotalSeconds)
        {
            failureReason = "Timestamp callback đã hết thời hạn.";
            return false;
        }

        if (!request.Headers.TryGetValue(SignatureHeader, out var signatureHeader))
        {
            failureReason = "Thiếu chữ ký callback.";
            return false;
        }

        var encodedSignature = signatureHeader.ToString();
        if (encodedSignature.StartsWith("v1=", StringComparison.Ordinal))
        {
            encodedSignature = encodedSignature[3..];
        }

        byte[] providedSignature;
        try
        {
            providedSignature = Base64UrlDecode(encodedSignature);
        }
        catch (FormatException)
        {
            failureReason = "Chữ ký callback không đúng định dạng.";
            return false;
        }

        var expectedSignature = ComputeSignature(timestamp, payload);
        if (providedSignature.Length != expectedSignature.Length
            || !CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
        {
            failureReason = "Chữ ký callback không hợp lệ.";
            return false;
        }

        return true;
    }

    public static string Sign(string sharedSecret, long timestamp, ReadOnlySpan<byte> payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sharedSecret);
        var signer = new HmacWorkerCallbackAuthenticator(sharedSecret);
        return "v1=" + Base64UrlEncode(signer.ComputeSignature(timestamp, payload));
    }

    private byte[] ComputeSignature(long timestamp, ReadOnlySpan<byte> payload)
    {
        var prefix = Encoding.UTF8.GetBytes(timestamp.ToString(CultureInfo.InvariantCulture) + "\n");
        var signedPayload = new byte[prefix.Length + payload.Length];
        prefix.CopyTo(signedPayload, 0);
        payload.CopyTo(signedPayload.AsSpan(prefix.Length));
        return HMACSHA256.HashData(secret, signedPayload);
    }

    private static byte[] Base64UrlDecode(string value) =>
        Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
