---
title: Database bloat in Exchange 2010
date: 2015-06-03 00:00:00
thumbnail: ruby08.jpg
tags:
- Exchange Server
- Powershell
---

{% asset_img ruby08.jpg Picture of Ruby %}
I keep deciding not to write this post, because Exchange 2010 is out of mainstream support. And yet, we are still getting these cases from time to time, so I suppose I will finally write it, and hopefully it helps someone.

In Exchange 2010, we had several bugs that led to the database leaking space. These bugs had to do with the cleanup of deleted items. When a client deletes items, those items go into a cleanup queue. A background process is supposed to come through and process the items out of the cleanup queue to free up that space. Unfortunately, that didn't consistently work, and sometimes the space would be leaked.

There was an initial attempt to fix this, which did resolve many of the open cases I had at the time, but not all of them. Later, another fix went in, and this resolved the issue for all my remaining cases at the time. Both of those fixes were included in Exchange 2010 SP3 RU1.

After that, we occasionally still see a case where space is leaking even with these fixes in place. But every time we try to trace it so we can fix the problem, the act of turning on tracing fixes the behavior. I've been back and forth through that code, and there's no apparent reason that the tracing should affect the way cleanup actually behaves. Nonetheless, in these rare cases where the fixes didn't totally fix the problem, tracing fixes it every time. I wish I knew why.

The tracing workaround has its limitations, though. The cleanup queue is not persisted in the database, so tracing only works for an active leak where the database has not yet been dismounted. After the database is dismounted, any leaked space is effectively permanent at that point, and your best bet is to move the mailboxes off. When the entire mailbox is moved, that leaked space will be freed, since it was still associated with the mailbox.

So, how can you tell if you're being affected by this problem? One option is to just turn on tracing:

1. Launch **ExTRA**.
2. Choose **Trace Control**. You'll get a standard warning. Click OK.
3. Choose a location for the ETL file and choose the option for **circular logging**. You can make the file as large or as small as you want. It doesn't really matter, since our goal here isn't to look at the trace output.
4. Click the **Set manual trace tags** button.
5. At the top, check all eight **Trace Type** boxes.
6. Under **Components to Trace**, highlight the **Store** component (but don't check it).
7. In the **Trace Tags** for store on the right, check the box next to **tagCleanupMsg**. We only need this one tag.
8. Click **Start Tracing** at the bottom.

Let the trace run for a day or two and observe the effect on database whitespace. If you see significant amounts of space being freed with tracing on, then you're hitting this problem. Again, this only works if the database has not been dismounted since the space leaked.

Another option is to analyze the database space to see if you're hitting this problem. Here's how you do that.

1. Dismount the database and run
   `eseutil /ms /v "C:\databases\somedatabase.edb" > C:\spacereport.txt`
2. For the same database, launch **Exchange Management Shell** and run
   `Get-MailboxStatistics -Database SomeDatabase | Export-Csv C:\mailboxstatistics.csv`
3. Use my [Analyze-SpaceDump.ps1](https://gist.github.com/bill-long/4910d4f6453282819976) script to parse the spacereport.txt: 
   `.\Analyze-SpaceDump.ps1 C:\spacereport.txt`
4. Look for the "Largest body tables" at the bottom of the report. These are the largest mailboxes in terms of the actual space they use in the database. These numbers are in megabytes, so if it reports that a body table owns 7000, that means that mailbox owns 7 GB of space in the database.
5. Grab the ID from the body table. For example, if the table is Body-1-ABCD, then the ID is 1-ABCD. This will correspond to the MailboxTableIdentifier in the mailboxstatistics.csv.
6. Find that mailbox in the statistics output and add up the TotalItemSize and TotalDeletedItemSize. By comparing that against how much space the body table is using in the database, you know how much space has leaked.

It's often normal to have small differences, but when you see that a mailbox has leaked gigabytes, then you're hitting this problem.

You can also compare the overall leaked size with some quick Powershell scripting. When I get these files from a customer, I run the following to add up the mailbox size from the mailbox statistics csv:

{% codeblock lang:powershell %}

$stats = Import-Csv C:\mailboxstatistics.csv
$deletedItemBytes = ($stats | foreach { $bytesStart = $_.TotalDeletedItemSize.IndexOf("(") ; $bytes = $_.TotalDeletedItemSize.Substring($bytesStart + 1) ; $bytesEnd = $bytes.IndexOf(" ") ; $bytes = $bytes.Substring(0, $bytesEnd) ; $bytes } | Measure-Object –Sum).Sum
$totalItemBytes = ($stats | foreach { $bytesStart = $_.TotalItemSize.IndexOf("(") ; $bytes = $_.TotalItemSize.Substring($bytesStart + 1) ; $bytesEnd = $bytes.IndexOf(" ") ; $bytes = $bytes.Substring(0, $bytesEnd) ; $bytes } | Measure-Object –Sum).Sum
($deletedItemBytes + $totalItemBytes) / 1024 / 1024

{% endcodeblock %}

This gives you the size in megabytes as reported by Get-MailboxStatistics. Then, you can go look at the Analyze-SpaceDump.ps1 output and compare this to the "Spaced owned by body tables", which is also in megabytes. The difference between the two gives you an idea of how much total space has leaked across all mailboxes.

Ultimately, the resolution is usually to move the mailboxes. If the database has not been dismounted, you can turn on tagCleanupMsg tracing to recover the space.

The SP3 RU1 fixes made this problem extremely rare in Exchange 2010, and the store redesign in Exchange 2013 seems to have eliminated it completely. As of this writing, I haven't seen a single case of this on Exchange 2013.
