using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Intrinsics.X86;
using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Client.Types;

public readonly record struct HttpApiResponse<TApiResponse>(HttpStatusCode HttpStatusCode, APIResponse ApiResponse, string? ReasonPhrase)
    where TApiResponse : APIResponse
{
    public TApiResponse AsExpected()
    {
        ThrowIfNotSuccess();
        return ApiResponse is not TApiResponse tar
                ? throw new InvalidDataException($"The Request returned a response that was different from what was expected")
                : tar;
    }

    public bool TryGetAsExpected([NotNullWhen(true)] out TApiResponse? response)
    {
        ThrowIfNotSuccess();
        return (response = ApiResponse as TApiResponse) is not null;
    }

    public bool IsSuccessStatusCode
        => (int)HttpStatusCode is >= 200 and <= 299;

    public void ThrowIfNotSuccess()
    {
        string? apimsg = null;

        if (ApiResponse.Code.Code < 0)
            apimsg = ApiResponse is ErrorResponse err
                ? $"An error response was returned: {string.Join(", ", err.Errors)}; Trace: {err.TraceId}"
                : ApiResponse is TooManyRequestsResponse
                ? "The request was rate limited; try again later"
                : $"An unknown error ocurred when making the request, API Error Code: {ApiResponse.Code}";

        if (IsSuccessStatusCode is false)
            throw apimsg is null
                ? string.IsNullOrWhiteSpace(ReasonPhrase)
                    ? new HttpRequestException($"The StatusCode did not indicate success: {HttpStatusCode}")
                    : new HttpRequestException($"The StatusCode did not indicate success: {HttpStatusCode}; Reason: {ReasonPhrase}")
                : string.IsNullOrWhiteSpace(ReasonPhrase)
                    ? new HttpRequestException($"The StatusCode did not indicate success: {HttpStatusCode}; {apimsg}")
                    : new HttpRequestException($"The StatusCode did not indicate success: {HttpStatusCode}; Reason: {ReasonPhrase}; {apimsg}");

        if (apimsg is not null) // An error response was sent; but the HTTP Code did not indicate an error
            throw new HttpRequestException(apimsg);
    }

    public static implicit operator TApiResponse(HttpApiResponse<TApiResponse> httpApiResponse)
        => httpApiResponse.AsExpected();

    public static implicit operator APIResponse(HttpApiResponse<TApiResponse> httpApiResponse)
        => httpApiResponse.ApiResponse;
}

public readonly record struct HttpApiResponse(HttpStatusCode HttpStatusCode, APIResponse ApiResponse, string? ReasonPhrase)
{
    public bool IsSuccessStatusCode
        => (int)HttpStatusCode is >= 200 and <= 299;

    public void ThrowIfNotSuccess()
    {
        string? apimsg = null;

        if (ApiResponse.Code.Code < 0)
            apimsg = ApiResponse is ErrorResponse err
                ? $"An error response was returned: {string.Join(", ", err.Errors)}; Trace: {err.TraceId}"
                : ApiResponse is TooManyRequestsResponse
                ? "The request was rate limited; try again later"
                : $"An unknown error ocurred when making the request, API Error Code: {ApiResponse.Code}";

        if (IsSuccessStatusCode is false)
            throw apimsg is null
                ? string.IsNullOrWhiteSpace(ReasonPhrase)
                    ? new HttpRequestException($"The StatusCode did not indicate success: {HttpStatusCode}")
                    : new HttpRequestException($"The StatusCode did not indicate success: {HttpStatusCode}; Reason: {ReasonPhrase}")
                : string.IsNullOrWhiteSpace(ReasonPhrase)
                    ? new HttpRequestException($"The StatusCode did not indicate success: {HttpStatusCode}; {apimsg}")
                    : new HttpRequestException($"The StatusCode did not indicate success: {HttpStatusCode}; Reason: {ReasonPhrase}; {apimsg}");

        if (apimsg is not null) // An error response was sent; but the HTTP Code did not indicate an error
            throw new HttpRequestException(apimsg);
    }

    public HttpApiResponse<TApiResponse> Expect<TApiResponse>()
        where TApiResponse : APIResponse
        => new(HttpStatusCode, ApiResponse, ReasonPhrase);

    public static implicit operator APIResponse(HttpApiResponse httpApiResponse)
        => httpApiResponse.ApiResponse;
}
