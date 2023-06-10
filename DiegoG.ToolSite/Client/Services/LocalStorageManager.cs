using System.Runtime.CompilerServices;
using Microsoft.JSInterop;

namespace DiegoG.ToolSite.Client.Services;

[RegisterClientService(ServiceLifetime.Scoped, typeof(IStorageManager))]
public class LocalStorageManager : IStorageManager
{
    private readonly IJSRuntime Js;
    private IJSObjectReference? _module;
    private async ValueTask<IJSObjectReference> GetStorage()
        => _module ??= await Js.InvokeAsync<IJSObjectReference>("import", "/js/LocalStorage.js;");

    public LocalStorageManager(IJSRuntime js)
    {
        Js = js ?? throw new ArgumentNullException(nameof(js));
    }

    [ThreadStatic]
    private static object[]? ParamBuffer;

    public async ValueTask<T> Get<T>(string key)
        => JsonSerializer.Deserialize<T>(await Get(key) ?? throw new InvalidOperationException($"There is no value associated to key '{key}' in local storage"))!;

    public async ValueTask<(bool Success, T? Result)> TryGet<T>(string key)
    {
        var x = await Get(key);
        return x is not null ? ((bool Success, T? Result))(true, JsonSerializer.Deserialize<T>(x)!) : ((bool Success, T? Result))(false, default);
    }

    public async ValueTask<string?> Get(string key)
    {
        ParamBuffer ??= new object[2];
        ParamBuffer[0] = key;
        ParamBuffer[1] = Type.Missing;
        return await (await GetStorage()).InvokeAsync<string?>("get");
    }

    public ValueTask Set<T>(string key, T value)
        => Set(key, JsonSerializer.Serialize(value));

    public async ValueTask Set(string key, string value)
    {
        ParamBuffer ??= new object[2];
        ParamBuffer[0] = key;
        ParamBuffer[1] = value;
        await (await GetStorage()).InvokeVoidAsync("set", ParamBuffer);
    }

    public async ValueTask Clear()
        => await (await GetStorage()).InvokeVoidAsync("clear");

    public async ValueTask Remove(string key)
    {
        ParamBuffer ??= new object[2];
        ParamBuffer[0] = key;
        ParamBuffer[1] = Type.Missing;
        await (await GetStorage()).InvokeVoidAsync("remove", ParamBuffer);
    }
}
