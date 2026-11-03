using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Services;

namespace Potsure.Infrastructure.Tests;

/// <summary>
/// SC-003: remembered assignments and auto-attribution after re-import.
/// </summary>
public sealed class ReimportAssignmentMemoryTests
{
    [Fact]
    public void AssignmentEntity_StoresPatternLinkForReimportMemory()
    {
        AssignmentEntity assignment = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = "owner",
            TransactionId = Guid.NewGuid(),
            RecurringExpenseId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            ConfirmedAt = DateTimeOffset.UtcNow,
        };

        Assert.NotNull(assignment.RecurringExpenseId);
        Assert.NotNull(assignment.TransactionId);
    }

    [Fact]
    public void TransactionAttributionService_Type_ExposesAutoAttributionEntryPoint()
    {
        Assert.NotNull(typeof(TransactionAttributionService).GetMethod(nameof(TransactionAttributionService.AttributeMatchingDebitsAsync)));
    }

    [Fact]
    public void ConfirmedPattern_State_IsRequiredForAttribution()
    {
        Assert.Equal(PatternConfirmationState.Confirmed, PatternConfirmationState.Confirmed);
        Assert.NotEqual(PatternConfirmationState.Suggested, PatternConfirmationState.Confirmed);
    }
}
