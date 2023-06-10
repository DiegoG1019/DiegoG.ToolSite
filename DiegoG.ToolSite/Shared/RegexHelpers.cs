using System.Text.RegularExpressions;

namespace DiegoG.ToolSite.Shared;

public static partial class RegexHelpers
{
    [Flags]
    public enum HexStringVerificationOptions
    {
        Uppercase = 1,
        Lowercase = 2,
        AllowTrailing0x = 4,

        CaseInsensitive = Uppercase | Lowercase
    }

    [GeneratedRegex(@"^[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*@[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[A-z_-]+[0-9]*[A-z_-]*$")]
    private static partial Regex CssClassRegex();

    [GeneratedRegex(@"^[A-z0-9_]+$")]
    private static partial Regex AlphaNumericRegex();

    [GeneratedRegex(@"^[\dA-Fa-f]+$")]
    private static partial Regex VerifyHexStringCaseInsensitiveRegex();

    [GeneratedRegex(@"^(0[xX])?[\dA-Fa-f]+$")]
    private static partial Regex VerifyHexStringCaseInsensitiveWith0XRegex();

    [GeneratedRegex(@"^[\dA-F]+$")]
    private static partial Regex VerifyHexStringUppercaseRegex();

    [GeneratedRegex(@"^(0[xX])?[\dA-F]+$")]
    private static partial Regex VerifyHexStringUppercaseWith0XRegex();

    [GeneratedRegex(@"^[\da-f]+$")]
    private static partial Regex VerifyHexStringLowercaseRegex();

    [GeneratedRegex(@"^(0[xX])?[\da-f]+$")]
    private static partial Regex VerifyHexStringLowercaseWith0XRegex();

    public static Regex VerifyEmailRegex()
        => EmailRegex();

    public static Regex VerifyValidCssClassRegex()
        => CssClassRegex();

    public static Regex VerifyAlphaNumericRegex()
        => AlphaNumericRegex();

    public static Regex VerifyHexStringRegex(HexStringVerificationOptions options = HexStringVerificationOptions.CaseInsensitive)
        => options switch
        {
            HexStringVerificationOptions.CaseInsensitive | HexStringVerificationOptions.AllowTrailing0x => VerifyHexStringCaseInsensitiveWith0XRegex(),
            HexStringVerificationOptions.CaseInsensitive => VerifyHexStringCaseInsensitiveRegex(),

            HexStringVerificationOptions.Lowercase | HexStringVerificationOptions.AllowTrailing0x => VerifyHexStringLowercaseWith0XRegex(),
            HexStringVerificationOptions.Lowercase => VerifyHexStringLowercaseRegex(),

            HexStringVerificationOptions.Uppercase | HexStringVerificationOptions.AllowTrailing0x => VerifyHexStringUppercaseWith0XRegex(),
            HexStringVerificationOptions.Uppercase => VerifyHexStringUppercaseRegex(),

            _ => throw new ArgumentException("Options must specify whether its uppercase, lowercase, or case insensitive", nameof(options))
        };
}
