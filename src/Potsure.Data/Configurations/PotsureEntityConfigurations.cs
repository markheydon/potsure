using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Potsure.Data.Entities;

namespace Potsure.Data.Configurations;

internal sealed class BudgetProfileConfiguration : IEntityTypeConfiguration<BudgetProfileEntity>
{
    public void Configure(EntityTypeBuilder<BudgetProfileEntity> builder)
    {
        builder.ToTable("BudgetProfiles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerKey).HasMaxLength(450).IsRequired();
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.HasIndex(e => e.OwnerKey).IsUnique();
        builder.Property(e => e.EstimatedIncomeAmount).HasPrecision(18, 2);
        builder.Property(e => e.ActualIncomeWindowMonths).HasDefaultValue(3);
        builder.HasOne(e => e.MonzoAccount)
            .WithMany()
            .HasForeignKey(e => e.MonzoAccountId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.Categories)
            .WithOne(c => c.BudgetProfile)
            .HasForeignKey(c => c.BudgetProfileId);
        builder.HasMany(e => e.Pots)
            .WithOne(p => p.BudgetProfile)
            .HasForeignKey(p => p.BudgetProfileId);
    }
}

internal sealed class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategoryEntity>
{
    public void Configure(EntityTypeBuilder<BudgetCategoryEntity> builder)
    {
        builder.ToTable("BudgetCategories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerKey).HasMaxLength(450).IsRequired();
        builder.Property(e => e.AllocationAmount).HasPrecision(18, 2);
        builder.Property(e => e.RecordedFundingDirectAmount).HasPrecision(18, 2);
        builder.Property(e => e.PlannedFutureFundingDirectAmount).HasPrecision(18, 2);
        builder.HasIndex(e => new { e.BudgetProfileId, e.Kind }).IsUnique();
    }
}

internal sealed class BankPotConfiguration : IEntityTypeConfiguration<BankPotEntity>
{
    public void Configure(EntityTypeBuilder<BankPotEntity> builder)
    {
        builder.ToTable("BankPots");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerKey).HasMaxLength(450).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.AllocationToPotAmount).HasPrecision(18, 2);
        builder.Property(e => e.RecordedFundingAmount).HasPrecision(18, 2);
        builder.Property(e => e.PlannedFutureFundingAmount).HasPrecision(18, 2);
        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MonzoAccountConfiguration : IEntityTypeConfiguration<MonzoAccountAssociationEntity>
{
    public void Configure(EntityTypeBuilder<MonzoAccountAssociationEntity> builder)
    {
        builder.ToTable("MonzoAccounts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerKey).HasMaxLength(450).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(200);
        builder.Property(e => e.AccountFingerprint).HasMaxLength(256);
        builder.HasIndex(e => e.OwnerKey).IsUnique();
    }
}

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<TransactionEntity>
{
    public void Configure(EntityTypeBuilder<TransactionEntity> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerKey).HasMaxLength(450).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.MonzoTransactionId).HasMaxLength(64);
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(512);
        builder.Property(e => e.DedupeKey).HasMaxLength(512).IsRequired();
        builder.HasIndex(e => new { e.OwnerKey, e.DedupeKey }).IsUnique();
        builder.HasOne(e => e.UploadBatch)
            .WithMany(u => u.Transactions)
            .HasForeignKey(e => e.UploadBatchId);
        builder.HasOne<BudgetCategoryEntity>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class StatementUploadConfiguration : IEntityTypeConfiguration<StatementUploadEntity>
{
    public void Configure(EntityTypeBuilder<StatementUploadEntity> builder)
    {
        builder.ToTable("StatementUploads");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerKey).HasMaxLength(450).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(260).IsRequired();
        builder.Property(e => e.SkippedReasonsJson).HasMaxLength(8000);
    }
}

internal sealed class IncomeSourceConfiguration : IEntityTypeConfiguration<IncomeSourceEntity>
{
    public void Configure(EntityTypeBuilder<IncomeSourceEntity> builder)
    {
        builder.ToTable("IncomeSources");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerKey).HasMaxLength(450).IsRequired();
        builder.Property(e => e.SourceLabel).HasMaxLength(256).IsRequired();
        builder.Property(e => e.FixedAmount).HasPrecision(18, 2);
        builder.Property(e => e.EstimateAmount).HasPrecision(18, 2);
        builder.Property(e => e.SplitNeedsPercent).HasPrecision(18, 2);
        builder.Property(e => e.SplitWantsPercent).HasPrecision(18, 2);
        builder.Property(e => e.SplitSavingsDebtPercent).HasPrecision(18, 2);
    }
}

internal sealed class RecurringExpenseConfiguration : IEntityTypeConfiguration<RecurringExpenseEntity>
{
    public void Configure(EntityTypeBuilder<RecurringExpenseEntity> builder)
    {
        builder.ToTable("RecurringExpenses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerKey).HasMaxLength(450).IsRequired();
        builder.Property(e => e.VendorLabel).HasMaxLength(256).IsRequired();
        builder.Property(e => e.FixedAmount).HasPrecision(18, 2);
        builder.Property(e => e.StoredMonthlyEstimate).HasPrecision(18, 2);
        builder.Property(e => e.Tag).HasMaxLength(128);
        builder.Property(e => e.PaymentType).HasMaxLength(64).IsRequired();
        builder.HasOne<BudgetCategoryEntity>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AssignmentConfiguration : IEntityTypeConfiguration<AssignmentEntity>
{
    public void Configure(EntityTypeBuilder<AssignmentEntity> builder)
    {
        builder.ToTable("Assignments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerKey).HasMaxLength(450).IsRequired();
        builder.Property(e => e.Tag).HasMaxLength(128);
        builder.HasOne<TransactionEntity>()
            .WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<RecurringExpenseEntity>()
            .WithMany()
            .HasForeignKey(e => e.RecurringExpenseId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<IncomeSourceEntity>()
            .WithMany()
            .HasForeignKey(e => e.IncomeSourceId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<BudgetCategoryEntity>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.OwnerKey, e.TransactionId })
            .IsUnique()
            .HasFilter("[TransactionId] IS NOT NULL");
    }
}
