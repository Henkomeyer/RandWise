using RandWise.Infrastructure.WhatsApp;

namespace RandWise.UnitTests.WhatsApp;

public sealed class DeterministicWhatsAppParserTests
{
    private static readonly DateOnly ReceivedDate = new(2026, 06, 17);

    [Theory]
    [InlineData("R250 petrol", 25000, "expense", "Petrol")]
    [InlineData("250 petrol", 25000, "expense", "Petrol")]
    [InlineData("spent R250 on petrol", 25000, "expense", "Petrol")]
    [InlineData("+850 dog sitting", 85000, "income", "Dog Sitting")]
    [InlineData("income 1200 tutoring", 120000, "income", "Tutoring")]
    public void Parse_SupportedTransactionMessages_ReturnsCreateTransaction(
        string text,
        long amountInCents,
        string transactionType,
        string description)
    {
        var parser = new DeterministicWhatsAppParser();

        var parsed = parser.Parse(text, ReceivedDate);

        Assert.Equal("create-transaction", parsed.Intent);
        Assert.Equal(amountInCents, parsed.AmountInCents);
        Assert.Equal(transactionType, parsed.TransactionType);
        Assert.Equal(description, parsed.Description);
        Assert.Equal(ReceivedDate, parsed.TransactionDate);
        Assert.Equal(9500, parsed.ConfidenceBasisPoints);
    }

    [Fact]
    public void Parse_UnsupportedMessage_ReturnsUnsupported()
    {
        var parser = new DeterministicWhatsAppParser();

        var parsed = parser.Parse("please do something vague", ReceivedDate);

        Assert.Equal("unsupported", parsed.Intent);
        Assert.Null(parsed.AmountInCents);
        Assert.Equal(0, parsed.ConfidenceBasisPoints);
    }
}
