using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using DiegoG.REST;
using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Client;

public static class ApiHelper
{
    public static async Task<HttpApiResponse> ProcessAPIMessage(HttpResponseMessage msg, CancellationToken ct = default)
    {
        var serializer = ClientProgram.Services.GetRequiredService<IRESTObjectSerializer<ResponseCode>>();
        var typetable = ClientProgram.Services.GetRequiredService<RESTObjectTypeTable<ResponseCode>>();

        return new(msg.StatusCode, await serializer.DeserializeAsync<APIResponse>(await msg.Content.ReadAsStreamAsync(ct), typetable), msg.ReasonPhrase);
    }

    public static async Task<HttpApiResponse<TAPIResponse>> ProcessAPIMessage<TAPIResponse>(HttpResponseMessage msg, CancellationToken ct = default) 
        where TAPIResponse : APIResponse
    {
        var serializer = ClientProgram.Services.GetRequiredService<IRESTObjectSerializer<ResponseCode>>();
        var typetable = ClientProgram.Services.GetRequiredService<RESTObjectTypeTable<ResponseCode>>();

        return new(msg.StatusCode, await serializer.DeserializeAsync<APIResponse>(await msg.Content.ReadAsStreamAsync(ct), typetable), msg.ReasonPhrase);
    }

    public static async Task<HttpApiResponse> GetFromAPIAsync(this HttpClient client, string? requestUri, CancellationToken ct = default)
        => await ProcessAPIMessage(await client.GetAsync(requestUri, ct), ct);

    public static async Task<HttpApiResponse> DeleteFromAPIAsync(this HttpClient client, string? requestUri, CancellationToken ct = default)
        => await ProcessAPIMessage(await client.DeleteAsync(requestUri, ct), ct);

    public static async Task<HttpApiResponse> PostInAPIAsync<TContent>(this HttpClient client, string? requestUri, TContent content, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default) 
        => await ProcessAPIMessage(await client.PostAsJsonAsync(requestUri, content, jsonOptions, ct), ct);

    public static async Task<HttpApiResponse> PatchInAPIAsync<TContent>(this HttpClient client, string? requestUri, TContent content, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
        => await ProcessAPIMessage(await client.PatchAsJsonAsync(requestUri, content, jsonOptions, ct), ct);

    public static async Task<HttpApiResponse> PutInAPIAsync<TContent>(this HttpClient client, string? requestUri, TContent content, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default) 
        => await ProcessAPIMessage(await client.PutAsJsonAsync(requestUri, content, jsonOptions, ct), ct);

    public static async Task<HttpApiResponse<TAPIResponse>> GetFromAPIAsync<TAPIResponse>(this HttpClient client, string? requestUri, CancellationToken ct = default)
        where TAPIResponse : APIResponse
        => await ProcessAPIMessage<TAPIResponse>(await client.GetAsync(requestUri, ct), ct);

    public static async Task<HttpApiResponse<TAPIResponse>> DeleteFromAPIAsync<TAPIResponse>(this HttpClient client, string? requestUri, CancellationToken ct = default)
        where TAPIResponse : APIResponse
        => await ProcessAPIMessage<TAPIResponse>(await client.DeleteAsync(requestUri, ct), ct);

    public static async Task<HttpApiResponse<TAPIResponse>> PostInAPIAsync<TAPIResponse, TContent>(this HttpClient client, string? requestUri, TContent content, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
        where TAPIResponse : APIResponse
        => await ProcessAPIMessage<TAPIResponse>(await client.PostAsJsonAsync(requestUri, content, jsonOptions, ct), ct);

    public static async Task<HttpApiResponse<TAPIResponse>> PatchInAPIAsync<TAPIResponse, TContent>(this HttpClient client, string? requestUri, TContent content, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
        where TAPIResponse : APIResponse
        => await ProcessAPIMessage<TAPIResponse>(await client.PatchAsJsonAsync(requestUri, content, jsonOptions, ct), ct);

    public static async Task<HttpApiResponse<TAPIResponse>> PutInAPIAsync<TAPIResponse, TContent>(this HttpClient client, string? requestUri, TContent content, JsonSerializerOptions? jsonOptions = null, CancellationToken ct = default)
        where TAPIResponse : APIResponse
        => await ProcessAPIMessage<TAPIResponse>(await client.PutAsJsonAsync(requestUri, content, jsonOptions, ct), ct);

    public static async Task<TAPIResponse> AsExpected<TAPIResponse>(this Task<HttpApiResponse<TAPIResponse>> task)
        where TAPIResponse : APIResponse
        => (await task).AsExpected();

    public static async Task<HttpApiResponse<TAPIResponse>> ThrowIfNotSuccess<TAPIResponse>(this Task<HttpApiResponse<TAPIResponse>> task)
        where TAPIResponse : APIResponse
    {
        var x = await task;
        x.ThrowIfNotSuccess();
        return x;
    }

    public static async Task<HttpApiResponse> ThrowIfNotSuccess(this Task<HttpApiResponse> task)
    {
        var x = await task;
        x.ThrowIfNotSuccess();
        return x;
    }
}
