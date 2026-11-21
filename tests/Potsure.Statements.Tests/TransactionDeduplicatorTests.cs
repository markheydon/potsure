using System.Globalization;
using Potsure.Statements;

namespace Potsure.Statements.Tests;

public sealed class TransactionDeduplicatorTests
{
    [Fact]
    public void CreateKey_FallbackKey_IsInvariantUnderNonInvariantCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            MonzoCsvRow row = new(
                MonzoTransactionId: null,
                TransactionDate: new DateOnly(2026, 10, 1),
                TransactionTime: null,
                Type: "Card payment",
                Name: "Coffee shop",
                Amount: -12.5m,
                Currency: "GBP",
                Description: "Morning coffee",
                MoneyOut: null,
                MoneyIn: null);

            TransactionDedupeKey key = TransactionDeduplicator.CreateKey(row);

            Assert.Equal("2026-10-01|-12.50|morning coffee", key.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
