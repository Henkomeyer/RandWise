using RandWise.Infrastructure.Security;

namespace RandWise.UnitTests.Security;

public class CryptographicRefreshTokenGeneratorTests
{
    [Fact]
    public void Generate_ReturnsUrlSafeHighEntropyToken()
    {
        var generator = new CryptographicRefreshTokenGenerator();

        var token = generator.Generate();

        Assert.True(token.Length >= 80);
        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
    }

    [Fact]
    public void Generate_ReturnsDifferentValues()
    {
        var generator = new CryptographicRefreshTokenGenerator();

        var first = generator.Generate();
        var second = generator.Generate();

        Assert.NotEqual(first, second);
    }
}
