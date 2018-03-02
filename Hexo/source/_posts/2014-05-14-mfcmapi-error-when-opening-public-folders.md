---
title: MfcMapi error when opening public folders
date: 2014-05-14 00:00:00
tags:
- Exchange Server
- Public Folders
---

There are a lot of little problems I run across that I never investigate, simply because there's no impact and no one seems to care. I have my hands full investigating issues that are impacting people, so investing time to chase down something else is usually not a good use of my time.

One of those errors is something that **MfcMapi** returns when you open public folders. In many environments, including some of my own lab environments, if you open **MfcMapi** and double-click **Public Folders**, you get a dialog box stating that error **0x8004010f** **MAPI\_E\_NOT\_FOUND** was encountered when trying to get a property list.


{% asset_img sshot-8.png Image %}

If you click OK, a second error indicates that **GetProps(NULL)** failed with the same error.

{% asset_img sshot-9.png Image %}

After clicking OK on that error, and then double-clicking on the public folders again, you get the same two errors, but then it opens. At this point you can see the folders and everything appears normal.

I've been seeing this error for at least five years - maybe closer to ten. It's hard to say at this point, but I've been seeing it for so long, I considered it normal. I never looked into it, because no one cared.

That is, until I got a case on it recently.

Some folks use **MfcMapi** as the benchmark to determine if things are working. If **MfcMapi** doesn't work, then the problem is with Exchange, and their own product can't be expected to work.

This was the basis for a recent case of mine. A third-party product wasn't working, so they tried to open the public folders with **MfcMapi**, and got this error. Therefore, they could not proceed with troubleshooting until we fixed this error.

Of course, as far as I knew, this error was totally normal, and I told them so, but they still wanted us to track it down. Fortunately, this provided a perfect opportunity to chase down one of those little errors that has bothered me for years, but that I never investigated.

By debugging **MfcMapi** (hey, it's open source, anyone can debug it) and taking an **ExTRA** trace on the Exchange side, we discovered that **MfcMapi** was trying to call **GetPropList** on an OAB that did not exist. Looking in the **NON\_IPM\_SUBTREE**, we only saw the **EX:<admin group legacy DN>** OAB, which Exchange hasn't used since Exchange 5.5.

{% asset_img sshot-10.png Image %}

In Exchange 2000 and later, we use the various OABs created through the Exchange management tools. The name will still have a legacy DN, but it won't start with **EX:**, so it's easy to distinguish the real OABs from an old unused legacy OAB folder. Here's what a real OAB looks like in the public folders, when it's present:

{% asset_img sshot-11.png Image %}

In this case, we didn't see the real OAB. We only saw the site-based OAB from the Exchange 5.5 days.

It turned out that the real OAB was set to only allow **web-based distribution**, not PF distribution. That explained why the OAB could not be seen in the **NON\_IPM\_SUBTREE**. Despite that fact, **MfcMapi** was still trying to call **GetPropList** on it. Since the folder didn't exist, it failed with **MAPI\_E\_NOT\_FOUND**.

Thus, one of the great mysteries of the universe (or at least my little Exchange Server universe) is finally solved!

In the customer environment, we fixed the error by enabling PF distribution for the OAB. I doubt this had anything to do with the issue the third-party tool was having, but who knows? At the very least, we were able to move the troubleshooting process forward by solving this, and maybe this blog post will save people from chasing their tails over this error in the future.
