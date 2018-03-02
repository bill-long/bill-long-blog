---
title: MapiExceptionNotFound during content replication
date: 2014-01-30 00:00:00
tags:
- Exchange Server
- Public Folders
---

Today, I want to talk about another public folder replication problem we see repeatedly. Aren't you glad PF replication is gone in Exchange 2013?

This is one of the rarer public folder replication issues that we see, and it's caused by the attributes on the database. Actually, a database in this state sometimes causes a problem and sometimes does not, and I want to explain why that is.

The way this problem surfaces is that you see an event **3085** stating that outgoing replication failed with error **0x8004010f**. If you try something like **Update Content**, you'll get some error output with a diagnostic context that looks like this:

```
Error:
Cannot start content replication against public folder '\SomeFolder' on public folder database 'PFDB1'.

MapiExceptionNotFound: StartContentReplication failed. (hr=0x8004010f, ec=-2147221233)
Diagnostic context:
    Lid: 1494    ---- Remote Context Beg ----
    Lid: 19149   Error: 0x0
    Lid: 25805   Error: 0x0
    Lid: 11752   StoreEc: 0x8004010F
    Lid: 25260  
    Lid: 19149   Error: 0x0
    Lid: 25805   Error: 0x0
    Lid: 11752   StoreEc: 0x8004010F
    Lid: 25260  
    Lid: 19149   Error: 0x0
    Lid: 3010    StoreEc: 0x8004010F
    Lid: 3010    StoreEc: 0x8004010F
    Lid: 3650    StoreEc: 0x8004010F
    Lid: 3010    StoreEc: 0x8004010F
    Lid: 3010    StoreEc: 0x8004010F
    Lid: 3650    StoreEc: 0x8004010F
    Lid: 2492    StoreEc: 0x8004010F
    Lid: 2108    StoreEc: 0x8004010F
    Lid: 18128   StoreEc: 0x8004010F
    Lid: 18536   StoreEc: 0x8004010F
    Lid: 18544   StoreEc: 0x8004010F
    Lid: 18560   StoreEc: 0x8004010F
    Lid: 18740   StoreEc: 0x8004010F
    Lid: 1267    StoreEc: 0x8004010F
    Lid: 33819   StoreEc: 0x8004010F
    Lid: 27225   StoreEc: 0x8004010F
    Lid: 1750    ---- Remote Context End ----
    Lid: 26322   StoreEc: 0x8004010F
```

There are many problems that could cause some diagnostic output that looks similar to this. For this particular problem the error must be **MapiExceptionNotFound**, and the sequence of **Lid**s will usually be pretty close to what you see here.

This error occurs when the replica list on a public folder contains the GUID of a public folder database which does not have an **msExchOwningPFTree** value. It's easy to find a database in this state with an ldifde command to dump the properties of any public folder database objects where this value is not set:

```
ldifde -d "CN=Configuration,DC=contoso,DC=com" -r "(&(objectClass=msExchPublicMDB)(!(msExchOwningPFTree=*)))" -f unlinkedpfdb.txt
```

To fix the problem, you can either:

1. Delete the folder, if you can figure out which one it is.
2. Populate the **msExchOwningPFTree** value.
3. Delete the database in question from the Active Directory.

**Option 1** is usually not desirable, but I included it to illustrate the fact that a database in this state only causes a problem if existing folders ever had replicas on it. Keep in mind that the replica list you see in the management tools only shows you the current active replicas. The internal replica list tracks every replica that has ever existed, forever. Even if you remove all replicas from the database in question using the management tools, *the GUID of that database is still present in the internal replica list*, and it always will be. Thus, **you cannot unlink a database from the hierarchy if any existing folder has ever had replicas on it** - at least, not without breaking replication.

This is important, because certain third-party software will purposely keep public folder databases around that are not linked to the hierarchy. And that works fine, as long as they don't have replicas, and never did.

**Option 2** is the proper approach to fixing this situation if the database is still alive. Perhaps someone manually cleared the **msExchOwningPFTree** while troubleshooting or trying to affect the routing of emails to public folders. Just set the value to contain the DN of the hierarchy object. You can check your other PF databases to see what it should look like, as they should all have the same value. A few minutes after setting the value, replication should start working again.

If the database has been decommissioned, perhaps ungracefully, and it no longer exists, then you can go with **option 3** and simply delete the Active Directory object for the database using ADSI Edit. When the GUID in the replica list does not resolve to an object in the AD, that's fine - that's the normal state for a folder that once had replicas on databases that aren't around anymore, so it doesn't cause any problem.
