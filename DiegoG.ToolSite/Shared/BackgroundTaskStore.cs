using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Serilog;

namespace DiegoG.ToolSite.Shared;
public static class BackgroundTaskStore
{
    private readonly record struct TaskCapsule(Task Task, Func<Task, ValueTask>? OnFailure);
    private readonly static AsyncLock sync = new();

    private static Queue<TaskCapsule> activeQueue = new();
    private static Queue<TaskCapsule> standbyQueue = new();

    private static bool active = true;

    static BackgroundTaskStore()
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) => active = false;
    }

    /// <summary>
    /// Adds a new background task to the store
    /// </summary>
    /// <param name="task">The task to add</param>
    /// <param name="onCompletion">An action to execute when the task completes, whether due to an error or not.</param>
    public static bool Add(Task task, Func<Task, ValueTask>? onCompletion = null)
    {
        if (active is false) return false;
        using (sync.Lock())
            activeQueue.Enqueue(new TaskCapsule(task, onCompletion));
        return true;
    }

    /// <summary>
    /// Adds a new background task to the store
    /// </summary>
    /// <param name="task">The task to add</param>
    /// <param name="onCompletion">An action to execute when the task completes, whether due to an error or not.</param>
    public static bool Add(Func<Task> task, bool reschedule, Func<Task, ValueTask>? onCompletion = null)
    {
        if (active is false) return false;
        if (reschedule)
            if (onCompletion is not null)
                using (sync.Lock())
                    activeQueue.Enqueue(new TaskCapsule(task(), async t =>
                    {
                        await onCompletion.Invoke(t);
                        Add(task, reschedule, onCompletion);
                    }));
            else
                using (sync.Lock())
                    activeQueue.Enqueue(new TaskCapsule(task(), async t =>
                    {
                        Add(task, reschedule, onCompletion);
                        await t;
                    }));
        else
            using (sync.Lock())
                activeQueue.Enqueue(new TaskCapsule(task(), onCompletion));

        return true;
    }

    /// <summary>
    /// Performs a single sweep on the store, searching for completed tasks to await
    /// </summary>
    public static async Task Sweep()
    {
        List<Exception> exceptions = new();
        List<Task> tasks = new();
        using (await sync.LockAsync())
        {
            while (activeQueue.TryDequeue(out var tc))
                tasks.Add(AwaitTask(tc, exceptions));

            (standbyQueue, activeQueue) = (activeQueue, standbyQueue);
        }

        await Task.WhenAll(tasks);

        if (exceptions?.Count is > 0)
            throw new AggregateException(exceptions);
    }

    private static async Task AwaitTask(TaskCapsule tc, List<Exception> exceptions)
    {
        await Task.Yield();
        var (task, onFailure) = tc;
        if (task.IsCompleted)
            try
            {
                if (onFailure is null)
                    await task;
                else
                {
                    await task;
                }
            }
            catch (Exception e)
            {
                lock (exceptions)
                    exceptions.Add(e);

                if (onFailure is not null)
                    await onFailure.Invoke(task);
            }
        else
            using (await sync.LockAsync())
                standbyQueue.Enqueue(tc);
    }
}
