using Microsoft.Extensions.DependencyInjection;
using LiveEventService.Core.Events;
using LiveEventService.Infrastructure.Data;
using LiveEventService.Infrastructure.Users;
using LiveEventService.Infrastructure.Events;
using LiveEventService.Application.Common.Interfaces;
using IUserRepository = LiveEventService.Core.Users.User.IUserRepository;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using LiveEventService.Core.Common;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, bool isTesting = false)
    {
        // Configure Npgsql to handle DateTime properly
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);
        
        // Register DbContext, repositories, etc.
        if (isTesting == false)
        {
            services.AddDbContext<LiveEventDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        
        // Register generic repository for EventRegistration
        services.AddScoped<IRepository<EventRegistrationEntity>, RepositoryBase<EventRegistrationEntity>>();
        
        return services;
    }
}
