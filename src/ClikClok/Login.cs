using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ClikClok;

/// <summary>OAuth 2 authorization code flow with PKCE, receiving the redirect on a local HTTP listener.</summary>
static class Login
{
    const string Scopes = "user.info.basic,video.upload,video.publish";
    const string VerifierChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";

    public static async Task<Account> Run(Settings settings, TikTokApi api, string? name, CancellationToken ct)
    {
        var clientKey = settings.RequireClientKey();
        var clientSecret = settings.RequireClientSecret();
        var redirectUri = settings.EffectiveRedirectUri;

        var verifier = RandomNumberGenerator.GetString(VerifierChars, 64);
        // TikTok's desktop flow expects the SHA-256 of the verifier hex encoded, not base64url as in RFC 7636.
        var challenge = Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = RandomNumberGenerator.GetHexString(32, lowercase: true);

        var url = TikTokApi.AuthorizeUrl +
                  $"?client_key={Uri.EscapeDataString(clientKey)}" +
                  $"&scope={Uri.EscapeDataString(Scopes)}" +
                  "&response_type=code" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&state={state}" +
                  $"&code_challenge={challenge}" +
                  "&code_challenge_method=S256";

        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri.EndsWith('/') ? redirectUri : redirectUri + "/");
        listener.Start();

        Console.WriteLine("Log in with the TikTok account in your browser. If it does not open, visit:");
        Console.WriteLine(url);
        OpenBrowser(url);

        var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromMinutes(5), ct);
        var query = context.Request.QueryString;
        var error = query["error"];
        var code = query["code"];
        var ok = error is null && code is not null && query["state"] == state;
        await Respond(context.Response, ok
            ? "ClikClok is connected. You can close this window."
            : "ClikClok could not connect. See the terminal for details.");

        if (error is not null)
            throw new UserError($"Login failed: {error} {query["error_description"]}");
        if (!ok)
            throw new UserError("Login failed: the redirect did not carry a valid code and state.");

        var token = await api.ExchangeCode(clientKey, clientSecret, code!, redirectUri, verifier, ct);
        var account = new Account
        {
            Name = "",
            OpenId = token.OpenId,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
        };
        Tokens.Apply(account, token);

        account.DisplayName = await api.GetDisplayName(account.AccessToken, ct);
        var username = account.HasScope("video.publish")
            ? (await api.GetCreatorInfo(account.AccessToken, ct)).CreatorUsername
            : null;
        account.Name = name?.TrimStart('@') ?? username ?? account.DisplayName ?? account.OpenId;

        settings.Accounts.RemoveAll(a => a.OpenId == account.OpenId || string.Equals(a.Name, account.Name,
            StringComparison.OrdinalIgnoreCase));
        settings.Accounts.Add(account);
        SettingsStore.Save(settings);
        return account;
    }

    static async Task Respond(HttpListenerResponse response, string message)
    {
        var html = $"<!doctype html><meta charset=utf-8><title>ClikClok</title>" +
                   $"<body style=\"font-family:sans-serif;padding:3em\"><h2>{WebUtility.HtmlEncode(message)}</h2>";
        var bytes = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // The URL is printed; the user can open it by hand.
        }
    }
}

static class Tokens
{
    static readonly TimeSpan Margin = TimeSpan.FromMinutes(5);

    public static void Apply(Account account, TokenResponse token)
    {
        var now = DateTimeOffset.UtcNow;
        account.AccessToken = token.AccessToken;
        account.AccessTokenExpiresAt = now.AddSeconds(token.ExpiresIn);
        account.RefreshToken = token.RefreshToken;
        account.RefreshTokenExpiresAt = now.AddSeconds(token.RefreshExpiresIn);
        account.Scope = token.Scope;
    }

    /// <summary>Returns a valid access token, refreshing and saving it when it is about to expire.</summary>
    public static async Task<string> Get(Settings settings, TikTokApi api, Account account, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (account.AccessTokenExpiresAt - Margin > now)
            return account.AccessToken;
        if (account.RefreshTokenExpiresAt <= now)
            throw new UserError($"The login of @{account.Name} has expired. Run 'clikclok login' again.");

        var token = await api.Refresh(settings.RequireClientKey(), settings.RequireClientSecret(), account.RefreshToken, ct);
        Apply(account, token);
        SettingsStore.Save(settings);
        return account.AccessToken;
    }
}
