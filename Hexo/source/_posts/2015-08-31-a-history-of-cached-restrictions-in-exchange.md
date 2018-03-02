---
title: A History of Cached Restrictions in Exchange
date: 2015-08-31 00:00:00
thumbnail: ruby01.jpg
tags:
- Exchange Server
- Exchange Web Services
- MAPI
---

{% asset_img ruby01.jpg Picture of Ruby %}
In this series of posts, I'm going to discuss three basic approaches to searching the content of Exchange mailboxes, and the tradeoffs that come with them. This series is for developers who are writing applications that talk to Exchange, or scripters who are using EWS Managed API from Powershell. I'm not going to be talking about New-MailboxSearch or searching from within Outlook, because in that case, the client code that executes the search is already written. This series is for people writing their own Exchange clients.

There are three basic ways to search a mailbox in Exchange Server:

1. Sort a table and seek to the items you're interested in. This approach is called a **sort-and-seek**.
2. Hand the server a set of criteria and tell it to only return items that match. This is the **Restrict** method in MAPI and **FindItems** in EWS.
3. Create a search folder with a set of criteria, and retrieve the contents of that folder to see the matching items.

For most of Exchange Server's history, approaches 2 and 3 were implemented basically the same way. Using either approach caused a table to be created in the database. These tables contained a small amount of information for each item that matched the search, and the tables would hang around in the database for some amount of time. These tables were called **cached restrictions** or **cached views**. I'm going to call them **cached restrictions**, because that was the popular terminology when I started supporting Exchange.

Recorded history basically starts with Exchange 5.5, so let's start there. Exchange 5.5 saved every single restriction for a certain amount of time. This meant that the first time you performed an [**IMAPITable::Restrict()**](https://msdn.microsoft.com/en-us/library/office/cc815682.aspx) on a certain folder, you would observe a delay while Exchange built the table. The second time you performed a **IMAPITable::Restrict()** on the same folder with the same restriction criteria, it was fast, because the restriction had been cached - that is, we now had a table for that restriction in the database, ready to be reused.

Exchange 5.5 continued keeping the cached restriction up to date as the content of the mailbox changed, just in case the client asked for that same search again. Every time a new item came into the Inbox, Exchange would update every cached restriction which was scoped to that folder. Unfortunately, this created a problem. If you had a lot of users sharing a mailbox, or you had an application that performed searches for lots of different criteria, you ended up with lots of different cached restrictions - possibly hundreds. Updating hundreds of cached restrictions every time a new email arrived got expensive and caused significant performance issues. As Exchange matured, changes were introduced to deal with this issue.

In Exchange 2003, a limit was put in place so Exchange would only cache 11 restrictions for a given folder (adjustable with **msExchMaxCachedViews** or **PR_MAX_CACHED_VIEWS**). This prevented hundreds of cached restrictions from accumulating for a folder, and neatly avoided that perf hit. However, this meant that if you had a user or application creating a bunch of one-off restrictions, the cache would keep cycling and no search would ever get satisfied from a cached restriction unless you adjusted these values. If you set the limit too high, then you reintroduced the performance problems that the limit had fixed.

In Exchange 2010, cached restrictions were changed to use dynamic updates instead of updating every time the mailbox changed. This made it less expensive to cache lots of restrictions, since they didn't all have to be kept up to date all the time. However, you could still run into situations where an application performed a bunch of one-off searches which were only used once but were then cached. When it came time to clean up those cached restrictions, the cleanup task could impact performance. We saw a few cases where Exchange 2010 mailboxes would be locked out for hours while the Information Store tried to clean up restrictions that were created across hundreds of folders.

In Exchange 2013 and 2016, the Information Store is selective about which restrictions it caches. As a developer of a client, you can't really predict whether your restriction is going to get cached, because this is a moving target. As Exchange 2013 and 2016 continue to evolve, they may cache something tomorrow that they don't cache today. If you're going to use the same search repeatedly in modern versions of Exchange, the only way to be sure the restriction is cached is to create a search folder. This is the behavior change described in [KB 3077710](https://support.microsoft.com/en-us/kb/3077710).

In all versions of Exchange, it was always important to think about how you were searching and try to use restrictions responsibly. Exchange 2013 and 2016 are unique in that they basically insist that you create a search folder if you want your restriction to be cached.

The next post in this series will explore some sample code that illustrates differences between Exchange 2013 and Exchange 2010 restriction behavior.