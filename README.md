# PostPilot

.NET 10 command line tool that uploads videos to TikTok through the official
[Content Posting API](https://developers.tiktok.com/doc/content-posting-api-get-started).
It works with several TikTok accounts.

## Build and install

```
dotnet build src/PostPilot
dotnet pack src/PostPilot -o nupkg
dotnet tool install -g PostPilot --add-source ./nupkg
```

## Setup

1. Create an app in the [TikTok developer portal](https://developers.tiktok.com/apps) with the products
   **Login Kit** (platform *Desktop*, redirect URI `http://localhost:8723/callback/`) and
   **Content Posting API** (Direct Post enabled), scopes `user.info.basic`, `video.upload`, `video.publish`.
2. While the app is in sandbox, add every TikTok account that should post as a target user.
3. `postpilot configure` stores client key and secret (or set `POSTPILOT_CLIENT_KEY` / `POSTPILOT_CLIENT_SECRET`).
4. `postpilot login` once per account.

Settings and tokens live in `%APPDATA%\PostPilot\settings.json` (override with `POSTPILOT_HOME`).

A `.env` file in the current directory is loaded at start; real environment variables win over it.

```
POSTPILOT_CLIENT_KEY=...
POSTPILOT_CLIENT_SECRET=...
POSTPILOT_REDIRECT_URI=http://localhost:8723/callback/
POSTPILOT_HOME=./.postpilot
```

## Usage

```
postpilot accounts
postpilot upload video.mp4 --account crayondraw0 --mode draft
postpilot upload video.mp4 -a crayondraw0 -m direct -c "Caption #tag" -v public \
  --comments on --duet off --stitch off --commercial none --ai-generated yes --confirm-music-usage -y
postpilot status <publish-id> -a crayondraw0
postpilot logout crayondraw0
```

Every post setting TikTok requires the user to choose has no default. Missing ones are asked for in the
terminal. When the tool runs without a terminal, a missing setting is an error. The choices offered come
from the account's creator info (allowed visibilities, interactions switched off, maximum video length).
Given options are checked against the account before any question is asked, and questions leave out
choices that conflict with given options (branded content cannot be private). In draft mode the post
settings are ignored with a notice, since they are set in the TikTok app.

## Limits of an unaudited app

Until TikTok has audited the app, direct posts are forced to *Only me* and the account must be private
when posting. At most 5 accounts can post per day. `--mode draft` has no such limit: the video lands in
the TikTok app inbox and is finished and posted there.
