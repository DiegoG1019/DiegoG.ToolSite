using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DiegoG.ToolSite.Server.Services;

namespace DiegoG.ToolSite.Server;

public static class Helper
{
    private readonly static string AppDataPath
        = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ToolSite-Server");

    public static void CreateAppDataDirectory()
        => Directory.CreateDirectory(AppDataPath);

    public static string? GetFormattedStringValue(this IConfiguration section, string key)
        => section.GetValue<string>(key)?.Replace("{AppData}", AppDataPath);

    public static string? GetFormattedConnectionString(this IConfiguration section, string key)
        => section.GetConnectionString(key)?.Replace("{AppData}", AppDataPath);

    public static void AddError(this ref ErrorList errors, string error)
        => (errors.Errors ??= new()).Add(error);

    public static string GetHash512(ReadOnlySpan<char> data)
    {
        Span<byte> pswd = stackalloc byte[sizeof(char) * data.Length];
        Encoding.UTF8.GetBytes(data, pswd);

        Span<byte> hash = stackalloc byte[SHA512.HashSizeInBytes];
        SHA512.TryHashData(pswd, hash, out _);

        return Encoding.UTF8.GetString(hash);
    }

    public static string GetHash512(ReadOnlySpan<byte> data)
    {
        Span<byte> hash = stackalloc byte[SHA512.HashSizeInBytes];
        SHA512.TryHashData(data, hash, out _);
        return Encoding.UTF8.GetString(hash);
    }

    public static string GetHash256(ReadOnlySpan<byte> data)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.TryHashData(data, hash, out _);

        return Encoding.UTF8.GetString(hash);
    }

    public static string GetHash256(ReadOnlySpan<char> data)
    {
        Span<byte> pswd = stackalloc byte[sizeof(char) * data.Length];
        Encoding.UTF8.GetBytes(data, pswd);

        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.TryHashData(pswd, hash, out _);

        return Encoding.UTF8.GetString(hash);
    }

    public static string UpperFirstLetter(this string word)
        => char.IsUpper(word[0]) ? word : $"{char.ToUpper(word[0])}{word[1..]}";

    public static ValueConverter<TimeSpan, long> TimeSpanToLongConverter { get; } = new TimeSpanValueConverter();

    private class TimeSpanValueConverter : ValueConverter<TimeSpan, long>
    {
        public TimeSpanValueConverter()
            : base(
                  x => (long)x.TotalMilliseconds,
                  x => TimeSpan.FromMilliseconds(x)
            )
        {
        }
    }
}
