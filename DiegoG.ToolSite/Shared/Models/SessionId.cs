using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace DiegoG.ToolSite.Shared.Models;

public readonly struct SessionId : IEquatable<SessionId>, IParsable<SessionId>
{
    private readonly long A;
    private readonly long B;
    private readonly long C;
    private readonly long D;

    public SessionId(long a, long b, long c, long d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }

    public unsafe static SessionId NewId()
    {
        SessionId id = default;
        RandomNumberGenerator.Fill(new Span<byte>(&id, sizeof(SessionId)));
        return id;
    }

    private unsafe static bool Equals_Internal(SessionId left, SessionId right)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            return Vector256.LoadUnsafe(ref Unsafe.As<SessionId, byte>(ref Unsafe.AsRef(in left))) == Vector256.LoadUnsafe(ref Unsafe.As<SessionId, byte>(ref Unsafe.AsRef(in right)));
        }

        ref long rA = ref Unsafe.AsRef(in left.A);
        ref long rB = ref Unsafe.AsRef(in right.A);

        // Compare each element

        return rA == rB
            && Unsafe.Add(ref rA, 1) == Unsafe.Add(ref rB, 1)
            && Unsafe.Add(ref rA, 2) == Unsafe.Add(ref rB, 2)
            && Unsafe.Add(ref rA, 3) == Unsafe.Add(ref rB, 3);
    }

    public unsafe override string ToString()
        => Convert.ToBase64String(new Span<byte>(Unsafe.AsPointer(ref Unsafe.AsRef(in this)), Unsafe.SizeOf<SessionId>()));

    public unsafe static SessionId Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(s);

        SessionId id = default;
        var span = new Span<byte>(&id, sizeof(SessionId));

        return Convert.TryFromBase64String(s, span, out _) is false
            ? throw new FormatException($"The input string '{s}' was not in a correct format.")
            : id;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out SessionId result)
        => TryParse(s, null, out result);

    public unsafe static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out SessionId result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = default;
            return false;
        }

        SessionId id = default;
        var span = new Span<byte>(&id, sizeof(SessionId));

        if (Convert.TryFromBase64String(s, span, out _) is false)
        {
            result = default;
            return false;
        }

        result = id;
        return true;
    }

    public bool Equals(SessionId other)
        => Equals_Internal(this, other);

    public override bool Equals(object? obj)
        => obj is SessionId id && Equals_Internal(this, id);

    public static bool operator ==(SessionId left, SessionId right)
        => Equals_Internal(left, right);

    public static bool operator !=(SessionId left, SessionId right)
        => !Equals_Internal(left, right);

    public override int GetHashCode()
        => HashCode.Combine(A, B, C, D);
}
