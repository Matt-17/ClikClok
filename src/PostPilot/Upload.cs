namespace PostPilot;

sealed record UploadOptions(
    FileInfo Video,
    string? Account,
    string? Mode,
    string? Caption,
    string? Visibility,
    string? Comments,
    string? Duet,
    string? Stitch,
    string? Commercial,
    string? AiGenerated,
    bool ConfirmMusicUsage,
    bool Yes,
    bool NoWait);

static class Upload
{
    const int MaxCaptionLength = 2200;

    static readonly (string Value, string Api, string Label)[] Visibilities =
    [
        ("public", "PUBLIC_TO_EVERYONE", "Everyone"),
        ("friends", "MUTUAL_FOLLOW_FRIENDS", "Friends (followers you follow back)"),
        ("followers", "FOLLOWER_OF_CREATOR", "Followers"),
        ("private", "SELF_ONLY", "Only me"),
    ];

    static readonly (string Value, string Label)[] CommercialChoices =
    [
        ("none", "No commercial content"),
        ("own-brand", "Your brand: promoting yourself or your own business"),
        ("branded", "Branded content: promoting another brand or a third party"),
        ("both", "Both"),
    ];

    public static async Task Run(Settings settings, TikTokApi api, UploadOptions o, CancellationToken ct)
    {
        if (!o.Video.Exists)
            throw new UserError($"File not found: {o.Video.FullName}");

        var account = ResolveAccount(settings, o.Account);
        var mode = o.Mode ?? Prompt.Choose("--mode", "How should the video be posted?",
        [
            ("direct", "Direct post: publish right away"),
            ("draft", "Draft: send to the TikTok app inbox and finish there"),
        ]);
        var accessToken = await Tokens.Get(settings, api, account, ct);
        var source = SourceInfo.ForFile(o.Video.Length);
        var duration = VideoFile.ReadDuration(o.Video.FullName);

        UploadTarget target;
        if (mode == "draft")
        {
            var ignored = new (object? Value, string Name)[]
                {
                    (o.Caption, "--caption"), (o.Visibility, "--visibility"), (o.Comments, "--comments"),
                    (o.Duet, "--duet"), (o.Stitch, "--stitch"), (o.Commercial, "--commercial"),
                    (o.AiGenerated, "--ai-generated"), (o.ConfirmMusicUsage ? true : null, "--confirm-music-usage"),
                }
                .Where(x => x.Value is not null).Select(x => x.Name).ToArray();
            if (ignored.Length > 0)
                Console.WriteLine($"Draft mode: {string.Join(", ", ignored)} ignored; you set these in the TikTok app.");
            Console.WriteLine($"Sending {o.Video.Name} to the TikTok inbox of @{account.Name}.");
            target = await api.InitDraft(accessToken, source, ct);
        }
        else
        {
            if (!account.HasScope("video.publish"))
                throw new UserError($"@{account.Name} did not grant 'video.publish'. Run 'postpilot login' again.");
            var postInfo = await AskPostInfo(api, accessToken, o, duration, ct);
            target = await api.InitDirectPost(accessToken, postInfo, source, ct);
        }

        await api.UploadFile(target.UploadUrl, o.Video.FullName, source,
            sent => Console.Write($"\rUploading {sent * 100 / o.Video.Length,3}%"), ct);
        Console.WriteLine();
        Console.WriteLine($"Publish id: {target.PublishId}");

        if (o.NoWait)
        {
            Console.WriteLine("TikTok is processing the video. Check with 'postpilot status'.");
            return;
        }
        await WaitForResult(api, settings, account, target.PublishId, ct);
    }

