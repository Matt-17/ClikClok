"""Builds the ClikClok website pages (overview, terms, privacy) into site/clikclok/."""
from pathlib import Path

from PIL import Image

HEAD = """<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>ClikClok · {title} · Code-iX</title>
<link rel="icon" href="/clikclok/favicon.ico" sizes="any"><link rel="icon" type="image/png" href="/clikclok/icon-512.png">
<link rel="apple-touch-icon" href="/clikclok/apple-touch-icon.png">
<style>
*{{box-sizing:border-box}}body{{margin:0;background:#F5F6FA;color:#16161D;font:18px/1.7 "Segoe UI",system-ui,sans-serif}}
main{{max-width:820px;margin:auto;padding:48px 24px 80px}}header{{display:flex;align-items:center;gap:24px}}
header svg{{width:80px;height:80px;flex:none}}h1{{font-size:clamp(28px,5vw,44px);line-height:1.15;margin:0}}
h2{{font-size:24px;line-height:1.3;margin-top:40px}}nav{{margin:32px 0}}nav a{{margin-right:24px}}
a{{color:#C8213F;text-underline-offset:4px}}a:focus-visible{{outline:3px solid #C8213F;outline-offset:4px}}
article{{padding-top:24px;margin-top:40px;border-top:1px solid #d4d6e0}}
footer{{margin-top:48px;font-size:16px}}code{{overflow-wrap:anywhere;background:#e8e9f0;padding:2px 6px;border-radius:4px}}
</style></head><body><main><header><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 108 108" role="img">
  <title>ClikClok</title>
  <defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#EE3A62"/><stop offset="1" stop-color="#36E4E0"/></linearGradient></defs>
  <rect width="108" height="108" fill="#121218"/>
  <rect x="9" y="9" width="90" height="90" rx="19" fill="none" stroke="url(#g)" stroke-width="3.5"/>
  <path d="M35 36 L50 48 L35 60" fill="none" stroke="#fff" stroke-width="5"/>
  <rect x="54" y="64" width="25" height="5" fill="#fff"/>
</svg>
<h1>ClikClok<br>{title}</h1></header>
<nav aria-label="Pages"><a href="./">Overview</a><a href="terms">Terms of Service</a><a href="privacy">Privacy Policy</a></nav>
<article>"""

FOOT = """</article>
<footer><a href="https://code-ix.de/impressum.html">Impressum / Legal notice</a> · <a href="mailto:m.voigt@code-ix.de">Contact Matthias Voigt</a></footer>
</main></body></html>
"""

OVERVIEW = """
<p>ClikClok is a command line tool that lets creators upload their own videos to their TikTok accounts from their computer, using TikTok's official Content Posting API.</p>
<h2>How it works</h2>
<p>You connect your TikTok account once through TikTok's login page in your browser. Afterwards you upload a video with one command. Before a video is posted, ClikClok shows the account it posts to and asks for every post setting TikTok requires you to choose: who can view the video, whether comments, duets and stitches are allowed, whether the video promotes a brand, whether it is AI-generated, and your agreement to TikTok's Music Usage Confirmation. You can also send a video to your TikTok inbox as a draft and finish it in the TikTok app.</p>
<p>ClikClok runs entirely on your computer. It has no server, no user accounts of its own and no tracking.</p>
<p>Developed by Matthias Voigt, Code-iX, Dresden, Germany.</p>
"""

