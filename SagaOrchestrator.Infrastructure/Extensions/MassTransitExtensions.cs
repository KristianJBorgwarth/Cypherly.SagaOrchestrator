using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SagaOrchestrator.Application.Sagas;
using SagaOrchestrator.Infrastructure.Persistence.Context;
using SagaOrchestrator.Infrastructure.Settings;
using SagaOrchestrator.Infrastructure.StateMachines;

namespace SagaOrchestrator.Infrastructure.Extensions;

internal static class MassTransitExtensions
{
    internal static void ConfigureMasstransit(this IServiceCollection services, Assembly consumerAssembly)
    {
        services.AddMassTransit(x =>
        {

            x.AddConsumers(consumerAssembly);

            x.AddSagaStateMachine<UserDeleteSagaMachine, UserDeleteSagaState>().EntityFrameworkRepository(r =>
            {
                r.ExistingDbContext<OrchestratorDbContext>();
                r.UsePostgres();
            });
            
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqSettings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;

                cfg.Host(rabbitMqSettings.Host, "/", h =>
                {
                    h.Username(rabbitMqSettings.Username ??
                               throw new InvalidOperationException("Cannot initialize RabbitMQ without a username"));
                    h.Password(rabbitMqSettings.Password ??
                               throw new InvalidOperationException("Cannot initialize RabbitMQ without a password"));
                });


                cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(5)));

                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                    cb.TripThreshold = 15;
                    cb.ActiveThreshold = 10;
                    cb.ResetInterval = TimeSpan.FromMinutes(5);
                });

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}