using MaintenanceService.Services;

namespace MaintenanceService.Services;

public class MaintenanceBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MaintenanceBackgroundService> _logger;
    private readonly TimeSpan _autoAssignInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _queueRebalanceInterval = TimeSpan.FromMinutes(15);

    public MaintenanceBackgroundService(IServiceProvider serviceProvider, ILogger<MaintenanceBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Maintenance Background Service started");

        var autoAssignTimer = new PeriodicTimer(_autoAssignInterval);
        var rebalanceTimer = new PeriodicTimer(_queueRebalanceInterval);
        
        var autoAssignTask = RunAutoAssignmentLoop(autoAssignTimer, stoppingToken);
        var rebalanceTask = RunQueueRebalanceLoop(rebalanceTimer, stoppingToken);

        await Task.WhenAny(autoAssignTask, rebalanceTask);
        
        _logger.LogInformation("Maintenance Background Service stopped");
    }

    private async Task RunAutoAssignmentLoop(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PerformAutoAssignment();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Auto-assignment loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in auto-assignment loop");
        }
    }

    private async Task RunQueueRebalanceLoop(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PerformQueueRebalance();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Queue rebalance loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in queue rebalance loop");
        }
    }

    private async Task PerformAutoAssignment()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var maintenanceService = scope.ServiceProvider.GetRequiredService<IMaintenanceService>();
            
            await maintenanceService.AutoAssignRequestsAsync();
            
            _logger.LogDebug("Auto-assignment completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled auto-assignment");
        }
    }

    private async Task PerformQueueRebalance()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var priorityQueueService = scope.ServiceProvider.GetRequiredService<IPriorityQueueService>();
            
            await priorityQueueService.RebalanceQueueAsync();
            
            _logger.LogDebug("Queue rebalance completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled queue rebalance");
        }
    }
}