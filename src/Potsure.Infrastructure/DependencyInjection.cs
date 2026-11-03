using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPotsureInfrastructureServices(this IServiceCollection services)
    {
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
