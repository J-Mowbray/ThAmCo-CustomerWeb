using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CustomerWeb.Services;

namespace ThAmCo.Web.BackgroundServices;

public class StockUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockUpdateService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(5);

    public StockUpdateService(
        IServiceProvider serviceProvider,
        ILogger<StockUpdateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock update service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateStockInformationAsync();

                _logger.LogInformation("Next stock update scheduled in {Minutes} minutes", _updateInterval.TotalMinutes);
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in stock update service");

                // Wait a bit before retrying after an error
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Stock update service stopping");
    }

    private async Task UpdateStockInformationAsync()
    {
        _logger.LogInformation("Updating stock information");

        // Create a scope to resolve scoped services
        using var scope = _serviceProvider.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductApiService>();

        await productService.RefreshStockInformationAsync();

        _logger.LogInformation("Stock information successfully updated");
    }
}