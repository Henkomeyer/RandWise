namespace RandWise.Application.Security;

public interface IRefreshTokenHasher
{
    string Hash(string refreshToken);

    bool Verify(string refreshToken, string tokenHash);
}
