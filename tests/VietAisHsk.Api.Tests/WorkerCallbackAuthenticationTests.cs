using System.Text;
using Microsoft.AspNetCore.Http;
using VietAisHsk.Api.Infrastructure;

namespace VietAisHsk.Api.Tests;

public sealed class WorkerCallbackAuthenticationTests
{
    [Fact]
    public void Valid_timestamped_signature_is_accepted()
    {
        const string secret = "worker-callback-test-secret";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = Encoding.UTF8.GetBytes("{\"jobId\":\"job-1\",\"status\":\"Completed\"}");
        var request = new DefaultHttpContext().Request;
        request.Headers["X-VietAIS-Worker-Timestamp"] = timestamp.ToString();
        request.Headers["X-VietAIS-Worker-Signature"] = HmacWorkerCallbackAuthenticator.Sign(secret, timestamp, payload);

        var authenticator = new HmacWorkerCallbackAuthenticator(secret);

        Assert.True(authenticator.TryVerify(request, payload, out var failure));
        Assert.Empty(failure);
    }

    [Fact]
    public void Changed_payload_or_expired_timestamp_is_rejected()
    {
        const string secret = "worker-callback-test-secret";
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var payload = Encoding.UTF8.GetBytes("{\"jobId\":\"job-1\"}");
        var request = new DefaultHttpContext().Request;
        request.Headers["X-VietAIS-Worker-Timestamp"] = timestamp.ToString();
        request.Headers["X-VietAIS-Worker-Signature"] = HmacWorkerCallbackAuthenticator.Sign(secret, timestamp, payload);

        var authenticator = new HmacWorkerCallbackAuthenticator(secret, maxClockSkewSeconds: 60);

        Assert.False(authenticator.TryVerify(request, Encoding.UTF8.GetBytes("{\"jobId\":\"tampered\"}"), out _));
    }

    [Fact]
    public void Extreme_timestamp_is_rejected_without_integer_overflow()
    {
        const string secret = "worker-callback-test-secret";
        var timestamp = long.MinValue;
        var payload = Encoding.UTF8.GetBytes("{}");
        var request = new DefaultHttpContext().Request;
        request.Headers["X-VietAIS-Worker-Timestamp"] = timestamp.ToString();
        request.Headers["X-VietAIS-Worker-Signature"] = HmacWorkerCallbackAuthenticator.Sign(secret, timestamp, payload);

        var authenticator = new HmacWorkerCallbackAuthenticator(secret);

        Assert.False(authenticator.TryVerify(request, payload, out _));
    }

    [Fact]
    public void Missing_secret_does_not_authenticate_as_worker()
    {
        var request = new DefaultHttpContext().Request;
        var authenticator = new HmacWorkerCallbackAuthenticator("");

        Assert.False(authenticator.IsConfigured);
        Assert.False(authenticator.TryVerify(request, "{}"u8, out var failure));
        Assert.Contains("chưa được cấu hình", failure, StringComparison.OrdinalIgnoreCase);
    }
}
