---
title: Use MAPIFolders for the TNEF issue
date: 2015-06-09 00:00:00
thumbnail: ruby09.jpg
tags:
- Exchange Server
- Powershell
---

{% asset_img ruby09.jpg Picture of Ruby %}
I've written a couple of previous posts on the corrupt TNEF issue that causes this error:

```
Microsoft.Exchange.Data.Storage.ConversionFailedException: The message content has become corrupted. ---> Microsoft.Exchange.Data.Storage.ConversionFailedException: Content conversion: Failed due to corrupt TNEF (violation status: 0x00008000)
```

For the history, see [this post](/2014/01/16/Public-Folder-Replication-fails-with-TNEF-violation-status-0x00008000) and [this post](/2014/06/02/TNEF-property-problem-update).

Previously, the solution was the [Delete-TNEFProps.ps1](https://gist.githubusercontent.com/bill-long/8573790) script. Unfortunately, that script has some limitations. Most notably, it cannot not fix attachments. This is a big problem for some environments where we have a lot of items with these properties on them.

I attempted to find a way to make the script remove the problem properties from attachments, but I could not figure out how to do it. Either this is impossible with **EWS**, or I'm missing an obscure trick. I finally gave up and went a different route.

For some time, I've been (slowly) working on a new tool called **MAPIFolders**. It is intended as a successor to **PFDAVAdmin** and **ExFolders**, though it is still fairly limited compared to those tools. It is also a command-line tool, unlike the older tools. However, it does have some advantages, such as the fact that it uses **MAPI**. This means it is not tied to deprecated APIs and frameworks like PFDAVAdmin was, and it doesn't rely on directly loading the Exchange DLLs like ExFolders does. It can be run from any client machine against virtually any version of Exchange, just like any other MAPI client.

Also, because it's MAPI, I can make it do almost anything, such as blowing away the properties on nested attachments and saving those changes.

Thanks to a customer who opened a case on the TNEF problem, I was able to test MAPIFolders in a significantly large public folder environment with a lot of corrupted TNEF items. After a bit of debugging and fixing things, MAPIFolders is now a far better solution to the TNEF issue than the Delete-TNEFProps script. It can remove the properties from attachments and even nested attachments.

The logging is very noisy and needs work, but that will have to wait. Writing C++ is just too painful. If you are running into the corrupt TNEF issue, you can grab MAPIFolders from GitHub: [https://github.com/bill-long/mapifolders/releases](https://github.com/bill-long/mapifolders/releases). The syntax for fixing the TNEF problem is described here: [https://github.com/bill-long/mapifolders/wiki/Check-Fix-Items-Operations](https://github.com/bill-long/mapifolders/wiki/Check-Fix-Items-Operations).
