namespace RandWise.Infrastructure.WhatsApp;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public string VerifyToken { get; set; } = "randwise-local-verify-token";
    public string AppSecret { get; set; } = string.Empty;
}
