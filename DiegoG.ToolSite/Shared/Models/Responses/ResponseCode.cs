using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public readonly record struct ResponseCode(ResponseCodeEnum Code)
{
    public string Name { get; } = Code.ToString();

    public override string ToString()
        => $"{Code}: {Name}";

    public static implicit operator ResponseCode(ResponseCodeEnum code)
        => new(code);

    public static implicit operator ResponseCodeEnum(ResponseCode code)
        => code.Code;
}
