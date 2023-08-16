using System.Collections.Concurrent;

namespace DiegoG.ToolSite.Client.Services;

[RegisterClientService(ServiceLifetime.Scoped)]
public class ActionScheduler : IDisposable
{
    private Task? _task;
    private readonly TimeSpan _interval;
    private readonly ConcurrentQueue<Func<ValueTask>> Pending = new();
    private readonly int maxPerCycle;

    public ActionScheduler(TimeSpan? interval = null, int maxPerCycle = 0)
    {
        if (interval is TimeSpan t && t.TotalMilliseconds < 50)
            throw new ArgumentException("Interval between checks cannot be less than 50 milliseconds", nameof(interval));
        _interval = interval ?? TimeSpan.FromMilliseconds(50);
    }

    public void Schedule(Action task)
        => Pending.Enqueue(() => { task(); return ValueTask.CompletedTask; });

    public void Schedule(Func<ValueTask> task)
        => Pending.Enqueue(task);

    public void Launch() => _task ??= Task.Run(async () =>
    {
        // Given that this method returns void, it's impossible to await this task. Therefore any delays will simply yield the context, hence why it works.
        while(_task is not null)
        {
            await Task.Delay(_interval);

            int c = 0;
            while ((maxPerCycle > 0 || c++ < maxPerCycle) && Pending.TryDequeue(out var t))
                await t();
        }
    });

    public void Dispose()
    {
        _task?.ConfigureAwait(false).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
