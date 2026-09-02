using Consul;

namespace AccountService.Infrastructure;

/// <summary>
/// Registers this service with Consul on startup and deregisters it on shutdown.
/// Registration is retried in the background so that a slow or briefly-unavailable
/// Consul never stops this service from starting.
/// </summary>
public static class ConsulRegistration
{
    public static IServiceCollection AddConsulRegistration(this IServiceCollection services, IConfiguration config)
    {
        var consulAddress = config["Consul:Address"] ?? "http://consul:8500";
        services.AddSingleton<IConsulClient>(new ConsulClient(c => c.Address = new Uri(consulAddress)));
        services.AddHostedService<ConsulHostedService>();
        return services;
    }
}

public class ConsulHostedService : IHostedService
{
    private readonly IConsulClient _consul;
    private readonly IConfiguration _config;
    private readonly ILogger<ConsulHostedService> _logger;
    private string _serviceId = "";

    public ConsulHostedService(IConsulClient consul, IConfiguration config, ILogger<ConsulHostedService> logger)
    {
        _consul = consul;
        _config = config;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var name = _config["Consul:ServiceName"] ?? "unknown-service";
        var host = _config["Consul:ServiceHost"] ?? name;
        var port = int.Parse(_config["Consul:ServicePort"] ?? "8080");
        _serviceId = $"{name}-{Guid.NewGuid():N}";

        var registration = new AgentServiceRegistration
        {
            ID = _serviceId,
            Name = name,
            Address = host,
            Port = port,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{host}:{port}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30)
            }
        };

        // Register in the background with retries — don't block or crash startup if Consul is not ready yet.
        _ = Task.Run(async () =>
        {
            for (var attempt = 1; attempt <= 10; attempt++)
            {
                try
                {
                    await _consul.Agent.ServiceDeregister(_serviceId);
                    await _consul.Agent.ServiceRegister(registration);
                    _logger.LogInformation("Registered {ServiceName} ({ServiceId}) with Consul", name, _serviceId);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Consul registration attempt {Attempt} failed: {Message}", attempt, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
        });

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _consul.Agent.ServiceDeregister(_serviceId);
            _logger.LogInformation("Deregistered {ServiceId} from Consul", _serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Consul deregistration failed: {Message}", ex.Message);
        }
    }
}
