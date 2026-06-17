namespace RandWise.Application.Security;

public interface ISensitiveDataProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedText);
}
