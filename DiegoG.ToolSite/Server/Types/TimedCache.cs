using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DiegoG.ToolSite.Shared;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;

namespace DiegoG.ToolSite.Server.Types;

public sealed class TimedCache<TKey, TValue> where TKey : notnull
{
    private static readonly LinkedList<WeakReference<TimedCache<TKey, TValue>>> CleanupList = new();

    static TimedCache()
    {
        BackgroundTaskStore.Add(() =>
        {
            var current = CleanupList.First;
            while (current is not null)
            {
                if (current.Value.TryGetTarget(out var cache))
                {
                    cache.Sweep();
                    current = current.Next;
                    continue;
                }

                var p = current;
                current = current.Next;
                CleanupList.Remove(p);
            }
            return Task.Delay(TimeSpan.FromSeconds(30));
        }, true);
    }

    private record Entry(TValue Value)
    {
        public DateTime Updated { get; set; } = DateTime.Now;
    }

    private readonly ConcurrentDictionary<TKey, Entry> _cache = new();
    private readonly Func<TKey, TValue, TimeSpan> TimeoutCheck;

    public TimedCache(Func<TKey, TValue, TimeSpan> timeoutCheck)
    {
        TimeoutCheck = timeoutCheck ?? throw new ArgumentNullException(nameof(timeoutCheck));
        lock (CleanupList)
            CleanupList.AddLast(new WeakReference<TimedCache<TKey, TValue>>(this));
    }
    
    public void Clear()
        => _cache.Clear();

    public void Sweep()
    {
        foreach (var (key, entry) in _cache)
            if (DateTime.Now - entry.Updated > TimeoutCheck(key, entry.Value))
                _cache.TryRemove(key, out _);
    }

    public bool TryRemove(TKey key, [MaybeNull] out TValue value)
    {
        var r = _cache.TryRemove(key, out var entry);
        value = entry is null ? default : entry.Value;
        return r;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] [NotNullWhen(true)] out TValue value)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            if (DateTime.Now - cached.Updated > TimeoutCheck(key, cached.Value) is false)
            {
                cached.Updated = DateTime.Now;
                value = cached.Value!;
                return true;
            }
            else
                _cache.TryRemove(key, out _);
        }

        value = default;
        return false;
    }

    public bool TryPeekValue(TKey key, [MaybeNullWhen(false)][NotNullWhen(true)] out TValue value)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            if (DateTime.Now - cached.Updated > TimeoutCheck(key, cached.Value) is false)
            {
                value = cached.Value!;
                return true;
            }
            else
                _cache.TryRemove(key, out _);
        }

        value = default;
        return false;
    }

    public TValue? FetchValue(TKey key)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            cached.Updated = DateTime.Now;
            return cached.Value;
        }
        else 
            return default;
    }

    public TValue? PeekValue(TKey key)
        => _cache.TryGetValue(key, out var cached) ? cached.Value : default;

    public bool TryAdd(TKey key, TValue value)
        => _cache.TryAdd(key, new Entry(value));

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        TValue result;
        if (_cache.TryGetValue(key, out var cached) is false || DateTime.Now - cached.Updated > TimeoutCheck(key, cached.Value))
            _cache[key] = new(result = valueFactory(key));
        else
        {
            cached.Updated = DateTime.Now;
            result = cached.Value;
        }
        return result;
    }

    public TValue PeekOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        TValue result;
        if (_cache.TryGetValue(key, out var cached) is false || DateTime.Now - cached.Updated > TimeoutCheck(key, cached.Value))
            _cache[key] = new(result = valueFactory(key));
        else
            result = cached.Value;
        return result;
    }

    public async ValueTask<TValue> GetOrAddAsync(TKey key, Func<TKey, ValueTask<TValue>> valueFactory)
    {
        TValue result;
        if (_cache.TryGetValue(key, out var cached) is false || DateTime.Now - cached.Updated > TimeoutCheck(key, cached.Value))
            _cache[key] = new(result = await valueFactory(key));
        else
        {
            cached.Updated = DateTime.Now;
            result = cached.Value;
        }
        return result;
    }

    public async ValueTask<TValue> PeekOrAddAsync(TKey key, Func<TKey, ValueTask<TValue>> valueFactory)
    {
        TValue result;
        if (_cache.TryGetValue(key, out var cached) is false || DateTime.Now - cached.Updated > TimeoutCheck(key, cached.Value))
            _cache[key] = new(result = await valueFactory(key));
        else
            result = cached.Value;
        return result;
    }
}
