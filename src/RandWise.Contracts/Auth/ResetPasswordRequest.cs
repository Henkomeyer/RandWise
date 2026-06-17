namespace RandWise.Contracts.Auth;

public sealed record ResetPasswordRequest(
    string Email,
    string ResetToken,
    string NewPassword);
