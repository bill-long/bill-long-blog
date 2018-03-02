---
title: Event 9414 means your OAB is missing objects
date: 2014-02-24 00:00:00
tags:
- Exchange Server
- Offline Address Book
---

Today, I want to highlight a behavior that isn't really called out anywhere in any existing documentation I can find. This is the behavior that occurs when Offline Address Book generation on Exchange 2010 logs an event **9414**, such as this one:

    Event ID: 9414
    Source: MSExchangeSA
    One or more properties cannot be read from Active Directory for 
    recipient '/o=Contoso/ou=Administrative Group/cn=Recipients/cn=User 1'
    in offline address book for '\Offline Address List - Contoso'.

When we stumble across a bad object like this, the OAB generation process will often skip a few *good* objects (in addition to the bad object) due to the way we handle the bookmark. As a result, **User 1**, from the event above, won't be the only thing missing from your Offline Address Book. If you turn up logging to the maximum so that OABGen logs every object it processes, you can figure out which objects are being skipped by observing which objects do *not* appear in the event log.

The bottom line is: If you want your OAB to be complete, you *must* fix the objects that are causing **9414**'s, even if the objects in the **9414**'s aren't ones you particularly care about.

So, why does it work this way, you ask?

The **9414** event was born in Exchange 2010 SP2 RU6. Before that, one of these bad objects would make OABGen fail completely and log the chain of events in **[KB 2751581](http://support.microsoft.com/kb/2751581)** - most importantly, the 9339:

	Event ID: 9339
	Source: MSExchangeSA
	Description: 
	Active Directory Domain Controller returned error 8004010e while
	generating the offline address book for '\Global Address List'. The
	last recipient returned by the Active Directory was 'User 9'. This
	offline address book will not be generated.
	- \Offline Address Book

Unfortunately, the old **9339** event didn't know what the actual problem object was. OABGen was working on batches of objects (typically 50 at a time), and when there was a problem with one object in the batch, the whole batch failed. All that OABGen could point to was the last object from the last successful group, which didn't really help much.

Thus, the **[OABValidate](http://oabvalidate.codeplex.com)** tool was born. The purpose of this tool is to scour the Active Directory looking for lingering links, lingering objects, and other issues that would trip up OABGen. As Exchange and Windows both changed the way they handled these calls, the behavior would often vary slightly between versions, so **OABValidate** just flags everything that could *possibly* be a problem. Which object was *actually* causing the **9339** wasn't certain, but if you fixed everything **OABValidate** highlighted, you would usually end up with a working OAB.

In large environments with hundreds of thousands of mail-enabled objects, cleaning up everything flagged by **OABValidate** could be a huge, time-consuming process. On top of that, residual AD replication issues could introduce new bad objects even as you were cleaning up the old bad objects.

Finally, thanks to a significant code change in Exchange 2010 SP2 RU6, Exchange was able to identify the actual problem object and point it out in a brand new event, the **9414**. In addition, OABGen would skip the object and continue generating the OAB, so that it wasn't totally broken by a single bad object anymore. This was a huge step forward that not only made **OABValidate** obsolete for most scenarios, but resulted in a situation where these OABGen errors can often go unnoticed for quite some time.

When someone finally does notice that the OAB is missing stuff, and you go look at your application log, you might think you can ignore these **9414**'s since they don't mention the object you're looking for. However, OABGen does still process objects in batches, and when it trips over that one bad object, the rest of the batch typically gets skipped.

So if you find that your OAB is missing objects, the first thing to do is check for **9414**'s and resolve the problems with those objects. While this does take a bit of work, it's much better than the methods you had to use to resolve this sort of issue before SP2 RU6.
