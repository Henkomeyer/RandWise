namespace RandWise.Application.WhatsApp;

public interface IDeterministicWhatsAppParser
{
    ParsedWhatsAppMessage Parse(string text, DateOnly receivedDate);
}
