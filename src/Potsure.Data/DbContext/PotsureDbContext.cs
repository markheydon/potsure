using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Potsure.Data.Entities;
using Potsure.Data.Identity;
using Potsure.Data.Tenancy;

namespace Potsure.Data;

public sealed class PotsureDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentOwnerAccessor ownerAccessor;

    public PotsureDbContext(DbContextOptions<PotsureDbContext> options, ICurrentOwnerAccessor ownerAccessor)
        : base(options)
    {
        this.ownerAccessor = ownerAccessor;
    }

    public DbSet<BudgetProfileEntity> BudgetProfiles => Set<BudgetProfileEntity>();

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
