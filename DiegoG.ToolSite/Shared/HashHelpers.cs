using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.ToolSite.Shared;

public static class HashHelpers
{
    public const int SHA512HexStringLength = 128;
    public const int SHA256HexStringLength = 64;

    public static string GetSHA512(ReadOnlySpan<char> data)
    {
        Span<byte> pswd = stackalloc byte[sizeof(char) * data.Length];
        Encoding.UTF8.GetBytes(data, pswd);

        Span<byte> hash = stackalloc byte[SHA512.HashSizeInBytes];
        SHA512.TryHashData(pswd, hash, out _);

        return Convert.ToHexString(hash);
    }

    public static string GetSHA512(ReadOnlySpan<byte> data)
    {
        Span<byte> hash = stackalloc byte[SHA512.HashSizeInBytes];
        SHA512.TryHashData(data, hash, out _);
        return Convert.ToHexString(hash);
    }

    public static string GetSHA256(ReadOnlySpan<byte> data)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.TryHashData(data, hash, out _);

        return Convert.ToHexString(hash);
    }

    public static string GetSHA256(ReadOnlySpan<char> data)
    {
        Span<byte> pswd = stackalloc byte[sizeof(char) * data.Length];
        Encoding.UTF8.GetBytes(data, pswd);

        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.TryHashData(pswd, hash, out _);

        return Convert.ToHexString(hash);
    }
}
