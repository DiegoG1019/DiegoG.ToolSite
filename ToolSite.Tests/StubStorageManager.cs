using DiegoG.ToolSite.Shared.Services;

namespace ToolSite.Tests;

public class StubStorageManager : IStorageManager
{
    public ValueTask<T> Get<T>(string key)
        => ValueTask.FromResult<T>(default!);

    public ValueTask<(bool Success, T? Result)> TryGet<T>(string key)
        => ValueTask.FromResult<(bool Success, T? Result)>((false, default));

    public ValueTask<string?> Get(string key)
        => ValueTask.FromResult("")!;

    public ValueTask Set<T>(string key, T value)
        => ValueTask.CompletedTask;

    public ValueTask Set(string key, string value)
        => ValueTask.CompletedTask;

    public ValueTask Clear()
        => ValueTask.CompletedTask;

    public ValueTask Remove(string key)
        => ValueTask.CompletedTask;
}