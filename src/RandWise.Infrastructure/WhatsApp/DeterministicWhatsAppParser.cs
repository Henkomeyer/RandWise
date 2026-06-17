using System.Globalization;
using System.Text.RegularExpressions;
using RandWise.Application.WhatsApp;

namespace RandWise.Infrastructure.WhatsApp;

public sealed partial class DeterministicWhatsAppParser : IDeterministicWhatsAppParser
{
    private const string Version = "deterministic-v1";

    public ParsedWhatsAppMessage Parse(string text, DateOnly receivedDate)
    {
        var normalized = Normalize(text);
        if (normalized.Length == 0)
        {
            return Unsupported();
        }

        if (normalized is "help" or "undo last" or "how much left")
        {
            return new ParsedWhatsAppMessage(normalized.Replace(' ', '-'), null, null, null, null, null, 9000, Version);
        }

        var incomeMatch = IncomePattern().Match(normalized);
        if (incomeMatch.Success)
        {
            return CreateTransaction(incomeMatch, "income", receivedDate);
        }

        var spentMatch = SpentPattern().Match(normalized);
        if (spentMatch.Success)
        {
            return CreateTransaction(spentMatch, "expense", receivedDate);
        }

        var simpleMatch = SimpleAmountPattern().Match(normalized);
        if (simpleMatch.Success)
        {
            var type = normalized.StartsWith('+') ? "income" : "expense";
            return CreateTransaction(simpleMatch, type, receivedDate);
        }

        return Unsupported();
    }

    private static ParsedWhatsAppMessage CreateTransaction(Match match, string transactionType, DateOnly receivedDate)
    {
        var amount = match.Groups["amount"].Value.Replace(",", ".", StringComparison.Ordinal);
        var description = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(match.Groups["description"].Value.Trim());
        var cents = (long)Math.Round(decimal.Parse(amount, CultureInfo.InvariantCulture) * 100, MidpointRounding.AwayFromZero);

        return new ParsedWhatsAppMessage(
            "create-transaction",
            cents,
            transactionType,
            description,
            null,
            receivedDate,
            9500,
            Version);
    }

    private static ParsedWhatsAppMessage Unsupported() =>
        new("unsupported", null, null, null, null, null, 0, Version);

    private static string Normalize(string text) =>
        Regex.Replace(text.Trim().ToLowerInvariant(), @"\s+", " ");

    [GeneratedRegex(@"^income\s+r?(?<amount>\d+(?:[\.,]\d{1,2})?)\s+(?<description>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex IncomePattern();

    [GeneratedRegex(@"^spent\s+r?(?<amount>\d+(?:[\.,]\d{1,2})?)\s+on\s+(?<description>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex SpentPattern();

    [GeneratedRegex(@"^\+?r?(?<amount>\d+(?:[\.,]\d{1,2})?)\s+(?<description>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex SimpleAmountPattern();
}
