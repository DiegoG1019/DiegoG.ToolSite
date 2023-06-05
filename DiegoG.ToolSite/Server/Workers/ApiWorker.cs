using DiegoG.ToolSite.Shared.Services;

namespace DiegoG.ToolSite.Server.Workers;

public abstract class ApiServiceWorker : ApiWorker
{
    public sealed class ApiServiceWorkerServices : IServiceProvider, IDisposable
    {
        private IServiceProvider ServiceProvider => _scope.ServiceProvider;
        private IServiceScope _scope;

        public ApiServiceWorkerServices(IServiceScope scope)
        {
            _scope = scope;
        }

        public object? GetService(Type serviceType) => ServiceProvider.GetService(serviceType);

        public void Dispose() => _scope.Dispose();
    }

    private readonly IServiceProvider RootProvider;

    protected ApiServiceWorker(IServiceProvider rootProvider) : base()
    {
        RootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
    }

    protected ApiServiceWorkerServices GetNewScopedServices()
        => new(RootProvider.CreateScope());
}

public abstract class ApiWorker : BackgroundService
{
    protected ILogger Log { get; }
    private TimeSpan ErrorDelay;
    private readonly string WorkerName;

    protected ApiWorker()
    {
        WorkerName = GetType().Name;
        Log = CreateLogger();
        ErrorDelay = ServerProgram.Settings.WorkerDelayOnError;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        Log.Information("Starting Worker");
        while (stoppingToken.IsCancellationRequested is false)
        {
            try
            {
                await Work(stoppingToken);
                ErrorDelay = ServerProgram.Settings.WorkerDelayOnError;
            }
            catch (TaskCanceledException e)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                Log.Error(e, "An error ocurred while performing {WorkerName}. Sleeping for {delay} before continuing", WorkerName, ErrorDelay);
                await Task.Delay(ErrorDelay, stoppingToken);

                var maxDelay = ServerProgram.Settings.WorkerMaxDelayOnError;
                ErrorDelay *= 2;
                if (ErrorDelay >= maxDelay)
                    ErrorDelay = maxDelay;
            }
            catch (Exception e)
            {
                Log.Error(e, "An error ocurred while performing {WorkerName}. Sleeping for {delay} before continuing", WorkerName, ErrorDelay);
                await Task.Delay(ErrorDelay, stoppingToken);

                var maxDelay = ServerProgram.Settings.WorkerMaxDelayOnError;
                ErrorDelay *= 2;
                if (ErrorDelay >= maxDelay)
                    ErrorDelay = maxDelay;
            }
        }
    }

    protected virtual ILogger CreateLogger()
        => LogHelper.CreateLogger("Workers", WorkerName, null, new LogProperty("WorkerName", WorkerName));

    public abstract Task Work(CancellationToken stoppingToken);
}