    public static async Task WaitForResult(TikTokApi api, Settings settings, Account account, string publishId,
        CancellationToken ct)
    {
        Console.WriteLine("TikTok is processing the video; it can take a few minutes to show up.");
        var deadline = DateTimeOffset.UtcNow.AddMinutes(15);
        string? last = null;
        while (true)
        {
            var status = await api.GetStatus(await Tokens.Get(settings, api, account, ct), publishId, ct);
            if (status.Status != last)
                Console.WriteLine($"  {status.Status}");
            last = status.Status;

            switch (status.Status)
            {
                case "PUBLISH_COMPLETE":
                    Console.WriteLine(status.PublicPostIds is [var id, ..]
                        ? $"Published: https://www.tiktok.com/@{account.Name}/video/{id}"
                        : "Published.");
                    return;
                case "SEND_TO_USER_INBOX":
                    Console.WriteLine("The draft is in your TikTok inbox. Open the TikTok app to finish and post it.");
                    return;
                case "FAILED":
                    throw new UserError($"TikTok rejected the video: {status.FailReason}");
            }
            if (DateTimeOffset.UtcNow > deadline)
                throw new UserError($"Still {status.Status} after 15 minutes. Check later with " +
                                    $"'postpilot status {publishId} --account {account.Name}'.");
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
    }

    public static Account ResolveAccount(Settings settings, string? name)
    {
        if (settings.Accounts.Count == 0)
            throw new UserError("No TikTok account connected. Run 'postpilot login' first.");
        name ??= Prompt.Choose("--account", "Which account?",
            [.. settings.Accounts.Select(a => (a.Name, $"@{a.Name}" + (a.DisplayName is null ? "" : $" ({a.DisplayName})")))]);
        return settings.FindAccount(name)
               ?? throw new UserError($"Unknown account '{name}'. Connected: " +
                                      string.Join(", ", settings.Accounts.Select(a => a.Name)));
    }

    /// <summary>
    /// Collects every post setting TikTok requires the user to decide on, limited to what the creator's
    /// account allows. Nothing is preset: each setting comes from an option or is asked for.
    /// </summary>
    static async Task<PostInfo> AskPostInfo(TikTokApi api, string accessToken, UploadOptions o, TimeSpan? duration,
        CancellationToken ct)
    {
        var creator = await api.GetCreatorInfo(accessToken, ct);
        Console.WriteLine($"Posting to @{creator.CreatorUsername} ({creator.CreatorNickname})");

        if (duration is { } d && d.TotalSeconds > creator.MaxVideoPostDurationSec)
            throw new UserError($"The video is {d.TotalSeconds:0} s long; this account may post at most " +
                                $"{creator.MaxVideoPostDurationSec} s.");

        // Check every given option against the account before asking anything, so a wrong option fails
        // before any question has been answered.
        var allowed = Visibilities.Where(v => creator.PrivacyLevelOptions.Contains(v.Api)).ToArray();
        if (o.Visibility is { } givenVisibility && allowed.All(v => v.Value != givenVisibility))
            throw new UserError($"This account cannot post with visibility '{givenVisibility}'. Allowed: " +
                                string.Join(", ", allowed.Select(v => v.Value)));
        foreach (var (given, what, disabled) in new[]
                 {
                     (o.Comments, "Comments", creator.CommentDisabled),
                     (o.Duet, "Duets", creator.DuetDisabled),
                     (o.Stitch, "Stitches", creator.StitchDisabled),
                 })
            if (disabled && given == "on")
                throw new UserError($"{what} are turned off in this account's TikTok settings.");
        if (IsBranded(o.Commercial) && o.Visibility == "private")
            throw new UserError("Branded content cannot be private. Choose another visibility.");
        if (o.Caption is { Length: > MaxCaptionLength })
            throw new UserError($"The caption has {o.Caption.Length} characters; TikTok allows {MaxCaptionLength}.");

        var caption = o.Caption ?? AskCaption();

        // Branded content cannot be private, so whichever of the two is asked leaves out the conflicting choice.
        var visibilityChoices = allowed.Where(v => !IsBranded(o.Commercial) || v.Api != "SELF_ONLY").ToArray();
        if (visibilityChoices.Length == 0)
            throw new UserError("This account can only post privately, and branded content cannot be private.");
        var visibilityValue = o.Visibility ?? Prompt.Choose("--visibility", "Who can view this video?",
            [.. visibilityChoices.Select(v => (v.Value, v.Label))]);
        var visibility = allowed.First(v => v.Value == visibilityValue);

        var comments = AskInteraction(o.Comments, "--comments", "comments", creator.CommentDisabled);
        var duet = AskInteraction(o.Duet, "--duet", "duets", creator.DuetDisabled);
        var stitch = AskInteraction(o.Stitch, "--stitch", "stitches", creator.StitchDisabled);

        var isPrivate = visibility.Api == "SELF_ONLY";
        var commercial = o.Commercial ?? Prompt.Choose("--commercial",
            "Does this video promote a brand, product or service?" +
            (isPrivate ? " (Branded content is not available for private videos.)" : ""),
            [.. CommercialChoices.Where(c => !isPrivate || !IsBranded(c.Value))]);
        var ownBrand = commercial is "own-brand" or "both";
        var branded = IsBranded(commercial);
        if (ownBrand || branded)
            Console.WriteLine(branded
                ? "Your video will be labeled \"Paid partnership\"."
                : "Your video will be labeled \"Promotional content\".");

        var aiGenerated = o.AiGenerated is { } ai
            ? ai == "yes"
            : Prompt.YesNo("--ai-generated", "Is this video AI-generated?");

        var declaration = branded
            ? "By posting, you agree to TikTok's Branded Content Policy and Music Usage Confirmation."
            : "By posting, you agree to TikTok's Music Usage Confirmation.";
        if (o.ConfirmMusicUsage)
            Console.WriteLine(declaration);
        else if (!Prompt.YesNo("--confirm-music-usage", declaration + " Agree?"))
            throw new UserError("Not posted: the declaration was not accepted.");

        if (Prompt.Interactive && !o.Yes)
        {
            Console.WriteLine();
            Console.WriteLine($"  Account     @{creator.CreatorUsername}");
            Console.WriteLine($"  Caption     {(caption.Length == 0 ? "(none)" : caption)}");
            Console.WriteLine($"  Visibility  {visibility.Label}");
            Console.WriteLine($"  Comments {OnOff(comments)}, duet {OnOff(duet)}, stitch {OnOff(stitch)}");
            Console.WriteLine($"  Commercial  {commercial}, AI-generated {(aiGenerated ? "yes" : "no")}");
            if (!Prompt.YesNo("--yes", "Post now?"))
                throw new UserError("Not posted.");
        }

        return new PostInfo(caption, visibility.Api, !comments, !duet, !stitch, ownBrand, branded, aiGenerated);
    }

    /// <summary>An interaction the creator switched off in TikTok stays off and cannot be turned on.</summary>
    static bool AskInteraction(string? given, string option, string what, bool disabledByCreator)
    {
        if (disabledByCreator)
        {
            Console.WriteLine($"{char.ToUpper(what[0])}{what[1..]}: off (turned off in the account's TikTok settings)");
            return false;
        }
        return given is { } value ? value == "on" : Prompt.YesNo(option, $"Allow {what}?");
    }

    static string AskCaption()
    {
        while (true)
        {
            var caption = Prompt.Text("--caption", "Caption (hashtags included, may be empty)", allowEmpty: true);
            if (caption.Length <= MaxCaptionLength)
                return caption;
            Console.WriteLine($"That is {caption.Length} characters; TikTok allows {MaxCaptionLength}.");
        }
    }

    static bool IsBranded(string? commercial) => commercial is "branded" or "both";

    static string OnOff(bool on) => on ? "on" : "off";
}
