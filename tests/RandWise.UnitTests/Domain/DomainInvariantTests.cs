using RandWise.Domain.Common;
using RandWise.Domain.Entities;
using RandWise.Domain.Enums;

namespace RandWise.UnitTests.Domain;

public sealed class DomainInvariantTests
{
    private static readonly DateTime NowUtc = new(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Transaction_Create_RejectsZeroAmountCents()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Transaction.Create(
                "txn-1",
                "user-1",
                "cat-1",
                0,
                TransactionType.Expense,
                "Petrol",
                "Shell",
                new DateOnly(2026, 6, 14),
                TransactionSource.Web,
                TransactionStatus.Confirmed,
                null,
                NowUtc));

        Assert.Contains("amountCents", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Transaction_Create_StoresIntegerCentsAndDateOnlyValues()
    {
        var transactionDate = new DateOnly(2026, 6, 14);

        var transaction = Transaction.Create(
            "txn-1",
            "user-1",
            "cat-1",
            25_000,
            TransactionType.Expense,
            "Petrol",
            "Shell",
            transactionDate,
            TransactionSource.WhatsApp,
            TransactionStatus.Confirmed,
            9_500,
            NowUtc);

        Assert.Equal(25_000, transaction.AmountCents);
        Assert.Equal(transactionDate, transaction.TransactionDate);
        Assert.Equal(DateTimeKind.Utc, transaction.CreatedUtc.Kind);
    }

    [Fact]
    public void Transaction_Create_RejectsOutOfRangeConfidence()
    {
        Assert.Throws<DomainException>(() =>
            Transaction.Create(
                "txn-1",
                "user-1",
                "cat-1",
                25_000,
                TransactionType.Expense,
                "Petrol",
                null,
                new DateOnly(2026, 6, 14),
                TransactionSource.WhatsApp,
                TransactionStatus.NeedsReview,
                10_001,
                NowUtc));
    }

    [Fact]
    public void Transaction_DeleteAndRestore_PreservesReviewStatusForMediumConfidence()
    {
        var transaction = Transaction.Create(
            "txn-1",
            "user-1",
            "cat-1",
            25_000,
            TransactionType.Expense,
            "Petrol",
            null,
            new DateOnly(2026, 6, 14),
            TransactionSource.WhatsApp,
            TransactionStatus.NeedsReview,
            7_500,
            NowUtc);

        transaction.MarkDeleted(NowUtc.AddMinutes(1));
        transaction.Restore(NowUtc.AddMinutes(2));

        Assert.Null(transaction.DeletedUtc);
        Assert.Equal(TransactionStatus.NeedsReview, transaction.Status);
    }

    [Fact]
    public void BudgetPeriod_Create_RejectsEndDateBeforeStartDate()
    {
        Assert.Throws<DomainException>(() =>
            BudgetPeriod.Create(
                "period-1",
                "user-1",
                new DateOnly(2026, 6, 25),
                new DateOnly(2026, 6, 24),
                100_000,
                0,
                NowUtc));
    }

    [Fact]
    public void BudgetPeriod_DaysRemaining_UsesInclusiveDateOnlyMath()
    {
        var period = BudgetPeriod.Create(
            "period-1",
            "user-1",
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            100_000,
            0,
            NowUtc);

        Assert.Equal(17, period.DaysRemaining(new DateOnly(2026, 6, 14)));
        Assert.Equal(0, period.DaysRemaining(new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void FinancialProfile_Configure_RejectsInvalidPayday()
    {
        var profile = FinancialProfile.Create("profile-1", "user-1", NowUtc);

        Assert.Throws<DomainException>(() =>
            profile.Configure(
                100_000,
                32,
                BudgetCycleType.PaydayToPayday,
                0,
                10_000,
                5_000,
                NotificationMode.Confirm,
                DayOfWeek.Monday,
                NowUtc));
    }

    [Fact]
    public void FinancialProfile_Configure_RejectsNegativeProtectedMoney()
    {
        var profile = FinancialProfile.Create("profile-1", "user-1", NowUtc);

        Assert.Throws<DomainException>(() =>
            profile.Configure(
                100_000,
                25,
                BudgetCycleType.PaydayToPayday,
                0,
                -1,
                5_000,
                NotificationMode.Confirm,
                DayOfWeek.Monday,
                NowUtc));
    }

    [Fact]
    public void AppUser_Create_RequiresUtcTimestamp()
    {
        var localTimestamp = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Local);

        Assert.Throws<DomainException>(() =>
            AppUser.Create("user-1", "identity-1", "User", localTimestamp));
    }

    [Fact]
    public void UserCategoryRule_Create_NormalizesMatchValue()
    {
        var rule = UserCategoryRule.Create(
            "rule-1",
            "user-1",
            CategoryRuleMatchType.Keyword,
            "  Pick N Pay  ",
            "cat-1",
            10,
            NowUtc);

        Assert.Equal("Pick N Pay", rule.MatchValue);
        Assert.Equal("pick n pay", rule.NormalizedMatchValue);
    }

    [Fact]
    public void CategoryBudget_Create_RejectsInvalidWarningThreshold()
    {
        Assert.Throws<DomainException>(() =>
            CategoryBudget.Create(
                "budget-1",
                "period-1",
                "cat-1",
                100_000,
                0,
                101,
                NowUtc));
    }
}