TERMS = """
<p>Updated: September 24, 2026</p>
<p>These terms apply to the use of ClikClok, a command line tool developed by Matthias Voigt, Code-iX ("we"). By using ClikClok you agree to them.</p>
<h2>The service</h2>
<p>ClikClok is software you run on your own computer to upload videos to TikTok accounts you are authorized to use. It communicates directly with TikTok's official APIs. We do not operate a server for ClikClok and do not receive your content.</p>
<h2>Your responsibilities</h2>
<p>You may only connect TikTok accounts you own or are authorized to manage, and only upload content you have the rights to publish. You remain responsible for the content you post and must comply with <a href="https://www.tiktok.com/legal/terms-of-service">TikTok's Terms of Service</a>, <a href="https://www.tiktok.com/community-guidelines">Community Guidelines</a>, Music Usage Confirmation and, for branded content, the Branded Content Policy. Do not use ClikClok to send spam, to automate mass posting or to circumvent TikTok's limits.</p>
<h2>TikTok</h2>
<p>ClikClok is an independent tool and is not affiliated with, endorsed or sponsored by TikTok. TikTok may change or limit its APIs at any time, which can affect what ClikClok can do. Whether and how a video is published is decided by TikTok.</p>
<h2>Warranty and liability</h2>
<p>ClikClok is provided free of charge and as is. We are liable without limitation for intent and gross negligence, for injury to life, body or health and under the German Product Liability Act. Otherwise we are liable only for breach of essential obligations, limited to the typical, foreseeable damage. To the extent permitted by law, any further liability is excluded.</p>
<h2>Changes and termination</h2>
<p>We may update these terms; the current version is published on this page. You can stop using ClikClok at any time and disconnect your accounts with <code>clikclok logout</code> or in your TikTok settings.</p>
<h2>Law and contact</h2>
<p>German law applies, excluding the UN Convention on Contracts for the International Sale of Goods; mandatory consumer protection rules of your country of residence remain unaffected. Questions: <a href="mailto:m.voigt@code-ix.de">m.voigt@code-ix.de</a>.</p>
"""

PRIVACY = """
<p>Updated: September 24, 2026</p>
<p>ClikClok is developed by Matthias Voigt, Code-iX, who is the controller for the contact data described below. Contact: <a href="mailto:m.voigt@code-ix.de">m.voigt@code-ix.de</a>. The linked legal notice contains the full address.</p>
<h2>What ClikClok processes</h2>
<p>ClikClok runs on your computer and has no server of its own. The developer does not receive any of the following data.</p>
<ul>
<li><strong>TikTok login:</strong> when you connect an account, TikTok returns access and refresh tokens, your TikTok open ID, display name, username and the granted permissions (<code>user.info.basic</code>, <code>video.upload</code>, <code>video.publish</code>). ClikClok stores them in a settings file in your user profile on your computer so it can post without a new login.</li>
<li><strong>Videos and post settings:</strong> the video file you choose, its caption and the post settings you select are sent directly to TikTok to publish the video or place it in your TikTok inbox.</li>
<li><strong>Creator information:</strong> before posting, ClikClok requests your account's posting options from TikTok (for example allowed privacy levels and maximum video length) and shows them to you. It is not stored.</li>
</ul>
<p>Data sent to TikTok is processed by TikTok under its own <a href="https://www.tiktok.com/legal/privacy-policy">Privacy Policy</a>.</p>
<h2>No tracking</h2>
<p>ClikClok contains no analytics, advertising or crash reporting and contacts no services other than TikTok's APIs.</p>
<h2>Your controls</h2>
<p><code>clikclok logout &lt;account&gt;</code> revokes the access at TikTok and deletes the account's tokens from your computer. You can also remove ClikClok's access in your TikTok app settings, and delete the settings file at any time.</p>
<h2>Contact and this website</h2>
<p>If you email us, your address, message and email metadata are processed only to handle your request, based on our legitimate interest in communicating with users (Article 6(1)(f) GDPR), and deleted once it is resolved unless retention is legally required. This website sets no cookies, uses no analytics and loads no third-party content; the hosting provider may process technically necessary server logs. Under the GDPR you have the rights of access, rectification, erasure, restriction, objection and data portability, and you may lodge a complaint with a supervisory authority.</p>
"""

# Static pages saved as .php so the site's .htaccess serves them without an extension (/clikclok/terms).
PAGES = {"index.php": ("Overview", OVERVIEW), "terms.php": ("Terms of Service", TERMS), "privacy.php": ("Privacy Policy", PRIVACY)}

out = Path(__file__).parent / "clikclok"
out.mkdir(exist_ok=True)
for name, (title, body) in PAGES.items():
    (out / name).write_text(HEAD.format(title=title) + body + FOOT, encoding="utf-8", newline="\n")

# Favicons come from the app icon itself, so the browser tab shows the same icon as the TikTok app listing.
icon = Image.open(Path(__file__).parent.parent / "assets" / "clikclok-icon.png").convert("RGB")
icon.save(out / "favicon.ico", sizes=[(16, 16), (32, 32), (48, 48), (64, 64)])
icon.resize((180, 180), Image.LANCZOS).save(out / "apple-touch-icon.png", optimize=True)
icon.resize((512, 512), Image.LANCZOS).save(out / "icon-512.png", optimize=True)
