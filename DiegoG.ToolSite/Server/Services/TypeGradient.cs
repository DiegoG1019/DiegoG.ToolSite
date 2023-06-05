using System.Drawing.Drawing2D;

namespace DiegoG.ToolSite.Server.Services;

public static class TypeGradient
{
    private static readonly Dictionary<string, (string First, string Second)> Colors = new()
    {
        {
            "grass",
            ("#78C850", "#4E8234")
        },
        {
            "normal",
            ("#A8A870", "#6D6D4E")
        },
        {
            "fire",
            ("#F08030", "#9C531F")
        },
        {
            "fighting",
            ("#C03020", "#7D1F1A")
        },
        {
            "water",
            ("#6890F0", "#445E9C")
        },
        {
            "flying",
            ("#A890F0", "#6D5E9C")
        },
        {
            "poison",
            ("#A040A0", "#682A68")
        },
        {
            "electric",
            ("#F8D030", "#A1871F")
        },
        {
            "ground",
            ("#E0C060", "#927D44")
        },
        {
            "psychic",
            ("#F85880", "#A13959")
        },
        {
            "rock",
            ("#B8A030", "#786824")
        },
        {
            "ice",
            ("#98D8D0", "#638D8D")
        },
        {
            "bug",
            ("#A8B820", "#6D7815")
        },
        {
            "dragon",
            ("#7038F0", "#4924A1")
        },
        {
            "ghost",
            ("#705890", "#493963")
        },
        {
            "dark",
            ("#705840", "#49392F")
        },
        {
            "steel",
            ("#B8B8D0", "#787887")
        },
        {
            "fairy",
            ("#EE99A0", "#9B6470")
        },
    };

    public static string GetAngledLinearGradient(int angleDegrees, string primary, string? secondary = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(primary);

        if (secondary is null)
        {
            var (c1, c2) = Colors[primary];
            return $"linear-gradient({angleDegrees}deg, {c1}, {c2})";
        }

        ArgumentException.ThrowIfNullOrEmpty(secondary); // it's not null, but it still can't 

        var (cp1, cp2) = Colors[primary];
        var (cs1, cs2) = Colors[secondary];
        return $"linear-gradient({angleDegrees}deg, {cp1}, {cp2}, {cs1}, {cs2})";
    }
}
