using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.ToolSite.Shared.Services;
public interface IStorageManager
{
    public ValueTask<T> Get<T>(string key);
    public ValueTask<(bool Success, T? Result)> TryGet<T>(string key);
    public ValueTask<string?> Get(string key);
    public ValueTask Set<T>(string key, T value);
    public ValueTask Set(string key, string value);
    public ValueTask Clear();
    public ValueTask Remove(string key);
}
