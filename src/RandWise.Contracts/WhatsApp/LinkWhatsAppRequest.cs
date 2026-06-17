namespace RandWise.Contracts.WhatsApp;

public sealed record LinkWhatsAppRequest(
    string PhoneNumber,
    string? PlatformContactId);
