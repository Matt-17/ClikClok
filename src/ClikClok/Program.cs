using System.CommandLine;
using ClikClok;

DotEnv.Load(Path.Combine(Environment.CurrentDirectory, ".env"));

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
var api = new TikTokApi(http);

var root = new RootCommand("ClikClok: upload videos to TikTok through the official Content Posting API.");
root.Subcommands.Add(ConfigureCommand());
root.Subcommands.Add(LoginCommand());
root.Subcommands.Add(AccountsCommand());
root.Subcommands.Add(LogoutCommand());
root.Subcommands.Add(UploadCommand());
root.Subcommands.Add(StatusCommand());

return await root.Parse(args).InvokeAsync();

Command ConfigureCommand()
{
    var clientKey = new Option<string?>("--client-key") { Description = "Client key of the TikTok developer app" };
    var clientSecret = new Option<string?>("--client-secret") { Description = "Client secret of the TikTok developer app" };
    var redirectUri = new Option<string?>("--redirect-uri")
    {
        Description = $"Redirect URI registered in the app (default {Settings.DefaultRedirectUri})",
    };
    var command = new Command("configure", "Store the credentials of your TikTok developer app")
        { clientKey, clientSecret, redirectUri };
    command.SetAction(parse => Run(async _ =>
    {
        var settings = SettingsStore.Load();
        settings.ClientKey = parse.GetValue(clientKey) ?? Prompt.Text("--client-key", "Client key");
        settings.ClientSecret = parse.GetValue(clientSecret) ?? Prompt.Text("--client-secret", "Client secret", secret: true);
        settings.RedirectUri = parse.GetValue(redirectUri) ?? settings.RedirectUri;
        SettingsStore.Save(settings);
        Console.WriteLine($"Saved to {SettingsStore.Directory}. Redirect URI: {settings.RedirectUri}");
        await Task.CompletedTask;
    }));
    return command;
}

Command LoginCommand()
{
    var name = new Option<string?>("--name", "-n") { Description = "Local name for the account (default: its TikTok username)" };
    var local = new Option<bool>("--local", "-l")
    {
        Description = $"Store the login in a {SettingsStore.LocalFolder} folder in the current directory (used from there and below)",
    };
    var link = Choice("--link", null, "How to open the TikTok login page: in the browser, or copy the link to the clipboard", "open", "copy");
    var command = new Command("login", "Connect a TikTok account through the browser") { name, local, link };
    command.SetAction((parse, ct) => Run(async _ =>
    {
        if (parse.GetValue(local))
            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, SettingsStore.LocalFolder));
        var account = await Login.Run(SettingsStore.Load(), api, parse.GetValue(name), parse.GetValue(link), ct);
        Console.WriteLine($"Connected @{account.Name}" + (account.DisplayName is null ? "" : $" ({account.DisplayName})") +
                          $" in {SettingsStore.Directory}");
        if (!account.HasScope("video.publish"))
            Console.WriteLine("Direct posting was not granted; only --mode draft works for this account.");
    }));
    return command;
}

Command AccountsCommand()
{
    var command = new Command("accounts", "List connected TikTok accounts");
    command.SetAction(_ => Run(async _ =>
    {
        var settings = SettingsStore.Load();
        if (settings.Accounts.Count == 0)
            Console.WriteLine("No accounts connected. Run 'clikclok login'.");
        foreach (var a in settings.Accounts)
            Console.WriteLine($"@{a.Name,-24} {a.DisplayName,-24} login valid until {a.RefreshTokenExpiresAt.LocalDateTime:yyyy-MM-dd}  [{a.Scope}]");
        await Task.CompletedTask;
    }));
    return command;
}

Command LogoutCommand()
{
    var account = new Argument<string>("account") { Description = "Account to disconnect" };
    var command = new Command("logout", "Disconnect a TikTok account and revoke its access") { account };
    command.SetAction((parse, ct) => Run(async _ =>
    {
        var settings = SettingsStore.Load();
        var found = settings.FindAccount(parse.GetValue(account)!)
                    ?? throw new UserError($"Unknown account '{parse.GetValue(account)}'.");
        try
        {
            await api.Revoke(settings.RequireClientKey(), settings.RequireClientSecret(), found.AccessToken, ct);
        }
        catch (Exception e) when (e is HttpRequestException or TikTokException)
        {
            Console.Error.WriteLine($"Could not revoke the token at TikTok ({e.Message}); removing it locally.");
        }
        settings.Accounts.Remove(found);
        SettingsStore.Save(settings);
        Console.WriteLine($"Disconnected @{found.Name}.");
    }));
    return command;
}

