using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ClikClok;

/// <summary>Thin client for the TikTok Login Kit and Content Posting API (v2).</summary>
sealed class TikTokApi(HttpClient http)
{
    public const string AuthorizeUrl = "https://www.tiktok.com/v2/auth/authorize/";
    const string ApiBase = "https://open.tiktokapis.com";

    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public Task<TokenResponse> ExchangeCode(string clientKey, string clientSecret, string code, string redirectUri,
        string codeVerifier, CancellationToken ct) =>
        Token(new()
        {
            ["client_key"] = clientKey,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier,
        }, ct);

    public Task<TokenResponse> Refresh(string clientKey, string clientSecret, string refreshToken, CancellationToken ct) =>
        Token(new()
        {
            ["client_key"] = clientKey,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        }, ct);

    public async Task Revoke(string clientKey, string clientSecret, string accessToken, CancellationToken ct)
    {
        using var response = await http.PostAsync(ApiBase + "/v2/oauth/revoke/", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_key"] = clientKey,
                ["client_secret"] = clientSecret,
                ["token"] = accessToken,
            }), ct);
    }

    async Task<TokenResponse> Token(Dictionary<string, string> form, CancellationToken ct)
    {
        using var response = await http.PostAsync(ApiBase + "/v2/oauth/token/", new FormUrlEncodedContent(form), ct);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct)) as JsonObject
            ?? throw new TikTokException("invalid_response", $"HTTP {(int)response.StatusCode} without JSON body", null);
        if (body["access_token"] is null)
            throw new TikTokException(body["error"]?.ToString() ?? "token_error",
                body["error_description"]?.ToString() ?? "", body["log_id"]?.ToString());
        return body.Deserialize<TokenResponse>(Json)!;
    }

    public async Task<string?> GetDisplayName(string accessToken, CancellationToken ct)
    {
        var data = await Call(HttpMethod.Get, "/v2/user/info/?fields=open_id,display_name", accessToken, null, ct);
        return data?["user"]?["display_name"]?.ToString();
    }

    public async Task<CreatorInfo> GetCreatorInfo(string accessToken, CancellationToken ct) =>
        (await Call(HttpMethod.Post, "/v2/post/publish/creator_info/query/", accessToken, new { }, ct))
            .Deserialize<CreatorInfo>(Json)!;

    public async Task<UploadTarget> InitDirectPost(string accessToken, PostInfo postInfo, SourceInfo sourceInfo,
        CancellationToken ct) =>
        (await Call(HttpMethod.Post, "/v2/post/publish/video/init/", accessToken,
            new { PostInfo = postInfo, SourceInfo = sourceInfo }, ct)).Deserialize<UploadTarget>(Json)!;

    public async Task<UploadTarget> InitDraft(string accessToken, SourceInfo sourceInfo, CancellationToken ct) =>
        (await Call(HttpMethod.Post, "/v2/post/publish/inbox/video/init/", accessToken,
            new { SourceInfo = sourceInfo }, ct)).Deserialize<UploadTarget>(Json)!;

    public async Task<PublishStatus> GetStatus(string accessToken, string publishId, CancellationToken ct) =>
        (await Call(HttpMethod.Post, "/v2/post/publish/status/fetch/", accessToken,
            new { PublishId = publishId }, ct)).Deserialize<PublishStatus>(Json)!;

    /// <summary>Uploads the file in the chunks announced in <paramref name="source"/>; the last chunk takes the remainder.</summary>
    public async Task UploadFile(string uploadUrl, string path, SourceInfo source, Action<long> progress, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var size = stream.Length;
        for (var i = 0; i < source.TotalChunkCount; i++)
        {
            var start = i * source.ChunkSize;
            var end = i == source.TotalChunkCount - 1 ? size - 1 : start + source.ChunkSize - 1;
            var buffer = new byte[end - start + 1];
            stream.Position = start;
            await stream.ReadExactlyAsync(buffer, ct);

            using var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = new ByteArrayContent(buffer) };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(VideoFile.ContentType(path));
            request.Content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, size);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new TikTokException("upload_failed",
                    $"Chunk {i + 1}/{source.TotalChunkCount} returned HTTP {(int)response.StatusCode}: " +
                    await response.Content.ReadAsStringAsync(ct), null);
            progress(end + 1);
        }
    }

    async Task<JsonNode?> Call(HttpMethod method, string path, string accessToken, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, ApiBase + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        using var response = await http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        var json = (text.Length > 0 ? JsonNode.Parse(text) : null) as JsonObject
            ?? throw new TikTokException("invalid_response", $"HTTP {(int)response.StatusCode} without JSON body", null);
        var error = json["error"];
        var code = error?["code"]?.ToString() ?? "unknown";
        if (code != "ok")
            throw new TikTokException(code, error?["message"]?.ToString() ?? "", error?["log_id"]?.ToString());
        return json["data"];
    }
}

sealed class TikTokException(string code, string message, string? logId)
    : Exception($"TikTok error '{code}': {message}" + (logId is null ? "" : $" (log id {logId})"))
{
    public string Code => code;
}

sealed record TokenResponse(
    string AccessToken, long ExpiresIn, string OpenId, string RefreshToken, long RefreshExpiresIn, string Scope);

sealed record CreatorInfo(
    string CreatorUsername,
    string CreatorNickname,
    string[] PrivacyLevelOptions,
    bool CommentDisabled,
    bool DuetDisabled,
    bool StitchDisabled,
    int MaxVideoPostDurationSec);

sealed record PostInfo(
    string Title,
    string PrivacyLevel,
    bool DisableComment,
    bool DisableDuet,
    bool DisableStitch,
    bool BrandOrganicToggle,
    bool BrandContentToggle,
    bool IsAigc);

sealed record SourceInfo(string Source, long VideoSize, long ChunkSize, int TotalChunkCount)
{
    const long MaxSingleChunk = 64L * 1024 * 1024;
    const long ChunkBytes = 10L * 1024 * 1024;

    /// <summary>Files up to 64 MB go up in one piece; larger ones in 10 MB chunks with the remainder in the last.</summary>
    public static SourceInfo ForFile(long size) =>
        size <= MaxSingleChunk
            ? new("FILE_UPLOAD", size, size, 1)
            : new("FILE_UPLOAD", size, ChunkBytes, (int)(size / ChunkBytes));
}

sealed record UploadTarget(string PublishId, string UploadUrl);

sealed record PublishStatus(
    string Status,
    string? FailReason,
    long UploadedBytes,
    // TikTok's field name is misspelled in the API.
    [property: JsonPropertyName("publicaly_available_post_id")] long[]? PublicPostIds);
