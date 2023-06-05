using Microsoft.Extensions.Hosting;
using DiegoG.ToolSite.Shared;

namespace DiegoG.ToolSite.Client.Workers;

public class BackgroundTaskSweeper : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            try
            {
                await Task.Delay(5000, stoppingToken);
                await BackgroundTaskStore.Sweep();
            }
            catch(TaskCanceledException) { }
        }
    }
}