Command UploadCommand()
{
    var video = new Argument<FileInfo>("video") { Description = "Video file (mp4, mov or webm)" };
    var account = new Option<string?>("--account", "-a") { Description = "Account to post to" };
    var mode = Choice("--mode", "-m", "direct: publish now; draft: send to the TikTok app inbox", "direct", "draft");
    var caption = new Option<string?>("--caption", "-c") { Description = "Caption with hashtags (direct mode)" };
    var visibility = Choice("--visibility", "-v", "Who can view the video", "public", "friends", "followers", "private");
    var comments = Choice("--comments", null, "Allow comments", "on", "off");
    var duet = Choice("--duet", null, "Allow duets", "on", "off");
    var stitch = Choice("--stitch", null, "Allow stitches", "on", "off");
    var commercial = Choice("--commercial", null, "Commercial content disclosure", "none", "own-brand", "branded", "both");
    var aiGenerated = Choice("--ai-generated", null, "Label the video as AI-generated", "yes", "no");
    var confirmMusic = new Option<bool>("--confirm-music-usage")
    {
        Description = "Agree to TikTok's Music Usage Confirmation (and Branded Content Policy for branded content)",
    };
    var preview = new Option<bool>("--preview", "-p") { Description = "Open the video in the system's player before posting" };
    var yes = new Option<bool>("--yes", "-y") { Description = "Skip the questions 'Open the video?' and 'Post now?'" };
    var noWait = new Option<bool>("--no-wait") { Description = "Return after the upload instead of waiting for TikTok" };

    var command = new Command("upload", "Upload a video. Settings that are not given are asked for.")
    {
        video, account, mode, caption, visibility, comments, duet, stitch, commercial, aiGenerated, confirmMusic, preview, yes, noWait,
    };
    command.SetAction((parse, ct) => Run(_ => Upload.Run(SettingsStore.Load(), api, new UploadOptions(
        parse.GetValue(video)!,
        parse.GetValue(account),
        parse.GetValue(mode),
        parse.GetValue(caption),
        parse.GetValue(visibility),
        parse.GetValue(comments),
        parse.GetValue(duet),
        parse.GetValue(stitch),
        parse.GetValue(commercial),
        parse.GetValue(aiGenerated),
        parse.GetValue(confirmMusic),
        parse.GetValue(preview),
        parse.GetValue(yes),
        parse.GetValue(noWait)), ct)));
    return command;
}

Command StatusCommand()
{
    var publishId = new Argument<string>("publish-id") { Description = "Publish id printed by 'upload'" };
    var account = new Option<string?>("--account", "-a") { Description = "Account the video was posted to" };
    var command = new Command("status", "Wait for or show the result of an upload") { publishId, account };
    command.SetAction((parse, ct) => Run(async _ =>
    {
        var settings = SettingsStore.Load();
        var found = Upload.ResolveAccount(settings, parse.GetValue(account));
        await Upload.WaitForResult(api, settings, found, parse.GetValue(publishId)!, ct);
    }));
    return command;
}

// A string option limited to fixed values, matched case-insensitively.
static Option<string?> Choice(string name, string? alias, string description, params string[] values)
{
    var option = alias is null ? new Option<string?>(name) : new Option<string?>(name, alias);
    option.Description = description;
    option.HelpName = string.Join("|", values);
    option.CustomParser = result => result.Tokens is [var token] ? token.Value.ToLowerInvariant() : null;
    option.Validators.Add(result =>
    {
        if (result.Tokens is [var token] && !values.Contains(token.Value.ToLowerInvariant()))
            result.AddError($"{name} must be one of: {string.Join(", ", values)}");
    });
    return option;
}

static async Task<int> Run(Func<CancellationToken, Task> action)
{
    try
    {
        await action(CancellationToken.None);
        return 0;
    }
    catch (Exception e) when (e is UserError or TikTokException)
    {
        Console.Error.WriteLine(e.Message);
        return 1;
    }
}
