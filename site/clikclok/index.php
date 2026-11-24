<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>ClikClok · Overview · Code-iX</title>
<link rel="icon" href="/clikclok/favicon.ico" sizes="any"><link rel="icon" type="image/png" href="/clikclok/icon-512.png">
<link rel="apple-touch-icon" href="/clikclok/apple-touch-icon.png">
<style>
*{box-sizing:border-box}body{margin:0;background:#F5F6FA;color:#16161D;font:18px/1.7 "Segoe UI",system-ui,sans-serif}
main{max-width:820px;margin:auto;padding:48px 24px 80px}header{display:flex;align-items:center;gap:24px}
header svg{width:80px;height:80px;flex:none}h1{font-size:clamp(28px,5vw,44px);line-height:1.15;margin:0}
h2{font-size:24px;line-height:1.3;margin-top:40px}nav{margin:32px 0}nav a{margin-right:24px}
a{color:#C8213F;text-underline-offset:4px}a:focus-visible{outline:3px solid #C8213F;outline-offset:4px}
article{padding-top:24px;margin-top:40px;border-top:1px solid #d4d6e0}
footer{margin-top:48px;font-size:16px}code{overflow-wrap:anywhere;background:#e8e9f0;padding:2px 6px;border-radius:4px}
</style></head><body><main><header><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 108 108" role="img">
  <title>ClikClok</title>
  <defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#EE3A62"/><stop offset="1" stop-color="#36E4E0"/></linearGradient></defs>
  <rect width="108" height="108" fill="#121218"/>
  <rect x="9" y="9" width="90" height="90" rx="19" fill="none" stroke="url(#g)" stroke-width="3.5"/>
  <path d="M35 36 L50 48 L35 60" fill="none" stroke="#fff" stroke-width="5"/>
  <rect x="54" y="64" width="25" height="5" fill="#fff"/>
</svg>
<h1>ClikClok<br>Overview</h1></header>
<nav aria-label="Pages"><a href="./">Overview</a><a href="terms">Terms of Service</a><a href="privacy">Privacy Policy</a></nav>
<article>
<p>ClikClok is a command line tool that lets creators upload their own videos to their TikTok accounts from their computer, using TikTok's official Content Posting API.</p>
<h2>How it works</h2>
<p>You connect your TikTok account once through TikTok's login page in your browser. Afterwards you upload a video with one command. Before a video is posted, ClikClok shows the account it posts to and asks for every post setting TikTok requires you to choose: who can view the video, whether comments, duets and stitches are allowed, whether the video promotes a brand, whether it is AI-generated, and your agreement to TikTok's Music Usage Confirmation. You can also send a video to your TikTok inbox as a draft and finish it in the TikTok app.</p>
<p>ClikClok runs entirely on your computer. It has no server, no user accounts of its own and no tracking.</p>
<h2>Install</h2>
<p>On Windows with <a href="https://learn.microsoft.com/windows/package-manager/">winget</a>:</p>
<p><code>winget install CodeiX.ClikClok</code></p>
<p>On Windows, macOS or Linux with the <a href="https://dotnet.microsoft.com/download">.NET 10 SDK</a>:</p>
<p><code>dotnet tool install -g ClikClok</code></p>
<p>Or download <code>clikclok.exe</code> for Windows from the <a href="https://github.com/Matt-17/ClikClok/releases/latest">latest release</a>; it needs nothing else installed.</p>
<h2>Getting started</h2>
<ol>
<li>Connect your TikTok account: <code>clikclok login</code>. Your browser opens TikTok's login page; after you allow access, ClikClok remembers the account. Repeat this for every account you want to post to.</li>
<li>Upload a video: <code>clikclok upload video.mp4</code>. ClikClok asks for the caption and every post setting, shows a summary and posts only after you confirm.</li>
<li>Or send it to your TikTok inbox as a draft and finish it in the app: <code>clikclok upload video.mp4 --mode draft</code>.</li>
</ol>
<p>Other commands: <code>clikclok accounts</code> lists your connected accounts, <code>clikclok status &lt;publish-id&gt;</code> shows whether TikTok has published a video, and <code>clikclok logout &lt;account&gt;</code> disconnects an account. <code>clikclok --help</code> lists all options.</p>
<h2>Support</h2>
<p>Questions or problems: <a href="https://github.com/Matt-17/ClikClok/issues">open an issue on GitHub</a> or email <a href="mailto:m.voigt@code-ix.de">m.voigt@code-ix.de</a>.</p>
<p>Developed by Matthias Voigt, Code-iX, Dresden, Germany.</p>
</article>
<footer><a href="https://code-ix.de/impressum.html">Impressum / Legal notice</a> · <a href="mailto:m.voigt@code-ix.de">Contact Matthias Voigt</a></footer>
</main></body></html>
