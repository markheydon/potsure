using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Potsure.Data;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Deletion;
using Potsure.Infrastructure.Export;
using Potsure.Infrastructure.Persistence.Repositories;
using Potsure.Infrastructure.Queries;
using Potsure.Infrastructure.Reporting;
using Potsure.Infrastructure.Services;
using Potsure.Infrastructure.StatementImport;

namespace Potsure.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPotsureInfrastructureServices(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddScoped<StatementUploadRepository>();
        services.AddScoped<StatementImportService>();
        services.AddScoped<IncomePatternService>();
        services.AddScoped<ExpensePatternService>();
        services.AddScoped<PatternMergeSplitService>();
        services.AddScoped<TransactionAttributionService>();
        services.AddScoped<BudgetProfileSeeder>();
        services.AddScoped<BudgetPeriodService>();
        services.AddScoped<AllocationPresetService>();
        services.AddScoped<BudgetedIncomeSuggestionService>();
        services.AddScoped<BankPotService>();
        services.AddScoped<RecordedFundingService>();
        services.AddScoped<PlannedFundingService>();
        services.AddScoped<OneOffCategorisationService>();
        services.AddScoped<ForecastHorizonService>();
        services.AddScoped<RecurrenceReviewService>();
        services.AddScoped<UncategorisedTransactionQuery>();
        services.AddScoped<AttributedTransactionQuery>();
        services.AddScoped<CategoryPotBreakdownQuery>();
        services.AddScoped<RecurringSummaryQuery>();
        services.AddScoped<PeriodReportService>();
        services.AddScoped<RunwayReportService>();
        services.AddScoped<ExportSnapshotBuilder>();
        services.AddScoped<ExportService>();
        services.AddScoped<UserDataDeletionService>();

        services.AddDataProtection()
            .PersistKeysToDbContext<PotsureDataProtectionDbContext>();

        return services;
    }

    public static IServiceCollection AddPotsureDbContext(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        services.AddDbContext<PotsureDbContext>(configure);
        return services;
    }
}
