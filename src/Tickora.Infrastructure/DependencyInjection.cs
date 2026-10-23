using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickora.Application;
using Tickora.Infrastructure.Persistence;

namespace Tickora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTickoraInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<ApplicationDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<ITicketUnitOfWorkFactory, TicketUnitOfWorkFactory>();
        services.AddScoped<IHelpdeskQueries, HelpdeskQueries>();
        services.AddScoped<TicketService>();
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}
public sealed class SystemClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
