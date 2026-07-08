using POS.Api.Middleware;

namespace POS.ContractTests;

/// <summary>
/// Khóa logic checksum/timestamp-window của PosApiKeyMiddleware — thay checksum sai vẫn pass,
/// hoặc cửa sổ timestamp bị nới lỏng ngoài ý muốn → test đỏ.
/// </summary>
public class PosApiKeyChecksumTests
{
    [Fact]
    public void Compute_SameInputs_ProducesSameChecksum()
    {
        var c1 = PosApiKeyChecksum.Compute("req-1", 1_700_000_000, "secret", "POS001");
        var c2 = PosApiKeyChecksum.Compute("req-1", 1_700_000_000, "secret", "POS001");

        Assert.Equal(c1, c2);
    }

    [Theory]
    [InlineData("req-2", 1_700_000_000, "secret", "POS001")]   // requestId khác
    [InlineData("req-1", 1_700_000_001, "secret", "POS001")]   // timestamp khác
    [InlineData("req-1", 1_700_000_000, "other-secret", "POS001")] // secret khác
    [InlineData("req-1", 1_700_000_000, "secret", "POS002")]   // posNo khác
    public void Compute_DifferentInputs_ProducesDifferentChecksum(
        string requestId, long timestamp, string secret, string posNo)
    {
        var baseline = PosApiKeyChecksum.Compute("req-1", 1_700_000_000, "secret", "POS001");
        var other = PosApiKeyChecksum.Compute(requestId, timestamp, secret, posNo);

        Assert.NotEqual(baseline, other);
    }

    [Fact]
    public void Verify_MatchingChecksum_ReturnsTrue()
    {
        var checksum = PosApiKeyChecksum.Compute("req-1", 1_700_000_000, "secret", "POS001");

        Assert.True(PosApiKeyChecksum.Verify(checksum, checksum));
        // Case-insensitive hex vẫn phải khớp (client có thể gửi lowercase)
        Assert.True(PosApiKeyChecksum.Verify(checksum, checksum.ToLowerInvariant()));
    }

    [Fact]
    public void Verify_WrongChecksum_ReturnsFalse()
    {
        var expected = PosApiKeyChecksum.Compute("req-1", 1_700_000_000, "secret", "POS001");
        var wrong = PosApiKeyChecksum.Compute("req-1", 1_700_000_000, "secret", "POS002");

        Assert.False(PosApiKeyChecksum.Verify(expected, wrong));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hex-string!!")]
    [InlineData("AB")] // hex hợp lệ nhưng sai độ dài so với SHA-256 (32 byte)
    public void Verify_MalformedActual_ReturnsFalseWithoutThrowing(string? actual)
    {
        var expected = PosApiKeyChecksum.Compute("req-1", 1_700_000_000, "secret", "POS001");

        Assert.False(PosApiKeyChecksum.Verify(expected, actual));
    }

    [Fact]
    public void IsWithinWindow_ExactlyAtBoundary_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.AddMinutes(-10).ToUnixTimeSeconds();

        Assert.True(PosApiKeyChecksum.IsWithinWindow(timestamp, now, windowMinutes: 10));
    }

    [Fact]
    public void IsWithinWindow_PastBoundary_ReturnsFalse()
    {
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.AddMinutes(-11).ToUnixTimeSeconds();

        Assert.False(PosApiKeyChecksum.IsWithinWindow(timestamp, now, windowMinutes: 10));
    }

    [Fact]
    public void IsWithinWindow_FutureTimestampWithinWindow_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.AddMinutes(5).ToUnixTimeSeconds();

        Assert.True(PosApiKeyChecksum.IsWithinWindow(timestamp, now, windowMinutes: 10));
    }
}
