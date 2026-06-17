namespace RandWise.Contracts.WhatsApp;

public sealed record WhatsAppStatusResponse(
    bool IsLinked,
    bool IsVerified,
    string? PlatformContactId,
    DateTime? VerifiedUtc);
