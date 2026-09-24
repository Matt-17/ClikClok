using System.Reflection;
using System.Text.Json;

namespace ClikClok;

/// <summary>
/// App credentials and connected accounts. Credentials resolve from environment variables, then this file,
/// then the values built into the assembly.
/// </summary>
sealed class Settings
{
    public const string DefaultRedirectUri = "http://localhost:8723/callback/";

    public string? ClientKey { get; set; }
    public string? ClientSecret { get; set; }
    public string RedirectUri { get; set; } = DefaultRedirectUri;
    public List<Account> Accounts { get; set; } = [];

    public string RequireClientKey() =>
        Environment.GetEnvironmentVariable("CLIKCLOK_CLIENT_KEY") is { Length: > 0 } key ? key
        : ClientKey ?? BuiltIn("ClikClok.ClientKey")
        ?? throw new UserError("No client key configured. Run 'clikclok configure' first.");

    public string RequireClientSecret() =>
        Environment.GetEnvironmentVariable("CLIKCLOK_CLIENT_SECRET") is { Length: > 0 } secret ? secret
        : ClientSecret ?? BuiltIn("ClikClok.ClientSecret")
        ?? throw new UserError("No client secret configured. Run 'clikclok configure' first.");

    static string? BuiltIn(string key) =>
        typeof(Settings).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value is { Length: > 0 } value ? value : null;

    public string EffectiveRedirectUri =>
        Environment.GetEnvironmentVariable("CLIKCLOK_REDIRECT_URI") is { Length: > 0 } uri ? uri : RedirectUri;

    public Account? FindAccount(string name) =>
        Accounts.FirstOrDefault(a => string.Equals(a.Name, name.TrimStart('@'), StringComparison.OrdinalIgnoreCase));
}

sealed class Account
{
    public required string Name { get; set; }
    public required string OpenId { get; set; }
    public string? DisplayName { get; set; }
    public required string AccessToken { get; set; }
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public required string RefreshToken { get; set; }
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    public string Scope { get; set; } = "";

    public bool HasScope(string scope) => Scope.Split(',', StringSplitOptions.TrimEntries).Contains(scope);
}

static class SettingsStore
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public const string LocalFolder = ".clikclok";

    /// <summary>CLIKCLOK_HOME, else the nearest .clikclok folder from the current directory up, else the user profile.</summary>
    public static string Directory =>
        Environment.GetEnvironmentVariable("CLIKCLOK_HOME") is { Length: > 0 } home ? home
        : FindLocal() ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClikClok");

    static string? FindLocal()
    {
        for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, LocalFolder);
            if (System.IO.Directory.Exists(candidate))
                return candidate;
        }
        return null;
    }

    static string FilePath => Path.Combine(Directory, "settings.json");

    public static Settings Load() =>
        File.Exists(FilePath)
            ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), Json) ?? new Settings()
            : new Settings();

    public static void Save(Settings settings)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Json));
        File.Move(temp, FilePath, overwrite: true);
    }
}

/// <summary>An error caused by input or state the user can fix; printed without a stack trace.</summary>
sealed class UserError(string message) : Exception(message);
