using RandWise.Infrastructure.Security;

namespace RandWise.UnitTests.Security;

public class Sha256RefreshTokenHasherTests
{
    [Fact]
    public void Hash_DoesNotStoreRawRefreshToken()
    {
        var hasher = new Sha256RefreshTokenHasher();
        const string refreshToken = "refresh-token-value";

        var hash = hasher.Hash(refreshToken);

        Assert.StartsWith("sha256:", hash);
        Assert.DoesNotContain(refreshToken, hash);
    }

    [Fact]
    public void Verify_AcceptsOriginalToken()
    {
        var hasher = new Sha256RefreshTokenHasher();
        const string refreshToken = "refresh-token-value";
        var hash = hasher.Hash(refreshToken);

        var verified = hasher.Verify(refreshToken, hash);

        Assert.True(verified);
    }

    [Fact]
    public void Verify_RejectsDifferentToken()
    {
        var hasher = new Sha256RefreshTokenHasher();
        var hash = hasher.Hash("refresh-token-value");

        var verified = hasher.Verify("different-refresh-token-value", hash);

        Assert.False(verified);
    }
}
