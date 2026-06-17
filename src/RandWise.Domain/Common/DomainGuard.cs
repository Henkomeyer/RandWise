namespace RandWise.Domain.Common;

internal static class DomainGuard
{
    public static string Required(string value, string name, int maxLength = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{name} is required.");
        }

        var trimmed = value.Trim();
        if (maxLength > 0 && trimmed.Length > maxLength)
        {
            throw new DomainException($"{name} must be {maxLength} characters or fewer.");
        }

        return trimmed;
    }

    public static string? Optional(string? value, string name, int maxLength = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (maxLength > 0 && trimmed.Length > maxLength)
        {
            throw new DomainException($"{name} must be {maxLength} characters or fewer.");
        }

        return trimmed;
    }

    public static long NonNegativeCents(long value, string name)
    {
        if (value < 0)
        {
            throw new DomainException($"{name} cannot be negative.");
        }

        return value;
    }

    public static long PositiveCents(long value, string name)
    {
        if (value <= 0)
        {
            throw new DomainException($"{name} must be greater than zero.");
        }

        return value;
    }

    public static int Range(int value, string name, int minimum, int maximum)
    {
        if (value < minimum || value > maximum)
        {
            throw new DomainException($"{name} must be between {minimum} and {maximum}.");
        }

        return value;
    }

    public static DateTime Utc(DateTime value, string name)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException($"{name} must be a UTC timestamp.");
        }

        return value;
    }

    public static void DateRange(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new DomainException("End date cannot be before start date.");
        }
    }
}
