# ClikClok

.NET 10 command line tool that uploads videos to TikTok through the official
[Content Posting API](https://developers.tiktok.com/doc/content-posting-api-get-started).
It works with several TikTok accounts. Website: <https://code-ix.de/clikclok>

## Install

```
dotnet tool install -g ClikClok     # Windows, macOS, Linux with the .NET 10 SDK
```

Or download `clikclok.exe` from the [latest release](https://github.com/Matt-17/ClikClok/releases/latest).
Then connect your account with `clikclok login` and upload with `clikclok upload video.mp4`.

## Build from source

```
dotnet build src/ClikClok
dotnet pack src/ClikClok -o nupkg
dotnet tool install -g ClikClok --add-source ./nupkg
```

Releases are built by `.github/workflows/release.yml` when a tag `vX.Y.Z` matching the project version is
pushed. It needs the repository secrets `CLIKCLOK_CLIENT_KEY` and `CLIKCLOK_CLIENT_SECRET`. nuget.org uses Trusted
Publishing (policy for repository `Matt-17/ClikClok`, workflow `release.yml`, environment `NuGet`) with the
repository variable `NUGET_USER`.

## Using your own TikTok app

The released builds contain the credentials of the ClikClok TikTok app. To build against your own app instead:

1. Create an app in the [TikTok developer portal](https://developers.tiktok.com/apps) with the products
   **Login Kit** (platform *Desktop*, redirect URI `http://localhost:8723/callback/`) and
   **Content Posting API** (Direct Post enabled), scopes `user.info.basic`, `video.upload`, `video.publish`.
2. While the app is in sandbox, add every TikTok account that should post as a target user.
3. Put `CLIKCLOK_CLIENT_KEY` and `CLIKCLOK_CLIENT_SECRET` into `.env` at the repository root (or the build
   environment). The build embeds them into the assembly. At runtime environment variables and
   `clikclok configure` override the built-in values.
4. `clikclok login` once per account; `clikclok login --local` stores the login in a `.clikclok` folder in the
   current directory instead, which is used from that directory and below (keep it out of git).

Settings and tokens live in `CLIKCLOK_HOME`, else the nearest `.clikclok` folder from the current directory
upwards, else `%APPDATA%\ClikClok\settings.json`.

A `.env` file in the current directory is loaded at start; real environment variables win over it.

```
CLIKCLOK_CLIENT_KEY=...
CLIKCLOK_CLIENT_SECRET=...
CLIKCLOK_REDIRECT_URI=http://localhost:8723/callback/
CLIKCLOK_HOME=./.clikclok
CLIKCLOK_BROWSER=C:\Program Files\Google\Chrome\Application\chrome.exe   # browser for the login (default: system default; none: only print the link)
```

## Usage

```
clikclok login --link copy   # copy the login link instead of opening the browser
clikclok accounts
clikclok upload video.mp4 --account crayondraw0 --mode draft
clikclok upload video.mp4 -a crayondraw0 -m direct -c "Caption #tag" -v public \
  --comments on --duet off --stitch off --commercial none --ai-generated yes --confirm-music-usage -y
  # -p / --preview opens the video in the player first; asked in the terminal unless -y
clikclok status <publish-id> -a crayondraw0
clikclok logout crayondraw0
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
