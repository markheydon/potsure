using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Identity;
using Potsure.Infrastructure.Persistence.Tenancy;

namespace Potsure.Infrastructure.Persistence;

public sealed class PotsureDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    private readonly ICurrentOwnerAccessor ownerAccessor;

    public PotsureDbContext(DbContextOptions<PotsureDbContext> options, ICurrentOwnerAccessor ownerAccessor)
        : base(options)
    {
        this.ownerAccessor = ownerAccessor;
    }

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<BudgetProfileEntity> BudgetProfiles => Set<BudgetProfileEntity>();

    public DbSet<BudgetPeriodEntity> BudgetPeriods => Set<BudgetPeriodEntity>();

    public DbSet<BudgetCategoryEntity> BudgetCategories => Set<BudgetCategoryEntity>();

    public DbSet<BankPotEntity> BankPots => Set<BankPotEntity>();

    public DbSet<MonzoAccountAssociationEntity> MonzoAccounts => Set<MonzoAccountAssociationEntity>();

    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();

    public DbSet<StatementUploadEntity> StatementUploads => Set<StatementUploadEntity>();

    public DbSet<IncomeSourceEntity> IncomeSources => Set<IncomeSourceEntity>();

    public DbSet<RecurringExpenseEntity> RecurringExpenses => Set<RecurringExpenseEntity>();

    public DbSet<AssignmentEntity> Assignments => Set<AssignmentEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(PotsureDbContext).Assembly);

        builder.Entity<BudgetProfileEntity>()
            .HasQueryFilter(e => e.OwnerKey == ownerAccessor.OwnerKey);
        builder.Entity<BudgetPeriodEntity>()
            .HasQueryFilter(e => e.OwnerKey == ownerAccessor.OwnerKey);
        builder.Entity<BudgetCategoryEntity>()
            .HasQueryFilter(e => e.OwnerKey == ownerAccessor.OwnerKey);
        builder.Entity<BankPotEntity>()
            .HasQueryFilter(e => e.OwnerKey == ownerAccessor.OwnerKey);
        builder.Entity<MonzoAccountAssociationEntity>()
            .HasQueryFilter(e => e.OwnerKey == ownerAccessor.OwnerKey);
        builder.Entity<TransactionEntity>()
            .HasQueryFilter(e => e.OwnerKey == ownerAccessor.OwnerKey);
        builder.Entity<StatementUploadEntity>()
            .HasQueryFilter(e => e.OwnerKey == ownerAccessor.OwnerKey);
        builder.Entity<IncomeSourceEntity>()
            .HasQueryFilter(e => e.OwnerKey == ownerAccessor.OwnerKey);
        builder.Entity<RecurringExpenseEntity>()
            .HasQueryFilter(e => e.OwnerKey == ownerAccessor.OwnerKey);
        builder.Entity<AssignmentEntity>()
            .HasQueryFilter(e => e.OwnerKey == ownerAccessor.OwnerKey);
    }
}
