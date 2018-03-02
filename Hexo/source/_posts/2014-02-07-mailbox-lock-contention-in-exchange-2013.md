---
title: Mailbox lock contention in Exchange 2013
date: 2014-02-07 00:00:00
tags:
- Exchange Server
---

In Exchange Server, when a call into the Information Store fails, we often report a **diagnostic context**. This information is extremely useful for those of us in support, because we can often use it to see exactly where the call failed without having to do any additional data collection. Unfortunately, diagnostic context info is mostly useless to customers, because it's impossible to make sense of it without the source code. In this post, I'll describe one specific thing you can look for in a diagnostic context to identify calls that are failing due to contention for the mailbox lock.

In Exchange 2013, changing something in a mailbox usually involves acquiring a lock so that other changes cannot be made at the same time. If an operation has grabbed the mailbox lock, any other operations that want to change things have to wait. They will line up and wait for the mailbox lock, and will eventually time out if they don't get it within a reasonable amount of time. However, there's a limit to how long the line itself is allowed to get. Once we have more than 10 operations waiting for the lock, any additional operations fail instantly with **MAPI\_E\_TIMEOUT** (**0x80040401**).

If you have a diagnostic context from Exchange 2013, perhaps from an event that was logged in the Application Log, then you can check for this situation by looking for **LID 53152** with **dwParam 0xA**. Here is an example:

```
    Lid: 55847   EMSMDBPOOL.EcPoolSessionDoRpc called [length=150]
    Lid: 43559   EMSMDBPOOL.EcPoolSessionDoRpc returned [ec=0x80040401][length=170][latency=0]
    Lid: 32881   StoreEc: 0x80040401
    Lid: 50035
    Lid: 64625   StoreEc: 0x80040401
    Lid: 52176   ClientVersion: 15.0.775.34
    Lid: 50032   ServerVersion: 15.0.775.6034
    Lid: 50128
    Lid: 1494    ---- Remote Context Beg ----
    Lid: 53152   dwParam: 0xA
    Lid: 43632   StoreEc: 0x80040401
    Lid: 58656   StoreEc: 0x80040401
    Lid: 35992   StoreEc: 0x80040401
    Lid: 1750    ---- Remote Context End ----
    Lid: 1494    ---- Remote Context Beg ----
    Lid: 53152   dwParam: 0xA
    Lid: 43632   StoreEc: 0x80040401
    Lid: 58656   StoreEc: 0x80040401
    Lid: 35992   StoreEc: 0x80040401
    Lid: 1750    ---- Remote Context End ----
    Lid: 50288
    Lid: 23354   StoreEc: 0x80040401
    Lid: 25913
    Lid: 21817   ROP Failure: 0x80040401
    Lid: 17361
    Lid: 19665   StoreEc: 0x80040401
    Lid: 37632
    Lid: 37888   StoreEc: 0x80040401
```

You'll notice the LID we're interested in is at the top of the **remote** context. The fact that LID 53152 shows a dwParam of 0xA means that we already have 0xA (decimal 10) operations waiting on the mailbox lock, so we purposely make this call fail instantly, without waiting at all. This usually results in a **MapiExceptionTimeout** and a **StorageTransientException** in all sorts of different places.

Once you've identified that mailbox contention is causing the error, there's still the question of why there is so much contention for the mailbox lock. Are there dozens of clients all trying to make changes in the mailbox at the same time? Is an application hammering the mailbox with requests? You still need to investigate to find the root cause, but after understanding this piece, you can at least begin to ask the right questions.
