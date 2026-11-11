using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Potsure.Infrastructure.Features.Export;

/// <summary>
/// Serialises a user financial snapshot to the version 1 JSON bundle contract.
/// </summary>
public static class JsonBundleExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static byte[] Export(UserFinancialExportSnapshot snapshot)
    {
        object document = new
        {
            exportVersion = "1.0",
            exportedAt = snapshot.ExportedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            currency = "GBP",
            budgetProfile = new
            {
                preset = snapshot.Preset.ToString(),
                forecastHorizonMonths = snapshot.ForecastHorizonMonths,
                standingPlan = new
                {
                    estimatedIncomeAmount = snapshot.EstimatedIncomeAmount.HasValue
                        ? RoundMoney(snapshot.EstimatedIncomeAmount.Value)
                        : (decimal?)null,
                    estimatedIncomeCadence = snapshot.EstimatedIncomeCadence?.ToString(),
                    actualIncomeWindowMonths = snapshot.ActualIncomeWindowMonths,
                    potMappingMode = snapshot.PotMappingMode.ToString(),
                },
                categories = snapshot.Categories.Select(c => new
                {
                    kind = c.Kind.ToString(),
                    allocation = RoundMoney(c.Allocation),
                    recordedFundingDirect = RoundMoney(c.RecordedFundingDirect),
                    plannedFutureFundingDirect = RoundMoney(c.PlannedFutureFundingDirect),
                }),
                bankPots = snapshot.BankPots.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    categoryKind = p.CategoryKind.ToString(),
                    allocationToPot = p.AllocationToPot.HasValue ? RoundMoney(p.AllocationToPot.Value) : (decimal?)null,
                    recordedFunding = RoundMoney(p.RecordedFunding),
                }),
                monzoAccount = snapshot.MonzoAccount is null
                    ? null
                    : new
                    {
                        displayName = snapshot.MonzoAccount.DisplayName,
                        fingerprint = snapshot.MonzoAccount.Fingerprint,
                    },
            },
            incomeSources = snapshot.IncomeSources,
            recurringExpenses = snapshot.RecurringExpenses,
            assignments = snapshot.Assignments,
            transactions = snapshot.Transactions.Select(t => new
            {
                t.Id,
                monzoTransactionId = t.MonzoTransactionId,
                date = t.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                time = t.Time?.ToString("O", CultureInfo.InvariantCulture),
                t.Type,
                t.Name,
                amount = RoundMoney(t.Amount),
                t.Currency,
                t.Description,
                kind = t.Kind.ToString(),
                categoryKind = t.CategoryKind?.ToString(),
                mappedPot = t.MappedPotName,
                t.Tag,
            }),
        };

        return JsonSerializer.SerializeToUtf8Bytes(document, SerializerOptions);
    }

    private static decimal RoundMoney(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.ToEven);
}
