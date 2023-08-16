using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DiegoG.ToolSite.Server.Services;

namespace DiegoG.ToolSite.Server;

public static class Helper
{
    public static string AppDataPath { get; } 
        = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ToolSite-Server");

    public static void CreateAppDataDirectory()
        => Directory.CreateDirectory(AppDataPath);

    public static string? GetFormattedStringValue(this IConfiguration section, string key)
        => section.GetValue<string>(key)?.Replace("{AppData}", AppDataPath);

    public static string? GetFormattedConnectionString(this IConfiguration section, string key)
        => section.GetConnectionString(key)?.Replace("{AppData}", AppDataPath);

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
