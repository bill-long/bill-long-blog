---
title: TNEF property problem update
date: 2014-06-02 00:00:00
tags:
- Exchange Server
- Public Folders
---

Back in January, I wrote a [blog post about PF replication failing due to corrupt TNEF](/2014/01/16/Public-Folder-Replication-fails-with-TNEF-violation-status-0x00008000). The problem is caused by the presence of a couple of properties that have been deprecated and shouldn't be present on items anymore. At the time I wrote that post, we thought you could run the cleanup script to remove the properties and live happily ever after. So much for that idea.

We found that, in some environments, the problem kept coming back. Within hours of running the script, public folder replication would break again, and we would discover new items with the deprecated properties.

We recently discovered how that was happening. It turns out that there is a code path in Exchange 2013 where one of the properties is still being set. This means messages containing that property will sometimes get delivered to an Exchange 2013 mailbox. The user can then copy such an item into a public folder. If the public folders are still on Exchange 2010 or 2007, replication for that folder breaks with the corrupt TNEF error:

```
Microsoft.Exchange.Data.Storage.ConversionFailedException: The message content has become corrupted. ---> Microsoft.Exchange.Data.Storage.ConversionFailedException: Content conversion: Failed due to corrupt TNEF (violation status: 0x00008000)
```

Now that we know how this is happening, an upcoming release of Exchange 2013 will include a fix that stops it from setting this property. You'll need to continue using the script from the previous post to clean up affected items for now, but there is light at the end of the tunnel.
