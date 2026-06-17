namespace RandWise.Application.Security;

public interface IAccessTokenIssuer
{
    string IssueAccessToken(AccessTokenDescriptor descriptor);
}
