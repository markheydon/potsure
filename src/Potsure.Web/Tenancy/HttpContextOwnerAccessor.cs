using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Potsure.Data.Tenancy;

namespace Potsure.Web.Tenancy;

public sealed class HttpContextOwnerAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentOwnerAccessor
{
    public string OwnerKey
    {
        get
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
            string? id = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Hosted mode requires an authenticated user.");
            }

            return id;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}
