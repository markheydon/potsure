using Microsoft.AspNetCore.Identity;
using Potsure.Data.Documents;
using Potsure.Data.Identity;
using Potsure.Infrastructure.Deletion;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Identity;

public sealed class BlobIdentityUserStore(
    IIdentityDocumentStore documentStore,
    IUserDocumentStore userDocumentStore)
    : IUserStore<ApplicationUser>,
        IUserPasswordStore<ApplicationUser>,
        IUserEmailStore<ApplicationUser>,
        IUserSecurityStampStore<ApplicationUser>,
        IUserLockoutStore<ApplicationUser>
{
    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        IdentityUserDocument document = ToDocument(user);
        try
        {
            await documentStore.SaveUserAsync(document, ifMatchEtag: null, cancellationToken);
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                string hash = IdentityEmailHasher.HashEmailForIndex(user.Email);
                await documentStore.SaveEmailIndexAsync(
                    hash,
                    new EmailIndexDocument { UserId = user.Id },
                    ifMatchEtag: null,
                    cancellationToken);
            }
        }
        catch (DocumentStoreConcurrencyException)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateUserName",
                Description = "User creation failed because the user or email index already exists.",
            });
        }

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        IdentityUserLoadResult loaded = await documentStore.LoadUserWithETagAsync(user.Id, cancellationToken);
        if (loaded.User is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "User no longer exists.",
            });
        }

        if (!string.Equals(loaded.User.ConcurrencyStamp, user.ConcurrencyStamp, StringComparison.Ordinal))
        {
            return IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure());
        }

        IdentityUserDocument document = ToDocument(user);
        try
        {
            await documentStore.SaveUserAsync(document, loaded.ETag, cancellationToken);
        }
        catch (DocumentStoreConcurrencyException)
        {
            return IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure());
        }

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        await HostedUserBlobDeletion.DeleteAsync(
            new ApplicationUserIdentityDeletionRequest(user.Id, user.Email),
            userDocumentStore,
            documentStore,
            cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        IdentityUserDocument? document = await documentStore.LoadUserAsync(userId, cancellationToken);
        return document is null ? null : ToUser(document);
    }

    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        string hash = IdentityEmailHasher.HashEmailForIndex(normalizedUserName);
        EmailIndexDocument? index = await documentStore.LoadEmailIndexAsync(hash, cancellationToken);
        if (index is null)
        {
            return null;
        }

        return await FindByIdAsync(index.UserId, cancellationToken);
    }

    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        string hash = IdentityEmailHasher.HashEmailForIndex(normalizedEmail);
        EmailIndexDocument? index = await documentStore.LoadEmailIndexAsync(hash, cancellationToken);
        if (index is null)
        {
            return null;
        }

        return await FindByIdAsync(index.UserId, cancellationToken);
    }

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.SecurityStamp);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnabled);

    public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    private static IdentityUserDocument ToDocument(ApplicationUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        NormalizedUserName = user.NormalizedUserName,
        Email = user.Email,
        NormalizedEmail = user.NormalizedEmail,
        EmailConfirmed = user.EmailConfirmed,
        PasswordHash = user.PasswordHash,
        SecurityStamp = user.SecurityStamp,
        ConcurrencyStamp = user.ConcurrencyStamp ?? Guid.NewGuid().ToString(),
        PhoneNumber = user.PhoneNumber,
        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
        TwoFactorEnabled = user.TwoFactorEnabled,
        LockoutEnd = user.LockoutEnd,
        LockoutEnabled = user.LockoutEnabled,
        AccessFailedCount = user.AccessFailedCount,
    };

    private static ApplicationUser ToUser(IdentityUserDocument document) => new()
    {
        Id = document.Id,
        UserName = document.UserName,
        NormalizedUserName = document.NormalizedUserName,
        Email = document.Email,
        NormalizedEmail = document.NormalizedEmail,
        EmailConfirmed = document.EmailConfirmed,
        PasswordHash = document.PasswordHash,
        SecurityStamp = document.SecurityStamp,
        ConcurrencyStamp = document.ConcurrencyStamp,
        PhoneNumber = document.PhoneNumber,
        PhoneNumberConfirmed = document.PhoneNumberConfirmed,
        TwoFactorEnabled = document.TwoFactorEnabled,
        LockoutEnd = document.LockoutEnd,
        LockoutEnabled = document.LockoutEnabled,
        AccessFailedCount = document.AccessFailedCount,
    };
}
