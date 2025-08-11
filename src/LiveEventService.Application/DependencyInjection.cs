using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using LiveEventService.Application.Common.Behaviors;
using MediatR;

namespace LiveEventService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR, AutoMapper, validators, etc.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddAutoMapper(cfg => {
            cfg.AddMaps(typeof(DependencyInjection).Assembly);
        });
        
        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        // Register validation behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        return services;
    }
} 